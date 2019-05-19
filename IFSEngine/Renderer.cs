using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Cloo;
using Cloo.Bindings;
using System.Linq;

using IFSEngine.Model;
using IFSEngine.mwc64x;

namespace IFSEngine
{
    public class Renderer
    {

        [DllImport("opengl32.dll")]
        extern static IntPtr wglGetCurrentDC();

        ComputePlatform platf;
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
        private CLEventCollection DisplayEventsCollection;

        int threadcnt = 1500;//gtx970: 1664, 610m: 48
        int texturetarget;
        Random rgen = new Random();
        public int rendersteps = 0;

        //
        public int Width => CurrentParams.Camera.Width;
        public int Height => CurrentParams.Camera.Height;

        /// <summary>
        /// Use <see cref="UpdateParams(IFS)"/> to set params
        /// </summary>
        public IFS CurrentParams { get; private set; }

        public Renderer(IFS Params) : this (Params, new IntPtr(0), -1) { }
        public Renderer(IFS Params, IntPtr glctxh, int texturetarget)
        {
            UpdateParams(Params);
            this.texturetarget = texturetarget;
            ComputeEventsCollection = new CLEventCollection(this.RenderFrame_Completed);
            DisplayEventsCollection = new CLEventCollection(this.DisplayFrame_Completed);

            //e.OnAnyError(ec => throw new Exception(ec.ToString()));

            string kernelSource = "#define width (" + Width + ")\r\n";
            kernelSource += "#define height (" + Height + ")\r\n";
            //kernelSource += "#define max_iters (" + max_iters + ")\r\n";
            var assembly = typeof(Renderer).GetTypeInfo().Assembly;
            Stream resource = assembly.GetManifestResourceStream("IFSEngine.ifs_kernel.cl");
            kernelSource += new StreamReader(resource).ReadToEnd();

            Debug.WriteLine("init opencl..");
            //platf = ComputePlatform.Platforms[0];
            platf = ComputePlatform.Platforms.Where(pl => pl.Name == "NVIDIA CUDA").First();
            device1 = platf.Devices[0];//TODO: minden platform

            if (glctxh.ToInt32() > 0)
            {
                IntPtr raw_context_handle = glctxh;
                ComputeContextProperty p1 = new ComputeContextProperty(ComputeContextPropertyName.CL_GL_CONTEXT_KHR, raw_context_handle);
                ComputeContextProperty p2 = new ComputeContextProperty(ComputeContextPropertyName.CL_WGL_HDC_KHR, wglGetCurrentDC());
                ComputeContextProperty p3 = new ComputeContextProperty(ComputeContextPropertyName.Platform, platf.Handle.Value);
                List<ComputeContextProperty> props = new List<ComputeContextProperty>() { p1, p2, p3 };
                ComputeContextPropertyList Properties = new ComputeContextPropertyList(props);
                ctx = new ComputeContext(ComputeDeviceTypes.Gpu, Properties, contextnotif, IntPtr.Zero);
            }
            else
                ctx = new Cloo.ComputeContext(new ComputeDevice[] { device1 }, new ComputeContextPropertyList(platf), contextnotif, IntPtr.Zero);
            cq = new Cloo.ComputeCommandQueue(ctx, /*glcq?*/device1, ComputeCommandQueueFlags.Profiling/*None*/);
            prog1 = new Cloo.ComputeProgram(ctx, kernelSource);
            try
            {
                prog1.Build(new ComputeDevice[] { device1 }, "", buildnotif, IntPtr.Zero);
            }
            catch (Cloo.BuildProgramFailureComputeException)
            {
                Debug.WriteLine("BUILD ERROR: " + prog1.GetBuildLog(device1));
                return;
            }
            computekernel = prog1.CreateKernel("Main");
            displaykernel = prog1.CreateKernel("Display");

            //randbuf = new Cloo.ComputeBuffer<float>(ctx, ComputeMemoryFlags.ReadOnly, maxthreadcnt * (/*max_iters*/10000 + 2));
            calcbuf = new Cloo.ComputeBuffer<float>(ctx, ComputeMemoryFlags.ReadWrite, Width * Height * 4);//rgba
            dispbuf = new Cloo.ComputeBuffer<float>(ctx, ComputeMemoryFlags.WriteOnly, Width * Height * 4);//rgba
            if (glctxh.ToInt32() > 0)
            {
                dispimg = Cloo.ComputeImage2D.CreateFromGLTexture2D(ctx, ComputeMemoryFlags.WriteOnly, 3553/*gl texture2d*/, 0, texturetarget);
                //dispimg = Cloo.ComputeImage2D.CreateFromGLRenderbuffer(ctx, ComputeMemoryFlags.WriteOnly, texturetarget/*buffertarget*/);
            }
            else
                dispimg = new Cloo.ComputeImage2D(ctx, ComputeMemoryFlags.WriteOnly, new ComputeImageFormat(ComputeImageChannelOrder.Rgba, ComputeImageChannelType.Float), Width, Height, 0, IntPtr.Zero);
            dispsettingsbuf = new Cloo.ComputeBuffer<float>(ctx, ComputeMemoryFlags.ReadOnly, 3);
            iteratorsbuf = new ComputeBuffer<Iterator>(ctx, ComputeMemoryFlags.ReadOnly, 10 /*max 10 most...*/ /*its+final*/);//HACK: ezt update params ban kene
            settingsbuf = new ComputeBuffer<Settings>(ctx, ComputeMemoryFlags.ReadOnly, 1);
            pointsstatebuf = new ComputeBuffer<float>(ctx, ComputeMemoryFlags.ReadWrite, threadcnt * 4);//minden szal pontjat mentjuk a kovi passba
            rng_states = new ComputeBuffer<mwc64x_state_t>(ctx, ComputeMemoryFlags.ReadWrite, threadcnt);

            computekernel.SetMemoryArgument(0, calcbuf);
            //computekernel.SetMemoryArgument(1, randbuf);
            computekernel.SetMemoryArgument(1, iteratorsbuf);//HACK: ezt update params ban kene
            computekernel.SetMemoryArgument(2, settingsbuf);
            computekernel.SetMemoryArgument(3, pointsstatebuf);
            computekernel.SetMemoryArgument(4, rng_states);

            displaykernel.SetMemoryArgument(0, calcbuf);
            displaykernel.SetMemoryArgument(1, dispbuf);
            displaykernel.SetMemoryArgument(2, dispimg);
            displaykernel.SetMemoryArgument(3, dispsettingsbuf);
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
        public void MutateCamera(Func<Camera,Camera> mutator)
        {
            CurrentParams.Camera = mutator(CurrentParams.Camera);
            invalidAccumulation = true;
        }

        public void UpdateParams(IFS newParams)
        {
            //cq.WriteToBuffer<float>(StartingDistributions.UniformUnitCube(threadcnt), pointsstatebuf, false, null);
            /*//if (its.Count != this.its.Count)
            {
                //var bufferHandle = iteratorsbuf?.Handle;
                iteratorsbuf?.Dispose();//ez miert nem szall el
                iteratorsbuf = new ComputeBuffer<Iterator>(ctx, ComputeMemoryFlags.ReadOnly, its.Count + 1);//its+final
                computekernel.Set0MemoryArgument(1, iteratorsbuf);
            }*/
            /*this.its = its;
            this.finalit = finalit;
            //rendersteps = 0;
            this.settings.itnum = its.Count;*/
            /*List<Iterator> its_and_final = new List<Iterator>(its);
            its_and_final.Add(finalit);
            cq.WriteToBuffer<Iterator>(its_and_final.ToArray(), iteratorsbuf, false, null);*/

            CurrentParams = newParams;

            invalidParams = true;
            invalidAccumulation = true;
        }
        public void MutateParams(Func<IFS,IFS> mutator)
        {
            UpdateParams(mutator(CurrentParams));
        }

        Stopwatch renderwatch = new Stopwatch();
        Stopwatch displaywatch = new Stopwatch();

        public void RenderFrame()
        {
            renderwatch.Restart();
            //if volt init()
            cq.Finish();//

            if (invalidAccumulation)
            {
                cq.WriteToBuffer<float>(new float[Width * Height * 4], calcbuf, false, null);
                invalidAccumulation = false;
                pass_iters = 100;//minden renderrel duplazodik
                rendersteps = 0;

                if (invalidParams)
                {
                    cq.WriteToBuffer<float>(StartingDistributions.UniformUnitCube(threadcnt), pointsstatebuf, false, null);//ezt gyakrabban?
                    List<Iterator> its_and_final = new List<Iterator>(CurrentParams.Iterators);
                    its_and_final.Add(CurrentParams.FinalIterator);
                    cq.WriteToBuffer<Iterator>(its_and_final.ToArray(), iteratorsbuf, false, null);
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

            cq.WriteToBuffer<Settings>(new Settings[] { settings }, settingsbuf, true, null);//camera motion blur es pass_iters miatt
            cq.Finish();//
            cq.Execute(computekernel, new long[] { 0 }, new long[] { threadcnt }, /*new long[] { 1 }*/null, ComputeEventsCollection);

            pass_iters = Math.Min((int)(pass_iters * 1.1), 50000);
            rendersteps++;

            cq.Finish();//
        }

        bool rendering = false;

        public void StartRendering()
        {
            if (!rendering)
            {
                rendering = true;
                RenderFrame();
            }
        }
        public void StopRendering()
        {
            rendering = false;
        }

        public bool UpdateDisplayOnRender { get; set; } = true;
        public event EventHandler RenderFrameCompleted;
        public event EventHandler DisplayFrameCompleted;
        private void RenderFrame_Completed(object sender, ComputeCommandStatusArgs args)
        {
            renderwatch.Stop();
            Debug.WriteLine($"RENDER: {renderwatch.ElapsedMilliseconds}ms");
            ComputeEventsCollection.Remove(args.Event);
            RenderFrameCompleted?.Invoke(this, null);//TODO: pass useful args
            if (UpdateDisplayOnRender)
                UpdateDisplay();
            else if (rendering)
                RenderFrame();
        }

        public void UpdateDisplay()
        {
            displaywatch.Restart();
            //Task.Run(() =>
            //{

            //cq.Finish();//
            cq.WriteToBuffer<float>(new float[] { /*threadcnt**/rendersteps/**width*height*/ , CurrentParams.Camera.Brightness, CurrentParams.Camera.Gamma }, dispsettingsbuf, false/**/, null);
            if (texturetarget > -1)//van gl
                cq.AcquireGLObjects(new ComputeMemory[] { dispimg }, null);//
            cq.Execute(displaykernel, new long[] { 0 }, new long[] { Width * Height }, /*new long[] { 1 }*/null, DisplayEventsCollection);
            if (texturetarget > -1)//van gl
                cq.ReleaseGLObjects(new ComputeMemory[] { dispimg }, null);//
            //cq.Finish();
            //});
        }

        private void DisplayFrame_Completed(object sender, ComputeCommandStatusArgs args)
        {
            displaywatch.Stop();
            Debug.WriteLine($"DISPLAY: {displaywatch.ElapsedMilliseconds}ms");

            DisplayEventsCollection.Remove(args.Event);
            DisplayFrameCompleted?.Invoke(this, null);//TODO: pass useful args
            if (rendering)
                RenderFrame();
        }

        public double[,][] GenerateImage()
        {
           float[] d = new float[Width * Height * 4];//rgba

            UpdateDisplay();//ebbol event jon

            cq.Finish();//
            cq.ReadFromBuffer(dispbuf, ref d, true, null);

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
            cq.Finish();//wait until queue is empty

            calcbuf.Dispose();
            dispbuf.Dispose();
            iteratorsbuf.Dispose();
            pointsstatebuf.Dispose();

            prog1.Dispose();
            computekernel.Dispose();
            cq.Dispose();
            ctx.Dispose();
        }

    }
}
