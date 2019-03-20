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

namespace IFSEngine
{

    internal struct mwc64x_state_t
    {
        uint x;
        uint c;
    }

    internal struct Settings
    {
        internal int itnum;//length of iterators - 1 (last one is finalit)
        internal int pass_iters;//iterations per pass
        internal int rendersteps;
        internal CameraSettings camera;
    }

    public struct Affine
    {
        public float ox;
        public float oy;
        public float oz;
         
        public float xx;
        public float xy;
        public float xz;
         
        public float yx;
        public float yy;
        public float yz;

        public float zx;
        public float zy;
        public float zz;

        public Affine(float ox, float oy, float oz, float xx, float xy, float xz, float yx, float yy, float yz, float zx, float zy, float zz)
        {
            this.ox = ox;
            this.oy = oy;
            this.oz = oz;

            this.xx = xx;
            this.xy = xy;
            this.xz = xz;

            this.yx = yx;
            this.yy = yy;
            this.yz = yz;

            this.zx = zx;
            this.zy = zy;
            this.zz = zz;
        }
    }

    public struct Iterator
    {
        public Affine aff;
        public int tfID;
        public float w;
        public float cs;
        public float ci;//color index, 0 - 1
        public float op;

        public Iterator(Affine aff, int tfID, float w, float cs, float ci, float op)
        {
            this.aff = aff;
            this.tfID = tfID;
            this.w = w;
            this.cs = cs;
            this.ci = ci;
            this.op = op;
        }
    }

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
        ComputeBuffer<float>/*<double4>*/ calcbuf;
        ComputeImage2D/*<double4>*/ dispimg;
        ComputeBuffer<float> dispbuf;
        ComputeBuffer<float>/*<double>*/ dispsettingsbuf;
        ComputeBuffer<Iterator>/*<double>*/ iteratorsbuf;
        ComputeBuffer<Settings>/*<double>*/ settingsbuf;
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

        int threadcnt = 1500;//gtx970: 1664, 610m: 48
        int width;
        int height;
        int texturetarget;
        Random rgen = new Random();
        public int rendersteps = 0;
        List<Iterator> its = new List<Iterator>();
        Iterator finalit;
        Settings settings = new Settings();//todo init

        //float[] pre_rnd;
        //float[] rnd;

        public Renderer(int width, int height, IntPtr glctxh, int texturetarget)
        {
            this.width = width;
            this.height = height;
            this.texturetarget = texturetarget;

            //e.OnAnyError(ec => throw new Exception(ec.ToString()));

            string kernelSource = "#define width (" + width + ")\r\n";
            kernelSource += "#define height (" + height + ")\r\n";
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
            calcbuf = new Cloo.ComputeBuffer<float>(ctx, ComputeMemoryFlags.ReadWrite, width * height * 4);//rgba
            dispbuf = new Cloo.ComputeBuffer<float>(ctx, ComputeMemoryFlags.WriteOnly, width * height * 4);//rgba
            if (glctxh.ToInt32() > 0)
            {
                dispimg = Cloo.ComputeImage2D.CreateFromGLTexture2D(ctx, ComputeMemoryFlags.WriteOnly, 3553/*gl texture2d*/, 0, texturetarget);
                //dispimg = Cloo.ComputeImage2D.CreateFromGLRenderbuffer(ctx, ComputeMemoryFlags.WriteOnly, texturetarget/*buffertarget*/);
            }
            else
                dispimg = new Cloo.ComputeImage2D(ctx, ComputeMemoryFlags.WriteOnly, new ComputeImageFormat(ComputeImageChannelOrder.Rgba, ComputeImageChannelType.Float), width, height, 0, IntPtr.Zero);
            dispsettingsbuf = new Cloo.ComputeBuffer<float>(ctx, ComputeMemoryFlags.ReadOnly, 3);
            //iteratorsbuf = new ComputeBuffer<Iterator>(ctx, ComputeMemoryFlags.ReadOnly, its.Count+1/*+final*/);//ezt load nal
            settingsbuf = new ComputeBuffer<Settings>(ctx, ComputeMemoryFlags.ReadOnly, 1);
            pointsstatebuf = new ComputeBuffer<float>(ctx, ComputeMemoryFlags.ReadWrite, threadcnt * 4);//minden szal pontjat mentjuk a kovi passba
            rng_states = new ComputeBuffer<mwc64x_state_t>(ctx, ComputeMemoryFlags.ReadWrite, threadcnt);

            computekernel.SetMemoryArgument(0, calcbuf);
            //computekernel.SetMemoryArgument(1, randbuf);
            //computekernel.SetMemoryArgument(1, iteratorsbuf);//ezt initben
            computekernel.SetMemoryArgument(2, settingsbuf);
            computekernel.SetMemoryArgument(3, pointsstatebuf);
            computekernel.SetMemoryArgument(4, rng_states);

            displaykernel.SetMemoryArgument(0, calcbuf);
            displaykernel.SetMemoryArgument(1, dispbuf);
            displaykernel.SetMemoryArgument(2, dispimg);
            displaykernel.SetMemoryArgument(3, dispsettingsbuf);
        }

        public Camera Camera { get; set; } = new Camera();

        private bool invalid = false;
        public void InvalidateAccumulation()
        {
            //tobben is hivhatjak, de eleg egyszer resetelni, ezert invalidate
            if (!invalid)
            {
                //ez elv redundans, de megis reszponzivabb??
                cq.WriteToBuffer<float>(new float[width * height * 4], calcbuf, false, null);
            }
            invalid = true;
            
        }

