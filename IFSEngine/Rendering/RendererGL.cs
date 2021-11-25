using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;

using IFSEngine.Model;
using IFSEngine.Rendering.GpuStructs;
using IFSEngine.Animation;
using IFSEngine.Utility;
using System.Numerics;
using System.Reflection;

namespace IFSEngine.Rendering
{
    public sealed class RendererGL : IDisposable
    {
        public event EventHandler DisplayFramebufferUpdated;

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
        /// Enable Epic's Temporal Anti-Aliasing.
        /// </summary>
        public bool EnableTAA { get; set; } = false;

        /// <summary>
        /// Number of dispatches since accumulation reset.
        /// This is needed for random generation.
        /// </summary>
        private int dispatchCnt = 0;

        public int HistogramWidth { get; private set; } = 1920;
        public int HistogramHeight { get; private set; } = 1080;
        public int DisplayWidth { get; private set; } = 1280;
        public int DisplayHeight { get; private set; } = 720;

        private List<Transform> registeredTransforms;

        public AnimationManager AnimationManager { get; set; }//TODO: Remove

        public IFS LoadedParams { get; private set; } = new IFS();
        private bool invalidHistogramResolution = false;
        private bool invalidHistogramBuffer = false;
        private bool invalidParamsBuffer = false;
        private bool invalidPointsStateBuffer = false;

        /// <summary>
        /// Number of workgroups to be dispatched. Each workgroup consists of 64 kernel invocations. Default value is 256.
        /// </summary>
        public int WorkgroupCount { get; private set; } = 256;
        public async Task SetWorkgroupCount(int s)
        {
            WorkgroupCount = s;
            await WithContext(() =>
            {
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, pointsBufferHandle);
                GL.BufferData(BufferTarget.ShaderStorageBuffer, InvocationCount * (4 * sizeof(float)) + 2 * sizeof(float) + 2 * sizeof(int), IntPtr.Zero, BufferUsageHint.StaticCopy);

                InvalidatePointsStateBuffer();
            });
        }

        private const int workgroupSize = 64;//nv:32, amd:64. Optimal is 64.

        public int InvocationCount => WorkgroupCount * workgroupSize;

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

        private bool updateDisplayNow = false;
        private readonly IGraphicsContext ctx;

        private int vertexShaderHandle;
        private int vao;
        //compute shader handles
        private int computeProgramHandle;
        private int histogramBufferHandle;
        private int settingsBufferHandle;
        private int iteratorsBufferHandle;
        private int aliasBufferHandle;
        private int pointsBufferHandle;
        private int paletteBufferHandle;
        private int realParametersBufferHandle;
        private int vec3ParametersBufferHandle;
        //fragment shader handles
        private int tonemapProgramHandle;
        private int deProgramHandle;
        private int taaProgramHandle;
        private int offscreenFBOHandle;
        private int renderTextureHandle;
        private int taaTextureHandle;

        private readonly AutoResetEvent stopRender = new(false); 
        private readonly float[] bufferClearColor = new float[] { 0.0f, 0.0f, 0.0f };
        private readonly string shadersPath = "IFSEngine.Rendering.Shaders.";
        private readonly bool debugFlag = false;

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

            Console.WriteLine($"{severity} {type} | {messageString}");

