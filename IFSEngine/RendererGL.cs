using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

//using OpenGL;
using OpenTK;
using OpenTK.Graphics.OpenGL;

using System.Linq;

using IFSEngine.Model;
using IFSEngine.mwc64x;

namespace IFSEngine
{
    public class RendererGL
    {

        [DllImport("opengl32.dll")]
        extern static IntPtr wglGetCurrentDC();

        /*ComputePlatform platf;
        static ComputeDevice device1;
        ComputeContext ctx;
        ComputeCommandQueue cq;
        static ComputeProgram prog1;
        ComputeKernel computekernel;
        ComputeKernel displaykernel;
        ComputeBuffer<float> calcbuf;
        ComputeImage2D dispimg;
        ComputeBuffer<float> dispbuf;
        ComputeBuffer<float> dispsettingsbuf;
        ComputeBuffer<Iterator> iteratorsbuf;
        ComputeBuffer<Settings> settingsbuf;
        ComputeBuffer<float> pointsstatebuf;
        ComputeBuffer<mwc64x_state_t> rng_states;

        ComputeErrorCode e;
        ComputeProgramBuildNotifier buildnotif = new ComputeProgramBuildNotifier(builddebug);
        static void builddebug(CLProgramHandle h, IntPtr p)
        {
            Debug.WriteLine("BUILD LOG: " + prog1.GetBuildLog(device1));
        }

        ComputeContextNotifier contextnotif = new ComputeContextNotifier((str, a, b, c) => {
            Debug.WriteLine("CTX ERROR INFO: " + str);
        });

        private CLEventCollection ComputeEventsCollection;
        private CLEventCollection DisplayEventsCollection;*/

        int threadcnt = 1500;//gtx970: 1664, 610m: 48
        int histogramH;
        int computeProgramH;
        int settingsbufH;

        //display
        private int dispTexH;
        private int displayProgramH;

        Random rgen = new Random();
        public int rendersteps = 0;
        private bool updateDisplayNow=false;

        //
        public int Width => CurrentParams.Camera.Width;
        public int Height => CurrentParams.Camera.Height;

        //TODO: ehelyett jobbat kitalalni, ezekhez nem kell Mutate()
        public float Brightness { get => CurrentParams.Brightness; set => CurrentParams.Brightness = value; }
        public float Gamma { get => CurrentParams.Gamma; set => CurrentParams.Gamma = value; }

        /// <summary>
        /// Use <see cref="UpdateParams(IFS)"/> to set params
        /// </summary>
        public IFS CurrentParams { get; private set; }

        public RendererGL(IFS Params) : this(Params, 1920, 1080) { }
        public RendererGL(IFS Params, int w, int h)
        {
            UpdateParams(Params);
            MutateParams(p =>{
                p.Camera.Width = w;
                p.Camera.Height = h;
                return p;
            });

            initDisplay();
            initRenderer();

        }

        private bool invalidAccumulation = false;
        private bool invalidParams = false;
        private int pass_iters;

        public void InvalidateAccumulation()
        {
            //tobben is hivhatjak, de eleg egyszer resetelni, ezert invalidate
            invalidAccumulation = true;

        }

        /// <summary>
        /// azert van kulon a kameranak, hogy mozgatasnal ne kelljen a fraktalt feleslegesen frissiteni
        /// </summary>
        public void MutateCamera(Func<Camera, Camera> mutator)
        {
            CurrentParams.Camera = mutator(CurrentParams.Camera);
            invalidAccumulation = true;
        }

        public void UpdateParams(IFS newParams)
        {
            CurrentParams = newParams;

            invalidParams = true;
            invalidAccumulation = true;
        }
        public void MutateParams(Func<IFS, IFS> mutator)
        {
            UpdateParams(mutator(CurrentParams));
        }

        Stopwatch renderwatch = new Stopwatch();

