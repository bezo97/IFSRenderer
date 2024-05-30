using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using IFSEngine.Model;
using IFSEngine.Rendering.GpuStructs;
using IFSEngine.Utility;

using Nito.AsyncEx;

using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;

namespace IFSEngine.Rendering;

public sealed class RendererGL : IAsyncDisposable
{
    /// <summary>
    /// Invoked by the rendering thread. The frequency of updates is influenced by <see cref="EnablePerceptualUpdates"/>.
    /// </summary>
    public event EventHandler DisplayFramebufferUpdated;

    /// <summary>
    /// Invoked by the rendering thread when <see cref="TotalIterations"/> reaches the limit defined by <see cref="IFS.TargetIterationLevel"/>.
    /// </summary>
    public event EventHandler TargetIterationReached;

    public bool IsInitialized { get; private set; } = false;
    public bool IsRendering { get; private set; } = false;

    public bool UpdateDisplayOnRender { get; set; } = true;

    /// <summary>
    /// Enable perceptually equal difference between updates.
    /// </summary>
    public bool EnablePerceptualUpdates { get; set; } = true;

    /// <summary>
    /// Enable Density Estimation.
    /// </summary>
    public bool EnableDE { get; set; } = false;
    public int DEMaxRadius { get; set; } = 9;
    public double DEPower { get; set; } = 0.2;
    public double DEThreshold { get; set; } = 0.4;

    /// <summary>
    /// Number of dispatches since accumulation reset.
    /// This is needed for random generation.
    /// </summary>
    private int _dispatchCnt = 0;

    public int HistogramWidth { get; private set; } = 1920;
    public int HistogramHeight { get; private set; } = 1080;
    public int DisplayWidth { get; private set; } = 1280;
    public int DisplayHeight { get; private set; } = 720;

    private string _includesSource;
    private List<Transform> _registeredTransforms;

    public IFS LoadedParams { get; private set; } = new IFS();
    private bool _invalidHistogramResolution = false;
    private bool _invalidHistogramBuffer = false;
    private bool _invalidParamsBuffer = false;
    private bool _invalidPointsStateBuffer = false;

    /// <summary>
    /// Number of workgroups to be dispatched. Each workgroup consists of 64 kernel invocations. Default value is 256.
    /// </summary>
    public int WorkgroupCount { get; private set; } = 256;
    public async Task SetWorkgroupCount(int s)
    {
        WorkgroupCount = s;
        await WithContext(() =>
        {
            GL.NamedBufferData(_pointsBufferHandle, InvocationCount * (4 * sizeof(float) + 2 * sizeof(float) + 2 * sizeof(int)), IntPtr.Zero, BufferUsageHint.StaticCopy);

            InvalidatePointsStateBuffer();
        });
    }

    private const int WorkgroupSize = 64;//nv:32, amd:64. Optimal is 64.

    public int InvocationCount => WorkgroupCount * WorkgroupSize;

    /// <summary>
    /// Number of iterations a single invocation performs.
    /// This value is adjusted after each dispatch in order to approach <see cref="TargetFramerate"/>
    /// </summary>
    public int InvocationIters { get; private set; } = 500;

    /// <summary>
    /// The render loop aims to reach the specified framerate by measuring compute kernel execution time and adjusting the workload size. Measured in Fps.
    /// </summary>
    public int TargetFramerate { get; set; } = 60;

    /// <summary>
    /// Total iterations since accumulation reset
    /// </summary>
    public ulong TotalIterations { get; private set; } = 0;

    /// <summary>
    /// Maximum radius of the spatial filter.
    /// Higher values are slow to render.
    /// </summary>
    public int MaxFilterRadius { get; set; } = 0;
    public double HistogramScale { get; private set; } = 1.0;
    /// <summary>
    /// Seed value used to generate random numbers. Recommended to change this for each animation frame.
    /// </summary>
    public uint Seed { get; set; } = 0;
    /// <summary>
    /// True when the level of <see cref="TotalIterations"/> is beyond <see cref="IFS.TargetIterationLevel"/>.
    /// Reset by <see cref="InvalidateHistogramBuffer"/>.
    /// </summary>
    public bool IsTargetIterationReached { get; private set; } = false;
    /// <summary>
    /// Whether to mark the area in focus with red. This is used as a visual aid when the user is changing the focus.
    /// Should be false when doing final render.
    /// </summary>
    public bool MarkAreaInFocus { get; set; } = false;

    private bool _updateDisplayNow = false;
    private readonly IGraphicsContext _ctx;

    private int _vertexShaderHandle;
    private int _vao;
    //compute shader handles
    private int _computeProgramHandle;
    private int _histogramBufferHandle;
    private int _settingsBufferHandle;
    private int _iteratorsBufferHandle;
    private int _aliasBufferHandle;
    private int _pointsBufferHandle;
    private int _paletteBufferHandle;
    private int _realParametersBufferHandle;
    private int _vec3ParametersBufferHandle;
    //fragment shader handles
    private int _tonemapProgramHandle;
    private int _deProgramHandle;
    private int _offscreenFBOHandle;
    private int _renderTextureHandle;

