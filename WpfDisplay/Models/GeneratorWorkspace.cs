using GalaSoft.MvvmLight;
using IFSEngine.Model;
using IFSEngine.Rendering;
using OpenTK;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

        public List<TransformFunction> LoadedTransforms { get; }

        private RendererGL renderer;
        private Dictionary<IFS, ImageSource> thumbnails = new Dictionary<IFS, ImageSource>();
        private List<IFS> pinnedIFS = new List<IFS>();
        private List<IFS> generatedIFS = new List<IFS>();
        private ConcurrentQueue<IFS> renderQueue = new ConcurrentQueue<IFS>();

        public GeneratorWorkspace()
        {
            //init thumbnail renderer
            GameWindow hw = new GameWindow();
            renderer = new RendererGL(hw.WindowInfo);
            renderer.SetDisplayResolution(100, 100);
            LoadedTransforms = System.IO.Directory.GetFiles(@".\Functions\Transforms")
                .Select(file => TransformFunction.FromFile(file)).ToList();//TODO: use existing functions
            renderer.Initialize(LoadedTransforms);
            //performance settings
            renderer.setWorkgroupCount(10).Wait();
            renderer.PassIters = 100;
        }

        public void GenerateNewRandomBatch(int batchSize)
        {
            generatedIFS.Clear();
            for (int i = 0; i < batchSize; i++)
            {
                GenerateRandom();
            }
        }

        public void GenerateRandom()
        {
            var r = IFS.GenerateRandom(LoadedTransforms);
            r.ImageResolution = new System.Drawing.Size(1080, 1080);
            generatedIFS.Add(r);
            renderQueue.Enqueue(r);
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
                    renderer.UpdateDisplay();
                    renderer.DispatchCompute();
                    renderer.RenderImage();
                    WriteableBitmap wbm = new WriteableBitmap(renderer.HistogramWidth, renderer.HistogramHeight, 96, 96, PixelFormats.Bgra32, null);
                    renderer.CopyPixelDataToBitmap(wbm.BackBuffer).Wait();
                    wbm.Freeze();
                    var thumbnail = new FormatConvertedBitmap(wbm, PixelFormats.Bgr32, null, 0);
                    thumbnails[ifs] = thumbnail;
                    RaisePropertyChanged(() => Thumbnails);
                }
            }
            //cleanup old thumbnails
            var removedParams = thumbnails.Keys.Where(k => !(pinnedIFS.Contains(k) || generatedIFS.Contains(k))).ToList();
            foreach (var t in removedParams)
                thumbnails.Remove(t);

        }


    }
}