        public void RenderFrame()
        {
            renderwatch.Restart();
            //if volt init()

            //GL.Finish();
            GL.UseProgram(computeProgramH);

            if (invalidAccumulation)
            {
                //reset accumulation
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, histogramH);
                GL.ClearNamedBufferData(histogramH, PixelInternalFormat.R32f, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
                invalidAccumulation = false;
                pass_iters = 100;//minden renderrel duplazodik
                rendersteps = 0;

                if (invalidParams)
                {
                    //update pointsstate
                    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, pointsbufH);
                    GL.BufferData(BufferTarget.ShaderStorageBuffer, 4*threadcnt*sizeof(float), StartingDistributions.UniformUnitCube(threadcnt), BufferUsageHint.DynamicCopy/**/);

                    List<Iterator> its_and_final = new List<Iterator>(CurrentParams.Iterators);
                    its_and_final.Add(CurrentParams.FinalIterator);

                    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, itersbufH);
                    GL.BufferData(BufferTarget.ShaderStorageBuffer, its_and_final.Count* 16*sizeof(float) * 1*sizeof(int), its_and_final.ToArray(), BufferUsageHint.DynamicCopy/**/);

                    invalidParams = false;
                }
            }

            //ez mennyire faj?
            var settings = new Settings
            {
                pass_iters = pass_iters,
                camera = CurrentParams.Camera.Params,
                rendersteps = rendersteps,
                enable_depthfog = CurrentParams.Camera.EnableDepthFog ? 1 : 0,
                itnum = CurrentParams.Iterators.Count,
            };
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, settingsbufH);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, 4 * sizeof(int)+17*sizeof(float), ref settings, BufferUsageHint.DynamicCopy/**/);

            GL.Finish();
            GL.DispatchCompute(threadcnt, 1, 1);

            pass_iters = Math.Min((int)(pass_iters * 1.1), 50000);
            rendersteps++;
            //GL.Finish();



            if (updateDisplayNow || UpdateDisplayOnRender)
            {
                GL.UseProgram(displayProgramH);
                GL.Uniform1(GL.GetUniformLocation(displayProgramH, "rendersteps"), rendersteps);
                GL.Uniform1(GL.GetUniformLocation(displayProgramH, "Brightness"), Brightness);
                GL.Uniform1(GL.GetUniformLocation(displayProgramH, "Gamma"), Gamma);
                //GL.Viewport(0, 0, display1.ClientSize.Width, display1.ClientSize.Height);
                //GL.Clear(ClearBufferMask.ColorBufferBit);
                GL.Begin(PrimitiveType.Quads);
                GL.Vertex2(0, 0);//0 0
                GL.Vertex2(0, 1);//0 1
                GL.Vertex2(1, 1);//1 1
                GL.Vertex2(1, 0);//1 0
                GL.End();

                DisplayFrameCompleted?.Invoke(this, null);
                updateDisplayNow = false;
            }

        }

        bool rendering = false;
        private int itersbufH;
        private int pointsbufH;

        public void StartRendering()
        {
            if (!rendering)
            {
                rendering = true;

                /*new System.Threading.Thread(() =>
                {//gl nem hivhato mas threadrol
                    while (rendering)
                        RenderFrame();
                }).Start();*/

                while(rendering)
                    RenderFrame();

            }
        }
        public void StopRendering()
        {
            rendering = false;
        }

        public bool UpdateDisplayOnRender { get; set; } = true;
        //public event EventHandler RenderFrameCompleted;
        public event EventHandler DisplayFrameCompleted;

        /*private void RenderFrame_Completed(object sender, object args)
        {
            renderwatch.Stop();
            Debug.WriteLine($"RENDER: {renderwatch.ElapsedMilliseconds}ms");
            //RenderFrameCompleted?.Invoke(this, null);//TODO: pass useful args
            //if (UpdateDisplayOnRender)
            //{
            //    UpdateDisplay();
            //    DisplayFrameCompleted?.Invoke(this, null);
            //}
            if (rendering)
                Parallel.Invoke(RenderFrame);
        }*/

        public void UpdateDisplay()
        {
            //displaywatch.Restart();

            //TODO: update uniform: rendersteps , CurrentParams.Camera.Brightness, CurrentParams.Camera.Gamma 

            //make sure writing to image has finished before read
            //GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);

            //GL.Finish();

            updateDisplayNow = true;

            //GL.Accum(AccumOp.Return, 1.0f);

            //GL.Clear(ClearBufferMask.ColorBufferBit);
            //GL.UseProgram(quad_program);
            //GL.BindVertexArray(quad_vao);
            //GL.ActiveTexture(GL_TEXTURE0);
            //GL.BindTexture(GL_TEXTURE_2D, tex_output);
            //GL.DrawArrays(GL_TRIANGLE_STRIP, 0, 4);

            //TODO: dispatch display kernel, Width * Height
            //DisplayFrame_Completed(this, null);

        }

        public double[,][] GenerateImage()
        {
            float[] d = new float[Width * Height * 4];//rgba

            UpdateDisplay();//ebbol event jon
            RenderFrame();

            GL.Finish();
            //TODO: read display texture

            double[,][] o = new double[Width, Height][];
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    o[x, y] = new double[4];
                    o[x, y][0] = d[x * 4 + y * 4 * Width + 0];
                    o[x, y][1] = d[x * 4 + y * 4 * Width + 1];
                    o[x, y][2] = d[x * 4 + y * 4 * Width + 2];
                    o[x, y][3] = d[x * 4 + y * 4 * Width + 3];
                }

            //TODO: image save netstandardbol?

            return o;
        }

        private void initDisplay()
        {
            dispTexH = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, dispTexH);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f/*cl float*/, Width, Height, 0, PixelFormat.Rgba, PixelType.Float, new IntPtr(0));
            //GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texID, 0);
            //DrawBuffersEnum[] dbe = new DrawBuffersEnum[1];
            //dbe[0] = DrawBuffersEnum.ColorAttachment0;
            //GL.DrawBuffers(1, dbe);
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
                Console.WriteLine("BAJBJABJABAJ");

            // Because we're also using this tex as an image (in order to write to it),
            // we bind it to an image unit as well
            GL.BindImageTexture(0, dispTexH, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);//EZKELL?

            var assembly = typeof(RendererGL).GetTypeInfo().Assembly;

            var vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, new StreamReader(assembly.GetManifestResourceStream("IFSEngine.glsl.Display.vert")).ReadToEnd());
            GL.CompileShader(vertexShader);
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int status);
            if (status == 0)
            {
                throw new GraphicsException(
                    String.Format("Error compiling {0} shader: {1}", ShaderType.VertexShader.ToString(), GL.GetShaderInfoLog(vertexShader)));
            }

            var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            string elo = $"#version 450 \n #extension GL_ARB_explicit_attrib_location : enable \n";
            elo += $"uniform int width={Width}; \n uniform int height={Height};\n";
            GL.ShaderSource(fragmentShader, elo + new StreamReader(assembly.GetManifestResourceStream("IFSEngine.glsl.Display.frag")).ReadToEnd());
            GL.CompileShader(fragmentShader);
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out status);
            if (status == 0)
            {
                throw new GraphicsException(
                    String.Format("Error compiling {0} shader: {1}", ShaderType.VertexShader.ToString(), GL.GetShaderInfoLog(fragmentShader)));
            }
            displayProgramH = GL.CreateProgram();
            GL.AttachShader(displayProgramH, vertexShader);
            GL.AttachShader(displayProgramH, fragmentShader);
            GL.LinkProgram(displayProgramH);
            GL.GetProgram(displayProgramH, GetProgramParameterName.LinkStatus, out status);
            if (status == 0)
            {
                throw new GraphicsException(
                    String.Format("Error linking program: {0}", GL.GetProgramInfoLog(displayProgramH)));
            }

            GL.DetachShader(displayProgramH, vertexShader);
            GL.DetachShader(displayProgramH, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            GL.UseProgram(displayProgramH);

            //beallitjuk a texturat
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);//to screen

            histogramH = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, histogramH);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, Width * Height * 4 * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicCopy/**/);

            GL.Viewport(0, 0, Width, Height);
        }

        private void initRenderer()
        {

            //assemble source string
            var resource = typeof(RendererGL).GetTypeInfo().Assembly.GetManifestResourceStream("IFSEngine.glsl.ifs_kernel.compute");
            string computeShaderSource = "";
            computeShaderSource += new StreamReader(resource).ReadToEnd();

            Debug.WriteLine("init openGL..");

            //platforms, devices

            //cq from ctx
            //compute program from source
            int computeShaderH = GL.CreateShader(ShaderType.ComputeShader);
            GL.ShaderSource(computeShaderH, computeShaderSource);
            GL.CompileShader(computeShaderH);
            Console.WriteLine(GL.GetShaderInfoLog(computeShaderH));

            computeProgramH = GL.CreateProgram();
            GL.AttachShader(computeProgramH, computeShaderH);
            GL.LinkProgram(computeProgramH);//build
            Console.WriteLine(GL.GetProgramInfoLog(computeProgramH));
            GL.UseProgram(computeProgramH);//EZKELL?

            //create kernels from program

            //calc, disp buffers Width * Height * 4);//rgba

            //points state storage buffer
            pointsbufH = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, pointsbufH);
            Vector4[] deb = new Vector4[threadcnt];
            /*Random r = new Random();
            for (int i = 0; i < deb.Length; i++)
            {
                deb[i] = new Vector4(r.Next(10,Width-10), r.Next(10,Height-10), 0, 0);
            }
            GL.BufferData(BufferTarget.ShaderStorageBuffer, threadcnt * 4 * sizeof(float), deb, BufferUsageHint.DynamicCopy);*/

            //histogram
            //int histogramH = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, histogramH);
            //GL.BufferData(BufferTarget.ShaderStorageBuffer, Width * Height * 4 * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicCopy/**/);

            //settings
            settingsbufH = GL.GenBuffer();
            //GL.BindBuffer(BufferTarget.ShaderStorageBuffer, settingsbufH);
            //GL.BufferData(BufferTarget.ShaderStorageBuffer, 4*sizeof(int), IntPtr.Zero, BufferUsageHint.DynamicCopy/**/);

            //settings
            itersbufH = GL.GenBuffer();
            //GL.BindBuffer(BufferTarget.ShaderStorageBuffer, itersbufH);
            //GL.BufferData(BufferTarget.ShaderStorageBuffer, , , BufferUsageHint.DynamicCopy/**/);

            //Uniforms
            //dispsettingsbuf = new Cloo.ComputeBuffer<float>(ctx, ComputeMemoryFlags.ReadOnly, 3);
            //iteratorsbuf = new ComputeBuffer<Iterator>(ctx, ComputeMemoryFlags.ReadOnly, 10 /*max 10 most...*/ /*its+final*/);//HACK: ezt update params ban kene
            //settingsbuf = new ComputeBuffer<Settings>(ctx, ComputeMemoryFlags.ReadOnly, 1);
            //pointsstatebuf = new ComputeBuffer<float>(ctx, ComputeMemoryFlags.ReadWrite, threadcnt * 4);//minden szal pontjat mentjuk a kovi passba
            //rng_states = new ComputeBuffer<mwc64x_state_t>(ctx, ComputeMemoryFlags.ReadWrite, threadcnt);*/

            //set uniforms
            GL.UseProgram(computeProgramH);//setterek erre vonatkoznak

            /* computekernel.SetMemoryArgument(0, calcbuf);
             //computekernel.SetMemoryArgument(1, randbuf);
             computekernel.SetMemoryArgument(1, iteratorsbuf);//HACK: ezt update params ban kene
             computekernel.SetMemoryArgument(2, settingsbuf);
             computekernel.SetMemoryArgument(3, pointsstatebuf);
             computekernel.SetMemoryArgument(4, rng_states);

             displaykernel.SetMemoryArgument(0, calcbuf);
             displaykernel.SetMemoryArgument(1, dispbuf);
             displaykernel.SetMemoryArgument(2, dispimg);
             displaykernel.SetMemoryArgument(3, dispsettingsbuf);*/

            //layout binds
            GL.BindImageTexture(0/**/, dispTexH, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1/**/, histogramH);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2/**/, pointsbufH);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3/**/, settingsbufH);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 4/**/, itersbufH);

            GL.Uniform1(GL.GetUniformLocation(computeProgramH, "width"), Width);
            GL.Uniform1(GL.GetUniformLocation(computeProgramH, "height"), Height);
        }

        public void Dispose()
        {
            rendering = false;
            //TOOD: dispose
        }

    }
}