    private readonly AsyncAutoResetEvent _stopRender = new(false);

    private readonly float[] _bufferClearColor = [0.0f, 0.0f, 0.0f];
    private readonly string _shadersPath = "IFSEngine.Rendering.Shaders.";
    private readonly bool _debugFlag = true;

    //https://gist.github.com/Vassalware/d47ff5e60580caf2cbbf0f31aa20af5d
    private static void DebugCallback(DebugSource source,
        DebugType type,
        int id,
        DebugSeverity severity,
        int length,
        IntPtr message,
        IntPtr userParam)
    {
        string messageString = Marshal.PtrToStringAnsi(message, length);

        System.Diagnostics.Debug.WriteLine($"{severity} {type} | {messageString}");

        if (type == DebugType.DebugTypeError)
        {
            throw new Exception(messageString);
        }
    }
    private static readonly DebugProc _debugProcCallback = DebugCallback;
    private static GCHandle _debugProcCallbackHandle;
    private int _timerQueryHandle;

    /// <summary>
    /// Number of iterator stucts in the buffer.
    /// </summary>
    private int _iteratorsBufferSize = 0;

    /// <summary>
    /// Number of real values in the buffer.
    /// </summary>
    private int _realParametersBufferSize = 0;

    /// <summary>
    /// Number of vec3 values in the buffer.
    /// </summary>
    private int _vec3ParametersBufferSize = 0;

    /// <summary>
    /// Number of iterators in the xaos matrix.
    /// </summary>
    private int _aliasBufferSize = 0;

    /// <summary>
    /// Number of colors in the buffer.
    /// </summary>
    private int _paletteBufferSize = 0;

    /// <summary>
    /// Creates a new renderer instance.
    /// <see cref="Initialize"/> must be called before starting the render loop.
    /// </summary>
    /// <param name="ctx"></param>
    public RendererGL(IGraphicsContext ctx)
    {
        this._ctx = ctx;
    }

    public async Task Initialize(IEnumerable<string> includeSources, IEnumerable<Transform> transforms)
    {
        if (IsInitialized)
            throw new InvalidOperationException("Renderer is already initialized.");

        if (_debugFlag)
        {
            _debugProcCallbackHandle = GCHandle.Alloc(_debugProcCallback);
            GL.DebugMessageCallback(_debugProcCallback, IntPtr.Zero);
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
        }

        _includesSource = string.Join(Environment.NewLine, includeSources);
        _registeredTransforms = transforms.ToList();

        //attributeless rendering
        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);
        //empty vao

        InitBuffers();
        InitTonemapPass();
        InitDEPass();
        InitComputeProgram();
        GL.DeleteShader(_vertexShaderHandle);

        _timerQueryHandle = GL.GenQuery();

        SetHistogramScaleToDisplay();

        IsInitialized = true;
        await SetWorkgroupCount(WorkgroupCount);

