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
        int dispTexH;
        int histogramH;
        int computeProgramH;
        Random rgen = new Random();
        public int rendersteps = 0;
        private bool updateDisplayNow=false;

        //
        public int Width => CurrentParams.Camera.Width;
        public int Height => CurrentParams.Camera.Height;

        /// <summary>
        /// Use <see cref="UpdateParams(IFS)"/> to set params
        /// </summary>
        public IFS CurrentParams { get; private set; }

        public RendererGL(IFS Params) : this(Params, null) { }
        public RendererGL(IFS Params, int? texturetarget)
        {
            UpdateParams(Params);
            //ComputeEventsCollection = new CLEventCollection(this.RenderFrame_Completed);
            //DisplayEventsCollection = new CLEventCollection(this.DisplayFrame_Completed);


            //assemble source string
            var resource = typeof(RendererGL).GetTypeInfo().Assembly.GetManifestResourceStream("IFSEngine.glsl.ifs_kernel.compute");
            string computeShaderSource = "";
            computeShaderSource += new StreamReader(resource).ReadToEnd();

            Debug.WriteLine("init openGL..");

            //platforms, devices

            //cq from ctx
            //compute program from source
            int computeShaderH = GL.CreateShader(ShaderType.ComputeShader);
            GL.ShaderSource(computeShaderH, computeShaderSource );
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
            int pointsbufH = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, pointsbufH);
            Vector4[] deb = new Vector4[4 * threadcnt];
            Random r = new Random();
            for(int i=0;i<deb.Length;i++)
            {
                deb[i] = new Vector4(r.Next(Width), r.Next(Height), 0, 0);
            }
            GL.BufferData(BufferTarget.ShaderStorageBuffer, threadcnt * 4 * sizeof(float), deb, BufferUsageHint.DynamicCopy/**/);

            if (texturetarget!=null)
            {
                //use texture texturetarget
            }
            else
            {
                //TODO: new texture Rgba, ComputeImageChannelType.Float), Width, Height
            }

            //histogram
            // dimensions of the image
            histogramH = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture1);//ebben a unitban
            GL.BindTexture(TextureTarget.Texture2D, histogramH);//ezt a texture objectet parameterezzuk
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, Width, Height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            GL.BindImageTexture(0/**/, histogramH, 0, false, 0, TextureAccess.ReadWrite/**/, SizedInternalFormat.Rgba32f);//EZ KELL?
                                                                                                                          //GL.Accum(AccumOp.Load, 0);

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

            //GL.ActiveTexture(TextureUnit.Texture0);//ebben a unitban
            //GL.BindTexture(TextureTarget.Texture2D, texturetarget??0);//ezt a texture objectet parameterezzuk
            //GL.Uniform1(GL.GetUniformLocation(computeProgramH, "img_output"), 0);

            //GL.ActiveTexture(TextureUnit.Texture0);//ebben a unitban
            //GL.BindTexture(TextureTarget.Texture2D, histogramH);//ezt a texture objectet parameterezzuk
            //GL.Uniform1(GL.GetUniformLocation(computeProgramH, "histogram"), 0);

            //layout binds
            GL.BindImageTexture(0/**/, texturetarget ?? 0, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2/**/, pointsbufH);


            Console.WriteLine(GL.GetError().ToString());
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

            GL.Finish();

            if (invalidAccumulation)
            {
                //TODO: reset accumulation
                //GL.ClearAccum(0, 0, 0, 0);
                GL.ClearTexImage(histogramH, 0, PixelFormat.Rgba, PixelType.Float, new float[] { 0, 0, 0, 0 });
                invalidAccumulation = false;
                pass_iters = 100;//minden renderrel duplazodik
                rendersteps = 0;

                if (invalidParams)
                {
                    //TODO: update pointsstate uniform StartingDistributions.UniformUnitCube(threadcnt)
                    List<Iterator> its_and_final = new List<Iterator>(CurrentParams.Iterators);
                    its_and_final.Add(CurrentParams.FinalIterator);
                    //TODO: update Iterators uniform, its_and_final
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

            //TODO: update Settings uniform
            GL.UseProgram(computeProgramH);
            if (updateDisplayNow || UpdateDisplayOnRender)
                GL.Uniform1(GL.GetUniformLocation(computeProgramH, "Display"), 1);
            else
                GL.Uniform1(GL.GetUniformLocation(computeProgramH, "Display"), 0);
            GL.Finish();
            GL.DispatchCompute(threadcnt, 1, 1);

            GL.Finish();
            //GL.Accum(AccumOp.Accum, 1.0f/rendersteps);

            pass_iters = Math.Min((int)(pass_iters * 1.1), 50000);
            rendersteps++;


            if (updateDisplayNow || UpdateDisplayOnRender)
            {
                DisplayFrameCompleted?.Invoke(this, null);
                updateDisplayNow = false;
            }

        }

        bool rendering = false;

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

        public void Dispose()
        {
            rendering = false;
            //TOOD: dispose
        }

    }
}
