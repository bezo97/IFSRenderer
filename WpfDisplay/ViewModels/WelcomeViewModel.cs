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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfDisplay.Models;
using Transform = IFSEngine.Model.Transform;

namespace WpfDisplay.ViewModels;

public sealed partial class WelcomeViewModel : ObservableObject
{
    private readonly IReadOnlyCollection<Transform> _loadedTransforms;

    public EventHandler<WelcomeWorkflow>? WorkflowSelected;
    public WelcomeWorkflow SelectedWorkflow { get; private set; } = WelcomeWorkflow.FromScratch;
    public IFS ExploreParams { get; private set; } = new IFS();

    [ObservableProperty] private string _selectedExpander = "0";
    [ObservableProperty] private ImageSource? _exploreThumbnail;
    [ObservableProperty] private IEnumerable<KeyValuePair<IFS, ImageSource>> _templates;
    [ObservableProperty] private IEnumerable<KeyValuePair<IFS, ImageSource>> _recentFiles;

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

        var templateParams = new Generator(_loadedTransforms).GenerateBatch(new() { BatchSize = 5 });
        Templates = templateParams.ToDictionary(p => p, p => RenderThumbnail(renderer, p)).ToList();

        var recentFilesParams = new Generator(_loadedTransforms).GenerateBatch(new() { BatchSize = 5 });
        RecentFiles = templateParams.ToDictionary(p => p, p => RenderThumbnail(renderer, p)).ToList();

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
    private void StartFromScratch()
    {
        SelectedWorkflow = WelcomeWorkflow.FromScratch;
        WorkflowSelected?.Invoke(this, SelectedWorkflow);
    }

    [RelayCommand]
    private void LoadFile()
    {
        SelectedWorkflow = WelcomeWorkflow.LoadFile;
        WorkflowSelected?.Invoke(this, SelectedWorkflow);
    }

    [RelayCommand]
    private void BrowseRandoms()
    { 
        SelectedWorkflow = WelcomeWorkflow.BrowseRandoms;
        WorkflowSelected?.Invoke(this, SelectedWorkflow);
    }

    [RelayCommand]
    private void Explore(IFS? ifs)
    {
        if(ifs is not null)
            ExploreParams = ifs;
        SelectedWorkflow = WelcomeWorkflow.Explore;
        WorkflowSelected?.Invoke(this, SelectedWorkflow);
    }

    [RelayCommand]
    private void VisitSettings()
    {
        SelectedWorkflow = WelcomeWorkflow.VisitSettings;
        WorkflowSelected?.Invoke(this, SelectedWorkflow);
    }
}
