using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using IFSEngine.Animation;

//using OpenGL;
using OpenTK;
using OpenTK.Graphics.OpenGL;

using IFSEngine.Model;
using System.ComponentModel;
using IFSEngine.Model.GpuStructs;
using OpenTK.Graphics;
using OpenTK.Platform;
using System.Threading.Tasks;
using IFSEngine.TransformFunctions;

namespace IFSEngine
{
    public class RendererGL
    {
        public event EventHandler DisplayFrameCompleted;
        //public event EventHandler RenderFrameCompleted;

        public bool UpdateDisplayOnRender { get; set; } = true;

        /// <summary>
        /// Enable perceptually equal difference between updates.
        /// </summary>
        public bool EnablePerceptualUpdates { get; set; } = true;

        /// <summary>
        /// Enable Density Estimation.
        /// </summary>
        public bool EnableDE { get; set; } = false;

        /// <summary>
        /// Enable Epic's Temporal Anti-Aliasing.
        /// </summary>
        public bool EnableTAA { get; set; } = false;

        /// <summary>
        /// Number of dispatches since accumulation reset.
        /// This is needed for random generation and 0. dispatch reset
        /// </summary>
        private int dispatchCnt = 0;

        public int HistogramWidth { get; private set; } = 1920;
        public int HistogramHeight { get; private set; } = 1080;
        public int DisplayWidth { get; private set; } = 1280;
        public int DisplayHeight { get; private set; } = 720;


        public IFS CurrentParams { get; private set; }


        public AnimationManager AnimationManager { get; set; }

        private bool invalidAccumulation = false;
        private bool invalidParams = false;

        /// <summary>
        /// TODO: make this adaptive
        /// </summary>
        public int workgroupCount { get; set; } = 100;

        private const int workgroupSize = 64;//nv:32, amd:64. Optimal is 64.

        public int InvocationCount => workgroupCount * workgroupSize;

        /// <summary>
        /// Performance setting: Number of iterations per dispatch.
        /// TODO: adaptive? depends on hardware.
        /// </summary>
        public int PassIters { get; set; } = 500;

        /// <summary>
        /// Number of iterations between resetting points.
        /// Apo/Chaotica: const 10000
        /// Zueuk: max 500 enough
        /// TODO: adaptive possible? Reset earlier if ... ?
        /// Gradually increase MaxIters? x
        /// Make it an option for high quality renders?
        /// TODO: move reset to compute shader? adaptive for each thread
        /// </summary>
        private int MaxIters { get; set; } = 1000;

        private int PassItersCnt = 0;

        /// <summary>
        /// Total iterations since accumulation reset
        /// </summary>
        public ulong TotalIterations { get; private set; } = 0;

        private bool updateDisplayNow = false;
        private bool rendering = false;


        private IGraphicsContext ctx;//add public setter?
        private IWindowInfo wInfo;//add public setter?

        //compute handlers
        private int computeProgramH;
        private int histogramH;
        private int settingsbufH;
        private int itersbufH;
        private int pointsbufH;
        private int palettebufH;
        private int tfparamsbufH;
        private int xaosbufH;
        private int last_tf_index_bufH;
        //display handlers
        private int fboDispH;
        private int dispTexH;
        private int displayProgramH;


        public RendererGL(IGraphicsContext ctx, IWindowInfo wInfo)
        {
            this.ctx = ctx;
            this.wInfo = wInfo;

            AnimationManager = new AnimationManager();

            LoadParams(IFS.GenerateRandom());

            //TODO: separate opengl initialization from ctor
            initDisplay();
            initRenderer();
            initTAA();

            SetHistogramScale(1.0);

        }

        public void LoadParams(IFS p)
        {
            if (CurrentParams != null)
                CurrentParams.ViewSettings.PropertyChanged -= HandleInvalidation;
            CurrentParams = p;
            CurrentParams.ViewSettings.PropertyChanged += HandleInvalidation;
            InvalidateParams();
        }