            if (type == DebugType.DebugTypeError)
            {
                throw new Exception(messageString);
            }
        }
        private static readonly DebugProc _debugProcCallback = DebugCallback;
        private static GCHandle _debugProcCallbackHandle;
        private int timerQueryHandle;

        /// <summary>
        /// Creates a new renderer instance.
        /// <see cref="Initialize"/> must be called before starting the render loop.
        /// </summary>
        /// <param name="ctx"></param>
        public RendererGL(IGraphicsContext ctx)
        {
            this.ctx = ctx;
        }

        public void Initialize(IEnumerable<Transform> transforms)
        {
            if (IsInitialized)
                throw new InvalidOperationException("Renderer is already initialized.");

            if (debugFlag)
            {
                _debugProcCallbackHandle = GCHandle.Alloc(_debugProcCallback);
                GL.DebugMessageCallback(_debugProcCallback, IntPtr.Zero);
                GL.Enable(EnableCap.DebugOutput);
                GL.Enable(EnableCap.DebugOutputSynchronous);
            }

            registeredTransforms = transforms.ToList();

            //attributeless rendering
            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);
            //empty vao

            InitBuffers();
            InitTonemapPass();
            InitDEPass();
            InitComputeProgram();
            InitTAAPass();
            GL.DeleteShader(vertexShaderHandle);

            timerQueryHandle = GL.GenQuery();

            SetHistogramScaleToDisplay();

            IsInitialized = true;
            SetWorkgroupCount(WorkgroupCount).Wait();

            InvalidateParamsBuffer();
        }

        public async Task LoadTransforms(IEnumerable<Transform> transforms)
        {
            if(!IsInitialized)
                throw NewNotInitializedException();

            await WithContext(() =>
            {
                registeredTransforms = transforms.ToList();
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
            invalidParamsBuffer = true;
        }

        /// <summary>
        /// Invalidates the data in the histogram buffer, which causes the render thread to empty it.
        /// Also invalidates the state buffer.
        /// Usually called whenever the <see cref="Camera"/> parameters are changed.
        /// </summary>
        public void InvalidateHistogramBuffer()
        {
            InvalidatePointsStateBuffer();
            invalidHistogramBuffer = true;
        }

        public void InvalidateDisplay()
        {
            updateDisplayNow = true;
        }

        private void InvalidatePointsStateBuffer()
        {
            invalidPointsStateBuffer = true;
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
                invalidHistogramResolution = true;
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

            GL.UseProgram(computeProgramHandle);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, histogramBufferHandle);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, HistogramWidth * HistogramHeight * 4 * sizeof(float), IntPtr.Zero, BufferUsageHint.StaticCopy);
            //resize display texture. TODO: separate & use display resolution
            GL.Uniform1(GL.GetUniformLocation(computeProgramHandle, "width"), HistogramWidth);
            GL.Uniform1(GL.GetUniformLocation(computeProgramHandle, "height"), HistogramHeight);
            GL.UseProgram(tonemapProgramHandle);
            GL.Uniform1(GL.GetUniformLocation(tonemapProgramHandle, "width"), HistogramWidth);
            GL.Uniform1(GL.GetUniformLocation(tonemapProgramHandle, "height"), HistogramHeight);
            GL.UseProgram(deProgramHandle);
            GL.Uniform1(GL.GetUniformLocation(deProgramHandle, "width"), HistogramWidth);
            GL.Uniform1(GL.GetUniformLocation(deProgramHandle, "height"), HistogramHeight);
            GL.UseProgram(taaProgramHandle);
            GL.Uniform1(GL.GetUniformLocation(taaProgramHandle, "width"), HistogramWidth);
            GL.Uniform1(GL.GetUniformLocation(taaProgramHandle, "height"), HistogramHeight);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, renderTextureHandle);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, HistogramWidth, HistogramHeight, 0, PixelFormat.Rgba, PixelType.Float, new IntPtr(0));
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, taaTextureHandle);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, HistogramWidth, HistogramHeight, 0, PixelFormat.Rgba, PixelType.Float, new IntPtr(0));
            
            GL.Viewport(0, 0, HistogramWidth, HistogramHeight);

            GL.ClearBuffer(ClearBuffer.Color, 0, bufferClearColor);
            ctx.SwapBuffers();//clear back buffer
            GL.ClearBuffer(ClearBuffer.Color, 0, bufferClearColor);
            DisplayFramebufferUpdated?.Invoke(this, null);

            invalidHistogramResolution = false;
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

            GL.UseProgram(computeProgramHandle);

            if (invalidHistogramBuffer)
            {
                if (invalidHistogramResolution)
                {
                    UpdateHistogramResolution();
                    GL.UseProgram(computeProgramHandle);
                }

                //reset accumulation
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, histogramBufferHandle);
                GL.ClearNamedBufferData(histogramBufferHandle, PixelInternalFormat.R32f, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
                invalidHistogramBuffer = false;
                dispatchCnt = 0;
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
                    max_filter_radius = MaxFilterRadius
                };
                GL.BindBuffer(BufferTarget.UniformBuffer, settingsBufferHandle);
                GL.BufferData(BufferTarget.UniformBuffer, Marshal.SizeOf(typeof(SettingsStruct)), ref settings, BufferUsageHint.StreamDraw);

            }

            if (invalidParamsBuffer)
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
                        tfId = registeredTransforms.IndexOf(it.Transform),
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

                GL.BindBuffer(BufferTarget.UniformBuffer, iteratorsBufferHandle);
                GL.BufferData(BufferTarget.UniformBuffer, its.Count * Marshal.SizeOf(typeof(IteratorStruct)), its.ToArray(), BufferUsageHint.DynamicDraw);

                GL.BindBuffer(BufferTarget.UniformBuffer, realParametersBufferHandle);
                GL.BufferData(BufferTarget.UniformBuffer, allRealParams.Count * sizeof(float), allRealParams.ToArray(), BufferUsageHint.DynamicDraw);

                GL.BindBuffer(BufferTarget.UniformBuffer, vec3ParametersBufferHandle);
                GL.BufferData(BufferTarget.UniformBuffer, allVec3Params.Count * 4 * sizeof(float), allVec3Params.ToArray(), BufferUsageHint.DynamicDraw);

                //normalize base weights
                double SumWeights = currentIterators.Sum(i => i.BaseWeight);
                var normalizedBaseWeights = currentIterators.ToDictionary(i => i, i => i.BaseWeight / SumWeights);
                var xaosAliasTables = new List<(double u, int k)>();
                foreach (var it in currentIterators)
                {
                    var itWeights = new List<double>(currentIterators.Count);
                    foreach (var toIt in currentIterators)
                    {
                        if (it.WeightTo.ContainsKey(toIt))//multiply with base weights
                            itWeights.Add(it.WeightTo[toIt] * normalizedBaseWeights[toIt]);
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
                GL.BindBuffer(BufferTarget.UniformBuffer, aliasBufferHandle);
                GL.BufferData(BufferTarget.UniformBuffer, currentIterators.Count * currentIterators.Count * sizeof(float) * 4, xaosAliasTables.Select(t => new Vector4((float)t.u, t.k, 0f, 0f)).ToArray(), BufferUsageHint.DynamicDraw);

                //update palette
                GL.BindBuffer(BufferTarget.UniformBuffer, paletteBufferHandle);
                GL.BufferData(BufferTarget.UniformBuffer, LoadedParams.Palette.Colors.Count * sizeof(float) * 4, LoadedParams.Palette.Colors.ToArray(), BufferUsageHint.DynamicDraw);

                invalidParamsBuffer = false;
            }

            //these values can change every dispatch
            GL.Uniform1(GL.GetUniformLocation(computeProgramHandle, "reset_points_state"), invalidPointsStateBuffer ? 1 : 0);
            GL.Uniform1(GL.GetUniformLocation(computeProgramHandle, "dispatch_cnt"), dispatchCnt);
            GL.Uniform1(GL.GetUniformLocation(computeProgramHandle, "invocation_iters"), InvocationIters);

            GL.Finish(); //blocking call
            GL.DispatchCompute(WorkgroupCount, 1, 1);

            invalidPointsStateBuffer = false;
            TotalIterations += Convert.ToUInt64(InvocationIters * InvocationCount);
            dispatchCnt++;

        }

        public void RenderImage()
        {
            if (!IsInitialized)
                throw NewNotInitializedException();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, offscreenFBOHandle);//

            GL.UseProgram(tonemapProgramHandle);
            GL.BindVertexArray(vao);
            GL.Uniform1(GL.GetUniformLocation(tonemapProgramHandle, "max_density"), 1 + (uint)(TotalIterations / (uint)(HistogramWidth * HistogramHeight)));//apo:*0.001//draw quad
            GL.Uniform1(GL.GetUniformLocation(tonemapProgramHandle, "brightness"), (float)LoadedParams.Brightness);
            GL.Uniform1(GL.GetUniformLocation(tonemapProgramHandle, "inv_gamma"), (float)(1.0f / LoadedParams.Gamma));
            GL.Uniform1(GL.GetUniformLocation(tonemapProgramHandle, "gamma_threshold"), (float)LoadedParams.GammaThreshold);
            GL.Uniform1(GL.GetUniformLocation(tonemapProgramHandle, "vibrancy"), (float)LoadedParams.Vibrancy);
            GL.Uniform3(GL.GetUniformLocation(tonemapProgramHandle, "bg_color"), LoadedParams.BackgroundColor.R / 255.0f, LoadedParams.BackgroundColor.G / 255.0f, LoadedParams.BackgroundColor.B / 255.0f);
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            if (EnableDE)
            {
                GL.UseProgram(deProgramHandle);
                GL.BindVertexArray(vao);
                GL.Uniform1(GL.GetUniformLocation(deProgramHandle, "de_max_radius"), (float)DEMaxRadius);
                GL.Uniform1(GL.GetUniformLocation(deProgramHandle, "de_power"), (float)DEPower);
                GL.Uniform1(GL.GetUniformLocation(deProgramHandle, "de_threshold"), (float)DEThreshold);
                GL.Uniform1(GL.GetUniformLocation(deProgramHandle, "max_density"), 1 + (uint)(TotalIterations / (uint)(HistogramWidth * HistogramHeight)));//apo:*0.001
                GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
            }

            if (EnableTAA)
            {
                GL.UseProgram(taaProgramHandle);
                GL.BindVertexArray(vao);
                GL.Uniform1(GL.GetUniformLocation(taaProgramHandle, "width"), HistogramWidth);
                GL.Uniform1(GL.GetUniformLocation(taaProgramHandle, "height"), HistogramHeight);
                GL.Uniform1(GL.GetUniformLocation(taaProgramHandle, "new_frame_tex"), 0);
                GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);
                GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
            }
        }

        private void BlitToDisplayFramebuffer()
        {
            float rw = DisplayWidth / (float)HistogramWidth;
            float rh = DisplayHeight / (float)HistogramHeight;
            float rr = (rw < rh ? rw : rh) * .98f;//
            GL.BlitNamedFramebuffer(offscreenFBOHandle,
                0, 0, 0, HistogramWidth, HistogramHeight,
                (int)(DisplayWidth / 2 - HistogramWidth / 2 * rr),
                (int)(DisplayHeight / 2 - HistogramHeight / 2 * rr),
                (int)(DisplayWidth / 2 + HistogramWidth / 2 * rr),
                (int)(DisplayHeight / 2 + HistogramHeight / 2 * rr),
                ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
            //GL.CopyImageSubData(dispTexH, ImageTarget.Texture2D, 1, 0, 0, 0, 0, ImageTarget.Texture2D, 1, dw / 2 - Width / 2, dh / 2 - Height / 2, dw, dh, Height, Width);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        private void AdjustWorkloadSize()
        {
            GL.GetQueryObject(timerQueryHandle, GetQueryObjectParam.QueryResult, out int timerQueryResultNs); //blocking call
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

                if(ctx.IsCurrent)
                    ctx.MakeNoneCurrent();

                new Thread(() =>
                {
                    ctx.MakeCurrent();
                    while (IsRendering)
                    {
                        GL.BeginQuery(QueryTarget.TimeElapsed, timerQueryHandle);
                        DispatchCompute();//compute the histogram
                        GL.EndQuery(QueryTarget.TimeElapsed);

                        AdjustWorkloadSize();

                        bool isPerceptuallyEqualFrame = BitOperations.IsPow2(dispatchCnt);
                        if (updateDisplayNow || (UpdateDisplayOnRender && (!EnablePerceptualUpdates || (EnablePerceptualUpdates && isPerceptuallyEqualFrame))))
                        {
                            //render image from histogram
                            RenderImage();
                            //display the image
                            BlitToDisplayFramebuffer();
                            ctx.SwapBuffers();
                            DisplayFramebufferUpdated?.Invoke(this, null);
                            updateDisplayNow = false;
                        }
                    }
                    GL.Finish();
                    ctx.MakeNoneCurrent();
                    stopRender.Set();
                }).Start();
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
            stopRender.WaitOne(); //TODO: find async solution
        }

        /// <summary>
        /// Helper to call opengl from current thread with context. Stops the render thread.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private async Task WithContext(Action action)
        {
            bool continueRendering = IsRendering;
            if(IsRendering)
                await StopRenderLoop();
            bool wasCurrentContext = ctx.IsCurrent;
            if(!ctx.IsCurrent)
                ctx.MakeCurrent();//acquire
            action();
            if (!wasCurrentContext)
                ctx.MakeNoneCurrent();//release
            if (continueRendering)
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
            await WithContext(() => {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, offscreenFBOHandle);
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
            await WithContext(() => {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, offscreenFBOHandle);
                GL.ReadPixels(0, 0, HistogramWidth, HistogramHeight, PixelFormat.Rgba, PixelType.Float, o);
            });

            return o;
        }

        public async Task<float[,,]> ReadHistogramData()
        {
            if (!IsInitialized)
                throw NewNotInitializedException();

            InvalidateDisplay();
            float[,,] o = new float[HistogramHeight, HistogramWidth, 4];
            await WithContext(() => {
                GL.GetNamedBufferSubData<float>(histogramBufferHandle, IntPtr.Zero, HistogramWidth * HistogramHeight * 4 * sizeof(float), o);
                GL.Finish();
            });

            return o;
        }

        private void InitTAAPass()
        {

            var resource = typeof(RendererGL).GetTypeInfo().Assembly.GetManifestResourceStream(shadersPath + "taa.frag.shader");
            string taaShaderSource = new StreamReader(resource).ReadToEnd();
            //compile taa shader
            int taaShaderH = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(taaShaderH, taaShaderSource);
            GL.CompileShader(taaShaderH);
            GL.GetShader(taaShaderH, ShaderParameter.CompileStatus, out int status);
            if (status == 0)
            {
                throw new Exception(
                    String.Format("Error compiling {0} shader: {1}", ShaderType.FragmentShader.ToString(), GL.GetShaderInfoLog(taaShaderH)));
            }

            //init taa image texture
            taaTextureHandle = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture2);//1
            GL.BindTexture(TextureTarget.Texture2D, taaTextureHandle);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            //TODO: display resolution?
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, HistogramWidth, HistogramHeight, 0, PixelFormat.Rgba, PixelType.Float, new IntPtr(0));
            GL.BindImageTexture(0, taaTextureHandle, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);

            taaProgramHandle = GL.CreateProgram();
            GL.AttachShader(taaProgramHandle, vertexShaderHandle);
            GL.AttachShader(taaProgramHandle, taaShaderH);
            GL.LinkProgram(taaProgramHandle);
            GL.GetProgram(taaProgramHandle, GetProgramParameterName.LinkStatus, out status);
            if (status == 0)
            {
                throw new Exception(
                    String.Format("Error linking taa program: {0}", GL.GetProgramInfoLog(taaProgramHandle)));
            }

            GL.DetachShader(taaProgramHandle, vertexShaderHandle);
            GL.DetachShader(taaProgramHandle, taaShaderH);
            GL.DeleteShader(vertexShaderHandle);
            GL.DeleteShader(taaShaderH);

            GL.UseProgram(taaProgramHandle);

            GL.Uniform1(GL.GetUniformLocation(taaProgramHandle, "new_frame_tex"), 0);
        }

        private void InitDEPass()
        {

            var resource = typeof(RendererGL).GetTypeInfo().Assembly.GetManifestResourceStream(shadersPath + "de.frag.shader");
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

            deProgramHandle = GL.CreateProgram();
            GL.AttachShader(deProgramHandle, vertexShaderHandle);
            GL.AttachShader(deProgramHandle, deShaderH);
            GL.LinkProgram(deProgramHandle);
            GL.GetProgram(deProgramHandle, GetProgramParameterName.LinkStatus, out status);
            if (status == 0)
            {
                throw new Exception(
                    String.Format("Error linking de program: {0}", GL.GetProgramInfoLog(deProgramHandle)));
            }

            GL.DetachShader(deProgramHandle, vertexShaderHandle);
            GL.DetachShader(deProgramHandle, deShaderH);
            GL.DeleteShader(deShaderH);

            GL.UseProgram(deProgramHandle);
            GL.Uniform1(GL.GetUniformLocation(deProgramHandle, "histogram_tex"), 0);

        }

        private void InitTonemapPass()
        {
            var assembly = typeof(RendererGL).GetTypeInfo().Assembly;

            vertexShaderHandle = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShaderHandle, new StreamReader(assembly.GetManifestResourceStream(shadersPath + "quad.vert.shader")).ReadToEnd());
            GL.CompileShader(vertexShaderHandle);
            GL.GetShader(vertexShaderHandle, ShaderParameter.CompileStatus, out int status);
            if (status == 0)
            {
                throw new Exception(
                    String.Format("Error compiling {0} shader: {1}", ShaderType.VertexShader.ToString(), GL.GetShaderInfoLog(vertexShaderHandle)));
            }

            var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, new StreamReader(assembly.GetManifestResourceStream(shadersPath + "tonemap.frag.shader")).ReadToEnd());
            GL.CompileShader(fragmentShader);
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out status);
            if (status == 0)
            {
                throw new Exception(
                    String.Format("Error compiling {0} shader: {1}", ShaderType.FragmentShader.ToString(), GL.GetShaderInfoLog(fragmentShader)));
            }
            
            //init display image texture
            renderTextureHandle = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, renderTextureHandle);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

            //TODO: display resolution?
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, HistogramWidth, HistogramHeight, 0, PixelFormat.Rgba, PixelType.Float, new IntPtr(0));

            offscreenFBOHandle = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, offscreenFBOHandle);//offscreen
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, renderTextureHandle, 0);
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
                throw new Exception("Frame Buffer Error");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);//screen

            tonemapProgramHandle = GL.CreateProgram();
            GL.AttachShader(tonemapProgramHandle, vertexShaderHandle);
            GL.AttachShader(tonemapProgramHandle, fragmentShader);
            GL.LinkProgram(tonemapProgramHandle);
            GL.GetProgram(tonemapProgramHandle, GetProgramParameterName.LinkStatus, out status);
            if (status == 0)
            {
                throw new Exception(
                    String.Format("Error linking program: {0}", GL.GetProgramInfoLog(tonemapProgramHandle)));
            }

            GL.DetachShader(tonemapProgramHandle, vertexShaderHandle);
            GL.DetachShader(tonemapProgramHandle, fragmentShader);
            GL.DeleteShader(fragmentShader);

        }

        private void InitComputeProgram()
        {
            //load functions
            string transformsSource = "";
            for(int tfIndex = 0; tfIndex < registeredTransforms.Count; tfIndex++)
            {
                var tf = registeredTransforms[tfIndex];
                transformsSource += $@"
if (iter.tfId == {tfIndex})
{{
{tf.SourceCode}
}}
";
            }

            //assemble source string
            var resource = typeof(RendererGL).GetTypeInfo().Assembly.GetManifestResourceStream(shadersPath + "ifs_kernel.comp.shader");
            string computeShaderSource = new StreamReader(resource).ReadToEnd();

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
            if (computeProgramHandle != 0)
                GL.DeleteProgram(computeProgramHandle);
            //build shader program
            computeProgramHandle = GL.CreateProgram();
            GL.AttachShader(computeProgramHandle, computeShaderH);
            GL.LinkProgram(computeProgramHandle);
            GL.GetProgram(computeProgramHandle, GetProgramParameterName.LinkStatus, out status);
            if (status == 0)
            {
                throw new Exception(
                    String.Format("Error linking de program: {0}", GL.GetProgramInfoLog(computeProgramHandle)));
            }
            GL.DetachShader(computeProgramHandle, computeShaderH);
            GL.DeleteShader(computeShaderH);

            GL.UseProgram(computeProgramHandle);
            GL.Uniform1(GL.GetUniformLocation(computeProgramHandle, "width"), HistogramWidth);
            GL.Uniform1(GL.GetUniformLocation(computeProgramHandle, "height"), HistogramHeight);

        }

        private void InitBuffers()
        {
            //create buffers
            histogramBufferHandle = GL.GenBuffer();
            pointsBufferHandle = GL.GenBuffer();
            settingsBufferHandle = GL.GenBuffer();
            iteratorsBufferHandle = GL.GenBuffer();
            aliasBufferHandle = GL.GenBuffer();
            paletteBufferHandle = GL.GenBuffer();
            realParametersBufferHandle = GL.GenBuffer();
            vec3ParametersBufferHandle = GL.GenBuffer();

            //bind layout:
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, histogramBufferHandle);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, pointsBufferHandle);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 2, settingsBufferHandle);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 3, iteratorsBufferHandle);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 4, aliasBufferHandle);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 5, paletteBufferHandle);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 6, realParametersBufferHandle);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 7, vec3ParametersBufferHandle);

        }

        private static InvalidOperationException NewNotInitializedException()
        {
            return new InvalidOperationException("Renderer is not initialized.");
        }

        public void Dispose()
        {
            DisplayFramebufferUpdated = null;
            if (IsInitialized)
            {
                if (IsRendering)
                    StopRenderLoop().Wait();
                if (debugFlag)
                    _debugProcCallbackHandle.Free();
                //TODO: dispose buffers

                GL.DeleteQuery(timerQueryHandle);
            }
            IsInitialized = false;
        }

    }
}