        InvalidateParamsBuffer();
    }

    public async Task LoadTransforms(IEnumerable<string> includeSources, IEnumerable<Transform> transforms)
    {
        if (!IsInitialized)
            throw NewNotInitializedException();

        await WithContext(() =>
        {
            _includesSource = string.Join(Environment.NewLine, includeSources);
            _registeredTransforms = transforms.ToList();
            InitComputeProgram();
            InvalidateParamsBuffer();
        });
    }

    public void LoadParams(IFS p)
    {
        LoadedParams = p;
        InvocationIters = 500;
        InvalidateParamsBuffer();
        SetHistogramScaleToDisplay();
    }

    /// <summary>
    /// Invalidates the data in the parameter buffers, which causes the render thread to update them.
    /// Also invalidates the accumulation and state buffers.
    /// Usually called whenever the loaded <see cref="IFS"/> structure or a parameter changes.
    /// </summary>
    public void InvalidateParamsBuffer()
    {
        InvalidateHistogramBuffer();
        _invalidParamsBuffer = true;
    }

    /// <summary>
    /// Invalidates the data in the histogram buffer, which causes the render thread to empty it.
    /// Also invalidates the state buffer.
    /// Usually called whenever the <see cref="Camera"/> parameters are changed.
    /// </summary>
    public void InvalidateHistogramBuffer()
    {
        InvalidatePointsStateBuffer();
        IsTargetIterationReached = false;
        _invalidHistogramBuffer = true;
    }

    public void InvalidateDisplay()
    {
        _updateDisplayNow = true;
    }

    private void InvalidatePointsStateBuffer()
    {
        _invalidPointsStateBuffer = true;
    }

    public void SetHistogramScale(double scale)
    {
        HistogramScale = scale;
        int newWidth = (int)(LoadedParams.ImageResolution.Width * HistogramScale);
        int newHeight = (int)(LoadedParams.ImageResolution.Height * HistogramScale);
        if (newWidth != HistogramWidth || newHeight != HistogramHeight)
        {
            HistogramWidth = newWidth;
            HistogramHeight = newHeight;
            _invalidHistogramResolution = true;
            InvalidateHistogramBuffer();
        }
    }

    public void SetHistogramScaleToDisplay()
    {
        double rw = DisplayWidth / (double)LoadedParams.ImageResolution.Width;
        double rh = DisplayHeight / (double)LoadedParams.ImageResolution.Height;
        double rr = Math.Min(rw, rh) * .98;
        SetHistogramScale(rr);
    }

    private void UpdateHistogramResolution()
    {
        GL.NamedBufferData(_histogramBufferHandle, HistogramWidth * HistogramHeight * 4 * sizeof(float), IntPtr.Zero, BufferUsageHint.StaticCopy);
        //resize display texture. TODO: separate & use display resolution
        GL.UseProgram(_computeProgramHandle);
        GL.Uniform1(GL.GetUniformLocation(_computeProgramHandle, "width"), HistogramWidth);
        GL.Uniform1(GL.GetUniformLocation(_computeProgramHandle, "height"), HistogramHeight);
        GL.UseProgram(_tonemapProgramHandle);
        GL.Uniform1(GL.GetUniformLocation(_tonemapProgramHandle, "width"), HistogramWidth);
        GL.Uniform1(GL.GetUniformLocation(_tonemapProgramHandle, "height"), HistogramHeight);
        GL.UseProgram(_deProgramHandle);
        GL.Uniform1(GL.GetUniformLocation(_deProgramHandle, "width"), HistogramWidth);
        GL.Uniform1(GL.GetUniformLocation(_deProgramHandle, "height"), HistogramHeight);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _renderTextureHandle);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, HistogramWidth, HistogramHeight, 0, PixelFormat.Rgba, PixelType.Float, new IntPtr(0));

        GL.Viewport(0, 0, HistogramWidth, HistogramHeight);

        //must clear the display framebuffer since we're only blitting to it so garbage from previous resolution could remain visible.
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.ClearBuffer(ClearBuffer.Color, 0, _bufferClearColor);
        _ctx.SwapBuffers();//clear back buffer
        GL.ClearBuffer(ClearBuffer.Color, 0, _bufferClearColor);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _offscreenFBOHandle);

        DisplayFramebufferUpdated?.Invoke(this, null);

        _invalidHistogramResolution = false;
    }

    public void SetDisplayResolution(int displayWidth, int displayHeight)
    {
        this.DisplayWidth = displayWidth;
        this.DisplayHeight = displayHeight;
        InvalidateDisplay();
    }

    public void DispatchCompute()
    {
        if (!IsInitialized)
            throw NewNotInitializedException();

        GL.UseProgram(_computeProgramHandle);

        if (_invalidHistogramBuffer)
        {
            if (_invalidHistogramResolution)
            {
                UpdateHistogramResolution();
                GL.UseProgram(_computeProgramHandle);
            }

            //reset accumulation
            GL.ClearNamedBufferData(_histogramBufferHandle, PixelInternalFormat.R32f, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
            _invalidHistogramBuffer = false;
            _dispatchCnt = 0;
            TotalIterations = 0;
            InvalidateDisplay();
            InvalidatePointsStateBuffer();//needed when IterationDepth is high

            //update settings struct
            var settings = new SettingsStruct
            {
                camera_params = LoadedParams.Camera.GetCameraParameters(),
                itnum = LoadedParams.Iterators.Count,
                fog_effect = (float)LoadedParams.FogEffect,
                palettecnt = LoadedParams.Palette.Colors.Count,
                entropy = (float)LoadedParams.Entropy,
                warmup = LoadedParams.Warmup,
                max_filter_radius = MaxFilterRadius,
                mark_area_in_focus = MarkAreaInFocus ? 1 : 0
            };
            GL.NamedBufferSubData(_settingsBufferHandle, 0, Marshal.SizeOf(typeof(SettingsStruct)), ref settings);

        }

        if (_invalidParamsBuffer)
        {
            //generate iterator and transform structs
            var its = new List<IteratorStruct>();
            var allRealParams = new List<float>();
            var allVec3Params = new List<Vector4>();
            var currentIterators = LoadedParams.Iterators.ToList();
            //start weights -> alias method tables
            double sumStartWeights = currentIterators.Sum(i => i.StartWeight);
            if (sumStartWeights == 0.0)
            {
                //TODO: throw new InvalidOperationException("Invalid params: No input iterator found.");
                return;
            }
            var normalizedStartWeights = currentIterators.Select(i => i.StartWeight / sumStartWeights).ToList();
            var aliasTables = AliasMethod.GenerateAliasTable(normalizedStartWeights).ToList();
            for (int iti = 0; iti < currentIterators.Count; iti++)
            {
                var it = currentIterators[iti];
                //iterators
                its.Add(new IteratorStruct
                {
                    tfId = _registeredTransforms.IndexOf(it.Transform),
                    real_params_index = allRealParams.Count,
                    vec3_params_index = allVec3Params.Count,
                    color_speed = (float)it.ColorSpeed,
                    color_index = (float)it.ColorIndex,
                    opacity = (float)it.Opacity,
                    shading_mode = (int)it.ShadingMode,
                    tf_mix = (float)it.Mix,
                    tf_add = (float)it.Add,
                    reset_alias = aliasTables[iti].k,
                    reset_prob = (float)aliasTables[iti].u
                });
                //collect transform params
                allRealParams.AddRange(it.RealParams.Values.Select(p => (float)p).ToList());
                allVec3Params.AddRange(it.Vec3Params.Values.Select(p => new Vector4(p, 0.0f)).ToList());
            }

            if (its.Count == _iteratorsBufferSize)
                GL.NamedBufferSubData(_iteratorsBufferHandle, 0, _iteratorsBufferSize * Marshal.SizeOf(typeof(IteratorStruct)), its.ToArray());
            else
            {//resize buffer when number of iterators change
                _iteratorsBufferSize = its.Count;
                GL.NamedBufferData(_iteratorsBufferHandle, _iteratorsBufferSize * Marshal.SizeOf(typeof(IteratorStruct)), its.ToArray(), BufferUsageHint.DynamicDraw);
            }

            if (allRealParams.Count == _realParametersBufferSize)
                GL.NamedBufferSubData(_realParametersBufferHandle, 0, _realParametersBufferSize * 4 * sizeof(float), allRealParams.Select(f => new Vector4(f)).ToArray());
            else
            {//resize buffer when number of real parameters change
                _realParametersBufferSize = allRealParams.Count;
                GL.NamedBufferData(_realParametersBufferHandle, _realParametersBufferSize * 4 * sizeof(float), allRealParams.Select(f => new Vector4(f)).ToArray(), BufferUsageHint.DynamicDraw);
            }

            if (allVec3Params.Count == _vec3ParametersBufferSize)
                GL.NamedBufferSubData(_vec3ParametersBufferHandle, 0, _vec3ParametersBufferSize * 4 * sizeof(float), allVec3Params.ToArray());
            else
            {//resize buffer when number of vec3 parameters change
                _vec3ParametersBufferSize = allVec3Params.Count;
                GL.NamedBufferData(_vec3ParametersBufferHandle, _vec3ParametersBufferSize * 4 * sizeof(float), allVec3Params.ToArray(), BufferUsageHint.DynamicDraw);
            }

            //normalize base weights
            double SumWeights = currentIterators.Sum(i => i.BaseWeight);
            var normalizedBaseWeights = currentIterators.ToDictionary(i => i, i => i.BaseWeight / SumWeights);
            var xaosAliasTables = new List<(double u, int k)>();
            foreach (var it in currentIterators)
            {
                var itWeights = new List<double>(currentIterators.Count);
                foreach (var toIt in currentIterators)
                {
                    if (it.WeightTo.TryGetValue(toIt, out var value))//multiply with base weights
                        itWeights.Add(value * normalizedBaseWeights[toIt]);
                    else//fill missing transitions with 0
                        itWeights.Add(0);
                }
                double sumw = itWeights.Sum();
                if (sumw > 0)
                {
                    itWeights = itWeights.Select(w => w / sumw).ToList();//normalize xaos weights
                    xaosAliasTables.AddRange(AliasMethod.GenerateAliasTable(itWeights));
                }
                else
                {//iteration resets here because there are no outgoing weights. Mark this with -1
                    xaosAliasTables.AddRange(Enumerable.Repeat((-1.0, -1), currentIterators.Count));
                }
            }

            //update xaos alias tables
            if (currentIterators.Count == _aliasBufferSize)
                GL.NamedBufferSubData(_aliasBufferHandle, 0, _aliasBufferSize * _aliasBufferSize * sizeof(float) * 4, xaosAliasTables.Select(t => new Vector4((float)t.u, t.k, 0f, 0f)).ToArray());
            else
            {//resize buffer when number of iterators change
                _aliasBufferSize = currentIterators.Count;
                GL.NamedBufferData(_aliasBufferHandle, _aliasBufferSize * _aliasBufferSize * sizeof(float) * 4, xaosAliasTables.Select(t => new Vector4((float)t.u, t.k, 0f, 0f)).ToArray(), BufferUsageHint.DynamicDraw);
            }

            //update palette
            if (LoadedParams.Palette.Colors.Count == _paletteBufferSize)
                GL.NamedBufferSubData(_paletteBufferHandle, 0, _paletteBufferSize * sizeof(float) * 4, LoadedParams.Palette.Colors.ToArray());
            else
            {//resize buffer when number of palette colors change
                _paletteBufferSize = LoadedParams.Palette.Colors.Count;
                GL.NamedBufferData(_paletteBufferHandle, _paletteBufferSize * sizeof(float) * 4, LoadedParams.Palette.Colors.ToArray(), BufferUsageHint.DynamicDraw);
            }

            _invalidParamsBuffer = false;
        }

        //these values can change every dispatch
        GL.Uniform1(GL.GetUniformLocation(_computeProgramHandle, "reset_points_state"), _invalidPointsStateBuffer ? 1 : 0);
        GL.Uniform1(GL.GetUniformLocation(_computeProgramHandle, "dispatch_cnt"), _dispatchCnt);
        GL.Uniform1(GL.GetUniformLocation(_computeProgramHandle, "invocation_iters"), InvocationIters);
        GL.Uniform1(GL.GetUniformLocation(_computeProgramHandle, "seed"), Seed);

        GL.Finish(); //blocking call
        GL.DispatchCompute(WorkgroupCount, 1, 1);

        _invalidPointsStateBuffer = false;
        TotalIterations += Convert.ToUInt64(InvocationIters * InvocationCount);
        _dispatchCnt++;

    }

    public void RenderImage()
    {
        if (!IsInitialized)
            throw NewNotInitializedException();

        GL.UseProgram(_tonemapProgramHandle);
        GL.BindVertexArray(_vao);
        GL.Uniform1(GL.GetUniformLocation(_tonemapProgramHandle, "max_density"), 1 + (uint)(TotalIterations / (uint)(HistogramWidth * HistogramHeight)));//apo:*0.001//draw quad
        GL.Uniform1(GL.GetUniformLocation(_tonemapProgramHandle, "brightness"), (float)LoadedParams.Brightness);
        GL.Uniform1(GL.GetUniformLocation(_tonemapProgramHandle, "inv_gamma"), (float)(1.0f / LoadedParams.Gamma));
        GL.Uniform1(GL.GetUniformLocation(_tonemapProgramHandle, "gamma_threshold"), (float)LoadedParams.GammaThreshold);
        GL.Uniform1(GL.GetUniformLocation(_tonemapProgramHandle, "vibrancy"), (float)LoadedParams.Vibrancy);
        GL.Uniform3(GL.GetUniformLocation(_tonemapProgramHandle, "bg_color"), LoadedParams.BackgroundColor.R / 255.0f, LoadedParams.BackgroundColor.G / 255.0f, LoadedParams.BackgroundColor.B / 255.0f);
        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

        if (EnableDE)
        {
            GL.UseProgram(_deProgramHandle);
            GL.BindVertexArray(_vao);
            GL.Uniform1(GL.GetUniformLocation(_deProgramHandle, "de_max_radius"), (float)DEMaxRadius);
            GL.Uniform1(GL.GetUniformLocation(_deProgramHandle, "de_power"), (float)DEPower);
            GL.Uniform1(GL.GetUniformLocation(_deProgramHandle, "de_threshold"), (float)DEThreshold);
            GL.Uniform1(GL.GetUniformLocation(_deProgramHandle, "max_density"), 1 + (uint)(TotalIterations / (uint)(HistogramWidth * HistogramHeight)));//apo:*0.001
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }
    }

    /// <summary>
    /// Fast write finished image from offscreen framebuffer to the center of the display framebuffer (0) with a small margin.
    /// </summary>
    private void BlitToDisplayFramebuffer()
    {
        float rw = DisplayWidth / (float)HistogramWidth;
        float rh = DisplayHeight / (float)HistogramHeight;
        float rr = (rw < rh ? rw : rh) * .98f;//
        GL.BlitNamedFramebuffer(_offscreenFBOHandle,
            0, 0, 0, HistogramWidth, HistogramHeight,
            (int)(DisplayWidth / 2 - HistogramWidth / 2 * rr),
            (int)(DisplayHeight / 2 - HistogramHeight / 2 * rr),
            (int)(DisplayWidth / 2 + HistogramWidth / 2 * rr),
            (int)(DisplayHeight / 2 + HistogramHeight / 2 * rr),
            ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
    }

    private void AdjustWorkloadSize()
    {
        GL.GetQueryObject(_timerQueryHandle, GetQueryObjectParam.QueryResult, out int timerQueryResultNs); //blocking call
        int actualMs = timerQueryResultNs / 1000000;//ns to ms
        int targetMs = 1000 / TargetFramerate;
        double adjustment = (actualMs > targetMs) ? 0.66 : 1.1;
        int adjustedInvocationIters = (int)(InvocationIters * adjustment);
        InvocationIters = Math.Clamp(adjustedInvocationIters, 50, 20000);
    }

    public void StartRenderLoop()
    {
        if (!IsInitialized)
            throw NewNotInitializedException();

        if (!IsRendering)
        {
            IsRendering = true;

            if (_ctx.IsCurrent)
                _ctx.MakeNoneCurrent();

            ThreadPool.QueueUserWorkItem((_) =>
            {
                _ctx.MakeCurrent();
                while (IsRendering)
                {
                    GL.BeginQuery(QueryTarget.TimeElapsed, _timerQueryHandle);
                    DispatchCompute();//compute the histogram
                    GL.EndQuery(QueryTarget.TimeElapsed);

                    AdjustWorkloadSize();

                    bool target = BitOperations.Log2(1 + TotalIterations / (ulong)(HistogramWidth * HistogramHeight)) >= LoadedParams.TargetIterationLevel;
                    bool isTargetIterationReachedByCurrentDispatch = !IsTargetIterationReached && target;
                    IsTargetIterationReached = target;

                    bool isPerceptuallyEqualFrame = BitOperations.IsPow2(_dispatchCnt);

                    if (_updateDisplayNow || isTargetIterationReachedByCurrentDispatch || (UpdateDisplayOnRender && (!EnablePerceptualUpdates || (EnablePerceptualUpdates && isPerceptuallyEqualFrame))))
                    {
                        //render image from histogram
                        RenderImage();
                        //display the image
                        BlitToDisplayFramebuffer();
                        _ctx.SwapBuffers();
                        _updateDisplayNow = false;
                        DisplayFramebufferUpdated?.Invoke(this, null);
                    }

                    if (isTargetIterationReachedByCurrentDispatch)
                        TargetIterationReached?.Invoke(this, null);
                }
                GL.Finish();
                _ctx.MakeNoneCurrent();
                _stopRender.Set();
            });
        }
    }

    /// <summary>
    /// Wait for the render thread to stop.
    /// </summary>
    public async Task StopRenderLoop()
    {
        if (!IsRendering)
            throw new InvalidOperationException("The render loop is not running.");
        IsRendering = false;
        await _stopRender.WaitAsync();
    }

    /// <summary>
    /// Helper to call opengl from current thread with context. Stops the render thread if context is not current.
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    private async Task WithContext(Action action)
    {
        bool renderingPaused = IsRendering && !_ctx.IsCurrent;
        if (renderingPaused)
            await StopRenderLoop();
        bool wasCurrentContext = _ctx.IsCurrent;
        if (!_ctx.IsCurrent)
            _ctx.MakeCurrent();//acquire
        action();
        if (!wasCurrentContext)
            _ctx.MakeNoneCurrent();//release
        if (renderingPaused)
            StartRenderLoop();//restart render thread if it was running
    }

    /// <summary>
    /// Writes the pixel data to the specified pointer.
    /// The format is ubyte, bgra, which is ideal for filling a bitmap buffer quickly.
    /// </summary>
    /// <param name="ptr">BitmapData.Scan0 or WriteableBitmap.BackBuffer</param>
    /// <remarks>        
    /// The resulting bitmap requires further transformations: 
    /// <list type="bullet">
    /// <item> Image must be flipped vertically </item>
    /// <item> Alpha channel may be removed </item>
    /// </list>
    /// </remarks>
    public async Task CopyPixelDataToBitmap(IntPtr ptr)
    {
        if (!IsInitialized)
            throw NewNotInitializedException();

        InvalidateDisplay();
        await WithContext(() =>
        {
            GL.ReadPixels(0, 0, HistogramWidth, HistogramHeight, PixelFormat.Bgra, PixelType.UnsignedByte, ptr);
        });
    }

    /// <summary>
    /// Format: float[y, x, rgba]
    /// </summary>
    /// 
    /// <remarks>
    /// For large images, GC configuration is required in App.config to avoid <see cref="OutOfMemoryException"/>:
    /// <code>gcAllowVeryLargeObjects</code>
    /// The LargeObjectHeap may be collected manually:
    /// <code>
    /// GCSettings.LargeObjectHeapCompactionMode = CompactOnce;
    /// GC.Collect();
    /// </code>
    /// 
    /// The resulting bitmap requires further transformations: 
    /// <list type="bullet">
    /// <item> Image must be flipped vertically </item>
    /// <item> Alpha channel may be removed </item>
    /// </list>
    /// </remarks>
    public async Task<float[,,]> ReadPixelData()
    {
        if (!IsInitialized)
            throw NewNotInitializedException();

        InvalidateDisplay();
        float[,,] o = new float[HistogramHeight, HistogramWidth, 4];
        await WithContext(() =>
        {
            GL.ReadPixels(0, 0, HistogramWidth, HistogramHeight, PixelFormat.Rgba, PixelType.Float, o);
        });

        return o;
    }

    public async Task<float[][][]> ReadHistogramData()
    {
        if (!IsInitialized)
            throw NewNotInitializedException();

        InvalidateDisplay();
        float[] o2 = new float[HistogramWidth * HistogramHeight * 4];
        await WithContext(() =>
        {
            GL.GetNamedBufferSubData<float>(_histogramBufferHandle, IntPtr.Zero, HistogramWidth * HistogramHeight * 4 * sizeof(float), o2);
            GL.Finish();
        });

        var div = LoadedParams.Brightness * (1 + (TotalIterations / (ulong)(HistogramWidth * HistogramHeight)));
        for (int i = 0; i < o2.Length; i++)
            o2[i] = (float)(o2[i] / div);

        var histogram = o2
            .Chunk(4)
            .Chunk(HistogramWidth)
            .Chunk(HistogramHeight)
            .First().Reverse().ToArray();

        return histogram;
    }

    private void InitDEPass()
    {

        var resource = typeof(RendererGL).GetTypeInfo().Assembly.GetManifestResourceStream(_shadersPath + "de.frag.shader");
        string deShaderSource = new StreamReader(resource).ReadToEnd();
        //compile de shader
        int deShaderH = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(deShaderH, deShaderSource);
        GL.CompileShader(deShaderH);
        GL.GetShader(deShaderH, ShaderParameter.CompileStatus, out int status);
        if (status == 0)
        {
            throw new Exception(
                String.Format("Error compiling {0} shader: {1}", ShaderType.FragmentShader.ToString(), GL.GetShaderInfoLog(deShaderH)));
        }

        _deProgramHandle = GL.CreateProgram();
        GL.AttachShader(_deProgramHandle, _vertexShaderHandle);
        GL.AttachShader(_deProgramHandle, deShaderH);
        GL.LinkProgram(_deProgramHandle);
        GL.GetProgram(_deProgramHandle, GetProgramParameterName.LinkStatus, out status);
        if (status == 0)
        {
            throw new Exception(
                String.Format("Error linking de program: {0}", GL.GetProgramInfoLog(_deProgramHandle)));
        }

        GL.DetachShader(_deProgramHandle, _vertexShaderHandle);
        GL.DetachShader(_deProgramHandle, deShaderH);
        GL.DeleteShader(deShaderH);

        GL.UseProgram(_deProgramHandle);
        GL.Uniform1(GL.GetUniformLocation(_deProgramHandle, "histogram_tex"), 0);

    }

    private void InitTonemapPass()
    {
        var assembly = typeof(RendererGL).GetTypeInfo().Assembly;

        _vertexShaderHandle = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(_vertexShaderHandle, new StreamReader(assembly.GetManifestResourceStream(_shadersPath + "quad.vert.shader")).ReadToEnd());
        GL.CompileShader(_vertexShaderHandle);
        GL.GetShader(_vertexShaderHandle, ShaderParameter.CompileStatus, out int status);
        if (status == 0)
        {
            throw new Exception(
                String.Format("Error compiling {0} shader: {1}", ShaderType.VertexShader.ToString(), GL.GetShaderInfoLog(_vertexShaderHandle)));
        }

        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, new StreamReader(assembly.GetManifestResourceStream(_shadersPath + "tonemap.frag.shader")).ReadToEnd());
        GL.CompileShader(fragmentShader);
        GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out status);
        if (status == 0)
        {
            throw new Exception(
                String.Format("Error compiling {0} shader: {1}", ShaderType.FragmentShader.ToString(), GL.GetShaderInfoLog(fragmentShader)));
        }

        //init display image texture
        _renderTextureHandle = GL.GenTexture();
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _renderTextureHandle);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
        //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

        //TODO: display resolution?
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, HistogramWidth, HistogramHeight, 0, PixelFormat.Rgba, PixelType.Float, new IntPtr(0));

        _offscreenFBOHandle = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _offscreenFBOHandle);//offscreen
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _renderTextureHandle, 0);
        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            throw new Exception("Frame Buffer Error");

        _tonemapProgramHandle = GL.CreateProgram();
        GL.AttachShader(_tonemapProgramHandle, _vertexShaderHandle);
        GL.AttachShader(_tonemapProgramHandle, fragmentShader);
        GL.LinkProgram(_tonemapProgramHandle);
        GL.GetProgram(_tonemapProgramHandle, GetProgramParameterName.LinkStatus, out status);
        if (status == 0)
        {
            throw new Exception(
                String.Format("Error linking program: {0}", GL.GetProgramInfoLog(_tonemapProgramHandle)));
        }

        GL.DetachShader(_tonemapProgramHandle, _vertexShaderHandle);
        GL.DetachShader(_tonemapProgramHandle, fragmentShader);
        GL.DeleteShader(fragmentShader);

    }

    private void InitComputeProgram()
    {
        //load functions
        string transformsSource = "";
        for (int tfIndex = 0; tfIndex < _registeredTransforms.Count; tfIndex++)
        {
            var tf = _registeredTransforms[tfIndex];
            transformsSource += $@"
if (iter.tfId == {tfIndex})
{{
{tf.SourceCode}
}}
";
        }

        //assemble source string
        var resource = typeof(RendererGL).GetTypeInfo().Assembly.GetManifestResourceStream(_shadersPath + "ifs_kernel.comp.shader");
        string computeShaderSource = new StreamReader(resource).ReadToEnd();

        //insert includes
        computeShaderSource = computeShaderSource.Replace("@includes", _includesSource);
        //insert transforms
        computeShaderSource = computeShaderSource.Replace("@transforms", transformsSource);

        //compile compute shader
        int computeShaderH = GL.CreateShader(ShaderType.ComputeShader);
        GL.ShaderSource(computeShaderH, computeShaderSource);
        GL.CompileShader(computeShaderH);
        GL.GetShader(computeShaderH, ShaderParameter.CompileStatus, out int status);
        if (status == 0)
        {
            throw new Exception(
                String.Format("Error compiling {0} shader: {1}", ShaderType.ComputeShader.ToString(), GL.GetShaderInfoLog(computeShaderH)));
        }

        //free previous resources
        if (_computeProgramHandle != 0)
            GL.DeleteProgram(_computeProgramHandle);
        //build shader program
        _computeProgramHandle = GL.CreateProgram();
        GL.AttachShader(_computeProgramHandle, computeShaderH);
        GL.LinkProgram(_computeProgramHandle);
        GL.GetProgram(_computeProgramHandle, GetProgramParameterName.LinkStatus, out status);
        if (status == 0)
        {
            throw new Exception(
                String.Format("Error linking de program: {0}", GL.GetProgramInfoLog(_computeProgramHandle)));
        }
        GL.DetachShader(_computeProgramHandle, computeShaderH);
        GL.DeleteShader(computeShaderH);

        GL.UseProgram(_computeProgramHandle);
        GL.Uniform1(GL.GetUniformLocation(_computeProgramHandle, "width"), HistogramWidth);
        GL.Uniform1(GL.GetUniformLocation(_computeProgramHandle, "height"), HistogramHeight);

    }

    private void InitBuffers()
    {
        //create buffers
        _settingsBufferHandle = GL.GenBuffer();
        _histogramBufferHandle = GL.GenBuffer();
        _pointsBufferHandle = GL.GenBuffer();
        _iteratorsBufferHandle = GL.GenBuffer();
        _aliasBufferHandle = GL.GenBuffer();
        _paletteBufferHandle = GL.GenBuffer();
        _realParametersBufferHandle = GL.GenBuffer();
        _vec3ParametersBufferHandle = GL.GenBuffer();

        //bind and allocate fixed-size buffers
        GL.BindBuffer(BufferTarget.UniformBuffer, _settingsBufferHandle);
        GL.BufferData(BufferTarget.UniformBuffer, Marshal.SizeOf(typeof(SettingsStruct)), IntPtr.Zero, BufferUsageHint.StreamDraw);

        //bind varying-size buffers to targets, without allocation
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _histogramBufferHandle);
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _pointsBufferHandle);
        GL.BindBuffer(BufferTarget.UniformBuffer, _iteratorsBufferHandle);
        GL.BindBuffer(BufferTarget.UniformBuffer, _aliasBufferHandle);
        GL.BindBuffer(BufferTarget.UniformBuffer, _paletteBufferHandle);
        GL.BindBuffer(BufferTarget.UniformBuffer, _realParametersBufferHandle);
        GL.BindBuffer(BufferTarget.UniformBuffer, _vec3ParametersBufferHandle);

        //bind buffers to layout:
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, _histogramBufferHandle);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, _pointsBufferHandle);
        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 2, _settingsBufferHandle);
        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 3, _iteratorsBufferHandle);
        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 4, _aliasBufferHandle);
        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 5, _paletteBufferHandle);
        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 6, _realParametersBufferHandle);
        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 7, _vec3ParametersBufferHandle);

    }

    private static InvalidOperationException NewNotInitializedException()
    {
        return new InvalidOperationException("Renderer is not initialized.");
    }

    public async ValueTask DisposeAsync()
    {
        DisplayFramebufferUpdated = null;
        if (IsInitialized)
        {
            if (IsRendering)
                await StopRenderLoop();
            if (_debugFlag)
                _debugProcCallbackHandle.Free();
            //TODO: dispose buffers

            GL.DeleteQuery(_timerQueryHandle);
        }
        IsInitialized = false;
    }

}
