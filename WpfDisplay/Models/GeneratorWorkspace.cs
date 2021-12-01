using IFSEngine.Generation;
using IFSEngine.Model;
using IFSEngine.Rendering;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using OpenTK.Windowing.Desktop;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfDisplay.Models;

/// <summary>
/// The generator workspace model that contains a utility <see cref="RendererGL"/> 
/// and lists of <see cref="IFSEngine.Model.IFS"/> that it is rendering.
/// </summary>
[ObservableObject]
public partial class GeneratorWorkspace
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

    /// <summary>
    /// Call <see cref="Initialize"/> before using
    /// </summary>
    /// <param name="loadedTransforms"></param>
    public GeneratorWorkspace(IReadOnlyCollection<IFSEngine.Model.Transform> loadedTransforms)
    {
        //init thumbnail renderer
        GameWindow hw = new(new GameWindowSettings
        {
            IsMultiThreaded = true
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
        var transforms = loadedTransforms.ToList();
        generator = new Generator(transforms);
    }

    public async Task Initialize()
    {
        await renderer.Initialize(generator.SelectedTransforms);
        //performance settings
        await renderer.SetWorkgroupCount(10);
    }

    public void GenerateNewRandomBatch(GeneratorOptions options)
    {
        options.baseParams = pinnedIFS.LastOrDefault() ?? options.baseParams;//TODO: use selection of pinned fractals
        generatedIFS.Clear();

        foreach (var r in generator.GenerateBatch(options, 30))
        {
            generatedIFS.Add(r);
            renderQueue.Enqueue(r);
        }
    }

    public void PinIFS(IFS ifs)
    {
        pinnedIFS.Add(ifs);
        if (!thumbnails.ContainsKey(ifs))
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