        private void HandleInvalidation(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "1":
                    InvalidateAccumulation();
                    break;
                case "2":
                    InvalidateParams();
                    break;
                case "0":
                default:
                    break;

            }
        }

        public void InvalidateAccumulation()
        {
            //can be called multiple times, but it's enough to reset once before first frame
            invalidAccumulation = true;

        }
        public void InvalidateParams()
        {
            //can be called multiple times, but it's enough to reset once before first frame
            InvalidateAccumulation();
            invalidParams = true;
        }

        public void SetHistogramScale(double scale)
        {
            WithContext(() =>
            {
                HistogramWidth = (int)(CurrentParams.ViewSettings.ImageResolution.Width * scale);
                HistogramHeight = (int)(CurrentParams.ViewSettings.ImageResolution.Height * scale);
                GL.UseProgram(computeProgramH);
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, histogramH);
                GL.BufferData(BufferTarget.ShaderStorageBuffer, HistogramWidth * HistogramHeight * 4 * sizeof(float), IntPtr.Zero, BufferUsageHint.StaticCopy);
                GL.Uniform1(GL.GetUniformLocation(computeProgramH, "width"), HistogramWidth);
                GL.Uniform1(GL.GetUniformLocation(computeProgramH, "height"), HistogramHeight);

                //resize display texture. TODO: separate & use display resolution
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, dispTexH);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, HistogramWidth, HistogramHeight, 0, PixelFormat.Rgba, PixelType.Float, new IntPtr(0));
                //TODO: GL.ClearTexImage(dispTexH, 0, PixelFormat.Rgba, PixelType.Float, ref clear_value);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, taaTexH);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, HistogramWidth, HistogramHeight, 0, PixelFormat.Rgba, PixelType.Float, new IntPtr(0));