        //TODO: kulon valaszt params es camera update
        public void UpdateParams(List<Iterator> its, Iterator finalit)
        {
            cq.WriteToBuffer<float>(StartingDistributions.UniformUnitCube(threadcnt), pointsstatebuf, false, null);
            if (its.Count != this.its.Count)
            {
                var bufferHandle = iteratorsbuf?.Handle;
                iteratorsbuf = new ComputeBuffer<Iterator>(ctx, ComputeMemoryFlags.ReadOnly, its.Count + 1/*its+final*/);
                bufferHandle?.Invalidate();//nem tudjuk csak siman Dispose() olni, mert lehet hogy epp szukseg van ra. Invalidate: A kovetkezo framek mar az ujt hasznaljak
                //TODO: de most leak??
                computekernel.SetMemoryArgument(1, iteratorsbuf);
            }
            this.its = its;
            this.finalit = finalit;
            rendersteps = 0;
            this.settings.itnum = its.Count;
            this.settings.pass_iters = 64;//minden renderrel duplazodik
            this.settings.rendersteps = rendersteps;//0
            //this.settings.camera = Camera.Settings;
            rndcarousel = 0;
            List<Iterator> its_and_final = new List<Iterator>(its);
            its_and_final.Add(finalit);
            cq.WriteToBuffer<Iterator>(its_and_final.ToArray(), iteratorsbuf, false, null);
            //cq.WriteToBuffer<Settings>(new Settings[] { settings }, settingsbuf, true, null);//ezt a renderben kell ugyis
            InvalidateAccumulation();

            //Render();
        }

        int rndcarousel = 0;
        int carouselLength = 0;

        public void Render()
        {
            //if volt init()

            //StartingDistributions teszt:
            //cq.WriteToBuffer<float>(StartingDistributions.UniformUnitCube(threadcnt), pointsstatebuf, false, null);

            if(invalid)
            {
                cq.WriteToBuffer<float>(new float[width * height * 4], calcbuf, false, null);
                invalid = false;
            }

            settings.pass_iters = Math.Min(settings.pass_iters * 2, 10000);
            settings.camera = Camera.Settings;
            settings.rendersteps = rendersteps;
            cq.WriteToBuffer<Settings>(new Settings[] { settings }, settingsbuf, true, null);//camera motion blur es pass_iters miatt

            //e = Cl.EnqueueMarker(cq, out Event start);

            cq.Execute(computekernel, new long[] { 0 }, new long[] { threadcnt }, /*new long[] { 1 }*/null, null);
            //cq.Finish();
            rendersteps++;
            /*if (rendersteps == 10)
            {
                rendersteps = 100;
                this.settings.max_iters = 10000;//Math.Min(10000, (int)(this.settings.max_iters += 3000));
            }*/

            //motionblur pr
            //this.settings.camera.ox = (float)Math.Pow(new Random().NextDouble()-0.5,0.25);

            //e = Cl.EnqueueMarker(cq, out Event end);
            //e = Cl.Finish(cq);

            //InfoBuffer startb = Cl.GetEventProfilingInfo(start, ProfilingInfo.Queued, out e);
            //InfoBuffer endb = Cl.GetEventProfilingInfo(end, ProfilingInfo.End, out e);
            //Debug.WriteLine("Calc time: " + (int.Parse(endb.ToString()) - int.Parse(startb.ToString())) / 1000000);
            //});
        }

        public void UpdateDisplay(float brightness, float gamma)
        {
            //Task.Run(() =>
            //{

            //cq.Finish();//
            cq.WriteToBuffer<float>(new float[] { /*threadcnt**/rendersteps/**width*height*/ , brightness, gamma }, dispsettingsbuf, false/**/, null);
            if (texturetarget > -1)//van gl
                cq.AcquireGLObjects(new ComputeMemory[] { dispimg }, null);//
            cq.Execute(displaykernel, new long[] { 0 }, new long[] { width * height }, /*new long[] { 1 }*/null, null);
            if (texturetarget > -1)//van gl
                cq.ReleaseGLObjects(new ComputeMemory[] { dispimg }, null);//
            //cq.Finish();
            //});
        }

        //TODO: rendberak kepbe iras
        /*public double[,][] Img(float brightness, float gamma)
        {
                float[] d = new float[width * height * 4];//rgba

                //UpdateDisplay()
                cq.WriteToBuffer<float>(new float[] { threadcnt * rendersteps /2(?), brightness, gamma }, dispsettingsbuf, true, null);
                if (texturetarget > -1)//van gl
                    cq.AcquireGLObjects(new ComputeMemory[] { dispimg }, null);//
                cq.Execute(displaykernel, new long[] { 0 }, new long[] { width * height }, new long[] { 1 }, null);
                cq.ReadFromBuffer<float>(dispbuf, ref d, true, null);
                cq.Finish();
                if (texturetarget > -1)//van gl
                    cq.ReleaseGLObjects(new ComputeMemory[] { dispimg }, null);//

                double[,][] o = new double[width, height][];
                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                    {
                        o[x, y] = new double[3];
                        o[x, y][0] = d[x * 4 + y * 4 * width + 0];//?
                        o[x, y][1] = d[x * 4 + y * 4 * width + 1];
                        o[x, y][2] = d[x * 4 + y * 4 * width + 2];
                        //s3 opacity
                    }
                return o;
        }*/

        public void Dispose()
        {
            calcbuf.Dispose();
            //randbuf.Dispose();
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
