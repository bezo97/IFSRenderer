#nullable enable
using Cavern.Format.Renderers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFSEngine.Generation;
using IFSEngine.Model;
using IFSEngine.Rendering;
using IFSEngine.Utility;
using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfDisplay.Properties;
using WpfDisplay.Serialization;
using Transform = IFSEngine.Model.Transform;

namespace WpfDisplay.ViewModels;

public sealed partial class WelcomeViewModel : ObservableObject
{
    private readonly IReadOnlyCollection<Transform> _loadedTransforms;

    public RelayCommand? ContinueCommand { get; set; }
    public WelcomeWorkflow SelectedWorkflow { get; private set; } = WelcomeWorkflow.FromScratch;
    public IFS ExploreParams { get; private set; } = new IFS();

    [ObservableProperty] private string _selectedExpander = "0";
    [ObservableProperty] private ImageSource? _exploreThumbnail;
    [ObservableProperty] private List<KeyValuePair<IFS, ImageSource>> _templates = new();
    [ObservableProperty] private List<KeyValuePair<IFS, ImageSource>> _recentFiles = new();

    public WelcomeViewModel(IReadOnlyCollection<Transform> loadedTransforms)
    {
        _loadedTransforms = loadedTransforms;
    }

    public async Task Initialize()
    {
        //init thumbnail renderer
        GameWindow hw = new(new GameWindowSettings(), new NativeWindowSettings
        {
            Flags = OpenTK.Windowing.Common.ContextFlags.Offscreen,
            IsEventDriven = true,
            StartFocused = false,
            StartVisible = false,
            WindowState = OpenTK.Windowing.Common.WindowState.Minimized
        });
        var renderer = new RendererGL(hw.Context);
        renderer.SetDisplayResolution(100, 100);
        await renderer.Initialize(_loadedTransforms);
        await renderer.SetWorkgroupCount(100);

        ExploreParams = new Generator(_loadedTransforms).GenerateOne(new());
        ExploreThumbnail = RenderThumbnail(renderer, ExploreParams);

        //load template files and recent files.
        List<Task<IFS>> templateFilesTasks = new();
        List<Task<IFS>> recentFilesTasks = new();
        try
        {
            templateFilesTasks = Directory
                .GetFiles(App.TemplatesDirectoryPath, "*.ifsjson")
                .Select(templatePath => IfsNodesSerializer.LoadJsonFileAsync(templatePath, _loadedTransforms, true))
                .ToList();
            var recentFilePaths = Settings.Default.RecentFiles.Cast<string>();
            recentFilesTasks = recentFilePaths
                .Select(recentPath => IfsNodesSerializer.LoadJsonFileAsync(recentPath, _loadedTransforms, true))
                .ToList();
            await Task.WhenAll(templateFilesTasks.Concat(recentFilesTasks));
        }
        catch (System.Runtime.Serialization.SerializationException){ /*Ignore broken files*/ }

        //render thumbnails
        Templates = templateFilesTasks
            .Where(t=>t.IsCompletedSuccessfully).Select(t=>t.Result)
            .ToDictionary(p => p, p => RenderThumbnail(renderer, p))
            .ToList();

        RecentFiles = recentFilesTasks
            .Where(t => t.IsCompletedSuccessfully).Select(t => t.Result)
            .ToDictionary(p => p, p => RenderThumbnail(renderer, p))
            .ToList();
    }

    private static ImageSource RenderThumbnail(RendererGL renderer, IFS ifs)
    {
        //modify settings to be optimal for thumbnail rendering
        var previewIfs = ifs.DeepClone();
        previewIfs.ImageResolution = new System.Drawing.Size(100, 100);
        previewIfs.Entropy = 1.0 / 100;
        previewIfs.Warmup = 0;
        //render image after 1 compute pass
        renderer.LoadParams(previewIfs);
        renderer.SetHistogramScaleToDisplay();
        renderer.DispatchCompute();
        renderer.RenderImage();
        //read rendered image data
        WriteableBitmap wbm = new WriteableBitmap(renderer.HistogramWidth, renderer.HistogramHeight, 96, 96, PixelFormats.Bgra32, null);
        renderer.CopyPixelDataToBitmap(wbm.BackBuffer).Wait();
        wbm.Freeze();
        //save thumbnail image
        var thumbnail = new FormatConvertedBitmap(wbm, PixelFormats.Bgr32, null, 0);
        thumbnail.Freeze();
        return thumbnail;
    }

    [RelayCommand]
    private void SelectWorkflow(WelcomeWorkflow selection)
    {
        SelectedWorkflow = selection;
        ContinueCommand?.Execute(null);
    }

    [RelayCommand]
    private void SelectExploreWorkflow(IFS? ifs)
    {
        SelectedWorkflow = WelcomeWorkflow.Explore;
        if (ifs is not null)
            ExploreParams = ifs;
        ContinueCommand?.Execute(null);
    }
}