                GL.Viewport(0, 0, HistogramWidth, HistogramHeight);
                InvalidateAccumulation();
            }).Wait();

        }

        public void SetDisplayResolution(int displayWidth, int displayHeight)
        {
            this.DisplayWidth = displayWidth;
            this.DisplayHeight = displayHeight;
        }

        private void UpdatePointsState()
        {
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, pointsbufH);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, InvocationCount * 4 * sizeof(float), StartingDistributions.UniformUnitCube(InvocationCount), BufferUsageHint.DynamicDraw);
        }

        public void RenderFrame()
        {
            GL.UseProgram(computeProgramH);
            if (invalidAccumulation)
            {
                //reset accumulation
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, histogramH);
                GL.ClearNamedBufferData(histogramH, PixelInternalFormat.R32f, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
                invalidAccumulation = false;
                dispatchCnt = 0;
                TotalIterations = 0;

                if (invalidParams)
                {
                    UpdatePointsState();

                    //update iterators
                    //generate iterator and transform structs
                    List<IteratorStruct> its = new List<IteratorStruct>();
                    List<float> tfsparams = new List<float>();
                    foreach (var it in CurrentParams.Iterators)
                    {
                        //iterators
                        its.Add(new IteratorStruct
                        {
                            tfId = it.Transform.Id,
                            tfParamsStart = tfsparams.Count,
                            wsum = (float)it.WeightTo.Sum(xw => xw.Value * xw.Key.baseWeight),
                            cs = (float)it.cs,
                            ci = (float)it.ci,
                            op = (float)it.op,
                        });
                        //transform params
                        IEnumerable<double> tfiparams = it.Transform.GetFunctionVariables().Select(p => it.Transform.GetVar<double>(p));// GetListOfParams();
                        tfsparams.AddRange(tfiparams.Select(p => (float)p));
                    }
                    //TODO: tfparamstart pop last value

                    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, itersbufH);
                    GL.BufferData(BufferTarget.ShaderStorageBuffer, its.Count * (4 * sizeof(int) + 4 * sizeof(float)), its.ToArray(), BufferUsageHint.DynamicDraw);

                    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, tfparamsbufH);
                    GL.BufferData(BufferTarget.ShaderStorageBuffer, tfsparams.Count * sizeof(float), tfsparams.ToArray(), BufferUsageHint.DynamicDraw);

                    //generate flattened xaos weight matrix
                    List<float> xaosm = new List<float>(CurrentParams.Iterators.Count * CurrentParams.Iterators.Count);
                    foreach (var it in CurrentParams.Iterators)
                    {
                        foreach (var toIt in CurrentParams.Iterators)
                        {
                            if (it.WeightTo.ContainsKey(toIt))
                                xaosm.Add((float)(it.WeightTo[toIt] * toIt.baseWeight));
                            else
                                xaosm.Add(0);
                        }
                    }
                    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, xaosbufH);
                    GL.BufferData(BufferTarget.ShaderStorageBuffer, xaosm.Capacity * sizeof(float), xaosm.ToArray(), BufferUsageHint.DynamicDraw);

                    //regenerated by shader in 1st iteration
                    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, last_tf_index_bufH);
                    GL.BufferData(BufferTarget.ShaderStorageBuffer, InvocationCount * sizeof(int), IntPtr.Zero, BufferUsageHint.DynamicDraw);

                    //update palette
                    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, palettebufH);
                    GL.BufferData(BufferTarget.ShaderStorageBuffer, CurrentParams.Palette.Colors.Count * sizeof(float) * 4, CurrentParams.Palette.Colors.ToArray(), BufferUsageHint.DynamicDraw);

                    invalidParams = false;
                }
            }

            if (PassItersCnt >= MaxIters)
            {
                PassItersCnt = 0;
                UpdatePointsState();
                //idea: place new random points along the most dense area?
                //idea: place new random points along the least dense area?
            }

            var settings = new SettingsStruct
            {
                CameraBase = CurrentParams.ViewSettings.Camera.Params,
                itnum = CurrentParams.Iterators.Count,
                pass_iters = PassIters,
                dispatchCnt = dispatchCnt,
                fog_effect = (float)CurrentParams.ViewSettings.FogEffect,
                dof = (float)CurrentParams.ViewSettings.Dof,
                focusdistance = (float)CurrentParams.ViewSettings.FocusDistance,
                focusarea = (float)CurrentParams.ViewSettings.FocusArea,
                focuspoint = CurrentParams.ViewSettings.Camera.Params.position + (float)CurrentParams.ViewSettings.FocusDistance * CurrentParams.ViewSettings.Camera.Params.forward,
                palettecnt = CurrentParams.Palette.Colors.Count
            };
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, settingsbufH);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, 4 * sizeof(int) + (24 + 8) * sizeof(float), ref settings, BufferUsageHint.StreamDraw);

            GL.Finish();
            GL.DispatchCompute(workgroupCount, 1, 1);

            TotalIterations += Convert.ToUInt64(PassIters * InvocationCount);
            PassItersCnt += PassIters;
            dispatchCnt++;

            //GL.Finish();
            bool isPerceptuallyEqualFrame = Helper.MathExtensions.IsPow2(dispatchCnt);
            if (updateDisplayNow || (UpdateDisplayOnRender && (!EnablePerceptualUpdates || (EnablePerceptualUpdates && isPerceptuallyEqualFrame))))
            {
                int lastFboH = fboDispH;

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboDispH);//
                GL.UseProgram(displayProgramH);
                //TODO:  only update if needed
                GL.Uniform1(GL.GetUniformLocation(displayProgramH, "width"), HistogramWidth);
                GL.Uniform1(GL.GetUniformLocation(displayProgramH, "height"), HistogramHeight);
                GL.Uniform1(GL.GetUniformLocation(displayProgramH, "ActualDensity"), 1 + (uint)(TotalIterations / (uint)(HistogramWidth * HistogramHeight)));//apo:*0.001
                GL.Uniform1(GL.GetUniformLocation(displayProgramH, "Brightness"), (float)CurrentParams.ViewSettings.Brightness);
                GL.Uniform1(GL.GetUniformLocation(displayProgramH, "InvGamma"), (float)(1.0f / CurrentParams.ViewSettings.Gamma));
                GL.Uniform1(GL.GetUniformLocation(displayProgramH, "GammaThreshold"), (float)CurrentParams.ViewSettings.GammaThreshold);
                GL.Uniform1(GL.GetUniformLocation(displayProgramH, "Vibrancy"), (float)CurrentParams.ViewSettings.Vibrancy);
                GL.Uniform3(GL.GetUniformLocation(displayProgramH, "BackgroundColor"), CurrentParams.ViewSettings.BackgroundColor.R / 255.0f, CurrentParams.ViewSettings.BackgroundColor.G / 255.0f, CurrentParams.ViewSettings.BackgroundColor.B / 255.0f);
                //tmp
                GL.Uniform1(GL.GetUniformLocation(displayProgramH, "EnableDE"), EnableDE?1:0);
                //draw quad
                GL.Begin(PrimitiveType.Quads);
                GL.Vertex2(0, 0);
                GL.Vertex2(0, 1);
                GL.Vertex2(1, 1);
                GL.Vertex2(1, 0);
                GL.End();

                if (EnableTAA)
                {
                    lastFboH = fboTaaH;
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboTaaH);//
                    GL.UseProgram(taaProgramH);
                    GL.Uniform1(GL.GetUniformLocation(taaProgramH, "width"), HistogramWidth);
                    GL.Uniform1(GL.GetUniformLocation(taaProgramH, "height"), HistogramHeight);
                    //draw quad
                    GL.Begin(PrimitiveType.Quads);
                    GL.Vertex2(0, 0);
                    GL.Vertex2(0, 1);
                    GL.Vertex2(1, 1);
                    GL.Vertex2(1, 0);
                    GL.End();
                }

                float rw = DisplayWidth / (float)HistogramWidth;
                float rh = DisplayHeight / (float)HistogramHeight;
                float rr = (rw < rh ? rw : rh) * .98f;
                GL.BlitNamedFramebuffer(lastFboH,
                    0, 0, 0, HistogramWidth, HistogramHeight,
                    (int)(DisplayWidth / 2 - HistogramWidth / 2 * rr),
                    (int)(DisplayHeight / 2 - HistogramHeight / 2 * rr),
                    (int)(DisplayWidth / 2 + HistogramWidth / 2 * rr),
                    (int)(DisplayHeight / 2 + HistogramHeight / 2 * rr),
                    ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
                //GL.CopyImageSubData(dispTexH, ImageTarget.Texture2D, 1, 0, 0, 0, 0, ImageTarget.Texture2D, 1, dw / 2 - Width / 2, dh / 2 - Height / 2, dw, dh, Height, Width);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

                DisplayFrameCompleted?.Invoke(this, null);
                updateDisplayNow = false;
            }

        }

        public void StartRendering()
        {
            if (!rendering)
            {
                rendering = true;

                new System.Threading.Thread(() =>
                {
                    ctx.MakeCurrent(wInfo);
                    while (rendering)
                        RenderFrame();
                    GL.Finish();
                    ctx.MakeCurrent(null);
                    stopRender.Set();
                }).Start();
            }
        }

        System.Threading.AutoResetEvent stopRender = new System.Threading.AutoResetEvent(false);
        private int taaProgramH;
        private int taaTexH;

        /// <summary>
        /// Wait for render thread to stop
        /// TODO: async waitone ? http://msdn.microsoft.com/en-us/library/hh873178.aspx#WaitHandles
        /// </summary>
        public async Task StopRendering()
        {
            if (rendering)
            {
                rendering = false;
                stopRender.WaitOne();
            }
        }

        public void UpdateDisplay()
        {
            updateDisplayNow = true;
            //TODO: make 1 frame, skip 1st (compute) pass
        }

        /// <summary>
        /// Helper to call opengl from current thread with context. Stops the render thread.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private async Task WithContext(Action action)
        {
            bool continueRendering = rendering;
            await StopRendering();
            ctx.MakeCurrent(wInfo);//lend context from render thread
            action();
            ctx.MakeCurrent(null);
            if (continueRendering)
                StartRendering();//restart render thread if it was running
        }

        /// <summary>
        /// Pixel format: rgba
        /// </summary>
        /// <returns></returns>
        public async Task<double[,][]> GenerateImage(bool fillAlpha = true)
        {
            float[] d = new float[HistogramWidth * HistogramHeight * 4];//rgba

            UpdateDisplay();//TODO: await 1 display frame

            await WithContext(() => {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboDispH);
                GL.ReadPixels(0, 0, HistogramWidth, HistogramHeight, PixelFormat.Rgba, PixelType.Float, d);
            });

            double[,][] o = new double[HistogramWidth, HistogramHeight][];
            await Task.Run(() =>
            {
                for (int x = 0; x < HistogramWidth; x++)
                    for (int y = 0; y < HistogramHeight; y++)
                    {
                        o[x, y] = new double[4];
                        o[x, y][0] = d[x * 4 + y * 4 * HistogramWidth + 0];
                        o[x, y][1] = d[x * 4 + y * 4 * HistogramWidth + 1];
                        o[x, y][2] = d[x * 4 + y * 4 * HistogramWidth + 2];
                        o[x, y][3] = fillAlpha ? 1.0 : d[x * 4 + y * 4 * HistogramWidth + 3];
                    }
            });

            //TODO: image save in netstandard?

            return o;
        }

        int vertexShaderH;
        private int fboTaaH;

        private void initTAA()
        {

            var resource = typeof(RendererGL).GetTypeInfo().Assembly.GetManifestResourceStream("IFSEngine.glsl.taa.frag.shader");
            string taaShaderSource = new StreamReader(resource).ReadToEnd();
            //compile taa shader
            int taaShaderH = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(taaShaderH, taaShaderSource);
            GL.CompileShader(taaShaderH);
            GL.GetShader(taaShaderH, ShaderParameter.CompileStatus, out int status);
            if (status == 0)
            {
                throw new GraphicsException(
                    String.Format("Error compiling {0} shader: {1}", ShaderType.FragmentShader.ToString(), GL.GetShaderInfoLog(taaShaderH)));
            }

            //init taa image texture
            taaTexH = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, taaTexH);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

            //TODO: display resolution?
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, HistogramWidth, HistogramHeight, 0, PixelFormat.Rgba, PixelType.Float, new IntPtr(0));

            fboTaaH = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboTaaH);//offscreen
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, taaTexH, 0);
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
                throw new GraphicsException("Frame Buffer Error");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);//screen

            taaProgramH = GL.CreateProgram();
            GL.AttachShader(taaProgramH, vertexShaderH);
            GL.AttachShader(taaProgramH, taaShaderH);
            GL.LinkProgram(taaProgramH);
            GL.GetProgram(taaProgramH, GetProgramParameterName.LinkStatus, out status);
            if (status == 0)
            {
                throw new GraphicsException(
                    String.Format("Error linking taa program: {0}", GL.GetProgramInfoLog(taaProgramH)));
            }

            GL.DetachShader(taaProgramH, vertexShaderH);
            GL.DetachShader(taaProgramH, taaShaderH);
            GL.DeleteShader(vertexShaderH);
            GL.DeleteShader(taaShaderH);

            GL.UseProgram(taaProgramH);

            //bind layout:
            //GL.BindImageTexture(0, taaTexH, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);
            //GL.BindImageTexture(1, dispTexH, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba32f);
            GL.Uniform1(GL.GetUniformLocation(taaProgramH, "t1"), 0);
            GL.Uniform1(GL.GetUniformLocation(taaProgramH, "t2"), 1);
        }

        private void initDisplay()
        {
            var assembly = typeof(RendererGL).GetTypeInfo().Assembly;

            vertexShaderH = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShaderH, new StreamReader(assembly.GetManifestResourceStream("IFSEngine.glsl.Display.vert.shader")).ReadToEnd());
            GL.CompileShader(vertexShaderH);
            GL.GetShader(vertexShaderH, ShaderParameter.CompileStatus, out int status);
            if (status == 0)
            {
                throw new GraphicsException(
                    String.Format("Error compiling {0} shader: {1}", ShaderType.VertexShader.ToString(), GL.GetShaderInfoLog(vertexShaderH)));
            }

            var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, new StreamReader(assembly.GetManifestResourceStream("IFSEngine.glsl.Display.frag.shader")).ReadToEnd());
            GL.CompileShader(fragmentShader);
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out status);
            if (status == 0)
            {
                throw new GraphicsException(
                    String.Format("Error compiling {0} shader: {1}", ShaderType.FragmentShader.ToString(), GL.GetShaderInfoLog(fragmentShader)));
            }
            
            //init display image texture
            dispTexH = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, dispTexH);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

            //TODO: display resolution?
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, HistogramWidth, HistogramHeight, 0, PixelFormat.Rgba, PixelType.Float, new IntPtr(0));

            fboDispH = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboDispH);//offscreen
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, dispTexH, 0);
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
                throw new GraphicsException("Frame Buffer Error");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);//screen

            displayProgramH = GL.CreateProgram();
            GL.AttachShader(displayProgramH, vertexShaderH);
            GL.AttachShader(displayProgramH, fragmentShader);
            GL.LinkProgram(displayProgramH);
            GL.GetProgram(displayProgramH, GetProgramParameterName.LinkStatus, out status);
            if (status == 0)
            {
                throw new GraphicsException(
                    String.Format("Error linking program: {0}", GL.GetProgramInfoLog(displayProgramH)));
            }

            GL.DetachShader(displayProgramH, vertexShaderH);
            GL.DetachShader(displayProgramH, fragmentShader);
            //GL.DeleteShader(vertexShaderH);
            GL.DeleteShader(fragmentShader);

            GL.UseProgram(displayProgramH); 

            histogramH = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, histogramH);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, HistogramWidth * HistogramHeight * 4 * sizeof(float), IntPtr.Zero, BufferUsageHint.StaticCopy); 
        }

        private void initRenderer()
        {

            //assemble source string
            var resource = typeof(RendererGL).GetTypeInfo().Assembly.GetManifestResourceStream("IFSEngine.glsl.ifs_kernel.compute");
            string computeShaderSource = new StreamReader(resource).ReadToEnd();

            //compile compute shader
            int computeShaderH = GL.CreateShader(ShaderType.ComputeShader);
            GL.ShaderSource(computeShaderH, computeShaderSource);
            GL.CompileShader(computeShaderH);
            GL.GetShader(computeShaderH, ShaderParameter.CompileStatus, out int status);
            if (status == 0)
            {
                throw new GraphicsException(
                    String.Format("Error compiling {0} shader: {1}", ShaderType.ComputeShader.ToString(), GL.GetShaderInfoLog(computeShaderH)));
            }

            //build shader program
            computeProgramH = GL.CreateProgram();
            GL.AttachShader(computeProgramH, computeShaderH);
            GL.LinkProgram(computeProgramH);
            Console.WriteLine(GL.GetProgramInfoLog(computeProgramH));
            GL.UseProgram(computeProgramH);

            //create buffers
            pointsbufH = GL.GenBuffer();
            settingsbufH = GL.GenBuffer();
            itersbufH = GL.GenBuffer();
            palettebufH = GL.GenBuffer();
            tfparamsbufH = GL.GenBuffer();
            xaosbufH = GL.GenBuffer();
            last_tf_index_bufH = GL.GenBuffer();

            //bind layout:
            GL.BindImageTexture(0, dispTexH, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);//TODO: use this or remove
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, histogramH);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, pointsbufH);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, settingsbufH);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 4, itersbufH);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 5, palettebufH);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 6, tfparamsbufH);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 7, xaosbufH);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 8, last_tf_index_bufH);

        }

        public void Dispose()
        {
            DisplayFrameCompleted = null;
            StopRendering().Wait();
            //TOOD: dispose
        }

    }
}
