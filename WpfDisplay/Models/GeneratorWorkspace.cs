using GalaSoft.MvvmLight;
using IFSEngine;
using IFSEngine.Model;
using OpenTK;
using OpenTK.Platform;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        }


        public void PinIFS(IFS ifs)
        {
            pinnedIFS.Add(ifs);
            renderQueue.Enqueue(ifs);
        }

        public async Task processQueue()
        {
            //TODO: separate thread, make context current
            while (renderQueue.TryDequeue(out IFS ifs))
            {
                renderer.LoadParams(ifs);
                await renderer.SetHistogramScaleToDisplay();
                renderer.UpdateDisplay();
                renderer.RenderFrame();
                WriteableBitmap wbm = new WriteableBitmap(renderer.HistogramWidth, renderer.HistogramHeight, 96, 96, PixelFormats.Bgra32, null);
                await renderer.CopyPixelDataToBitmap(wbm.BackBuffer);
                wbm.Freeze();
                var thumbnail = new FormatConvertedBitmap(wbm, PixelFormats.Bgr32, null, 0);
                thumbnails[ifs] = thumbnail;
                RaisePropertyChanged(() => Thumbnails);
            }
        }


    }
}
