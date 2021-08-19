using Microsoft.Toolkit.Mvvm.ComponentModel;
using IFSEngine.Model;
using IFSEngine.Rendering;
using OpenTK;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenTK.Windowing.Desktop;
using IFSEngine.Generation;
using System.Threading.Tasks;

namespace WpfDisplay.Models
{
    /// <summary>
    /// The generator workspace model that contains a utility <see cref="RendererGL"/> 
    /// and lists of <see cref="IFSEngine.Model.IFS"/> that it is rendering.
    /// </summary>
    public class GeneratorWorkspace : ObservableObject
    {
        public IReadOnlyList<IFS> PinnedIFS => pinnedIFS;
        public IReadOnlyList<IFS> GeneratedIFS => generatedIFS;
        public IReadOnlyDictionary<IFS, ImageSource> Thumbnails => thumbnails;

        

        private RendererGL renderer;
        private Generator generator;
        private Dictionary<IFS, ImageSource> thumbnails = new Dictionary<IFS, ImageSource>();
        private List<IFS> pinnedIFS = new List<IFS>();
        private List<IFS> generatedIFS = new List<IFS>();
        private ConcurrentQueue<IFS> renderQueue = new ConcurrentQueue<IFS>();

        public GeneratorWorkspace()
        {
            //init thumbnail renderer
            GameWindow hw = new(new GameWindowSettings
            {
                IsMultiThreaded=true
            }, new NativeWindowSettings
            {
                Flags = OpenTK.Windowing.Common.ContextFlags.Offscreen,
                IsEventDriven = true,
                StartFocused = false,
                StartVisible = false,
                WindowState = OpenTK.Windowing.Common.WindowState.Minimized
            });
            renderer = new RendererGL(hw.Context);
            renderer.SetDisplayResolution(200, 200);
            var transforms = System.IO.Directory.GetFiles(@".\Functions\Transforms")
                .Select(file => TransformFunction.FromFile(file)).ToList();//TODO: use existing functions
            generator = new Generator(transforms);
            renderer.Initialize(transforms);
            //performance settings
            renderer.SetWorkgroupCount(10).Wait();
        }

        public async Task GenerateNewRandomBatch(GeneratorOptions options)
        {
            options.baseParams = pinnedIFS.LastOrDefault() ?? new IFS();//TODO: use selection of pinned fractals
            generatedIFS.Clear();
            await foreach (IFS r in generator.GenerateBatch(options, 30))
            {
                generatedIFS.Add(r);
                renderQueue.Enqueue(r);
            }
        }

        public void PinIFS(IFS ifs)
        {
            pinnedIFS.Add(ifs);
            if(!thumbnails.ContainsKey(ifs))
                renderQueue.Enqueue(ifs);
        }

        public void UnpinIFS(IFS ifs)
        {
            pinnedIFS.Remove(ifs);
        }

        public void processQueue()
        {
            //TODO: separate thread, make context current
            lock (renderer)
            {
                while (renderQueue.TryDequeue(out IFS ifs))
                {
                    renderer.LoadParams(ifs);
                    renderer.SetHistogramScaleToDisplay();
                    renderer.DispatchCompute();
                    renderer.RenderImage();
                    WriteableBitmap wbm = new WriteableBitmap(renderer.HistogramWidth, renderer.HistogramHeight, 96, 96, PixelFormats.Bgra32, null);
                    renderer.CopyPixelDataToBitmap(wbm.BackBuffer).Wait();
                    wbm.Freeze();
                    var thumbnail = new FormatConvertedBitmap(wbm, PixelFormats.Bgr32, null, 0);
                    thumbnails[ifs] = thumbnail;
                    OnPropertyChanged(nameof(Thumbnails));
                }
            }
            //cleanup old thumbnails
            var removedParams = thumbnails.Keys.Where(k => !(pinnedIFS.Contains(k) || generatedIFS.Contains(k))).ToList();
            foreach (var t in removedParams)
                thumbnails.Remove(t);

        }


    }
}
