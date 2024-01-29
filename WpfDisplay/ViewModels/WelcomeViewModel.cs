#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using IFSEngine.Generation;
using IFSEngine.Model;
using IFSEngine.Rendering;
using IFSEngine.Utility;

using OpenTK.Windowing.Desktop;

using WpfDisplay.Properties;
using WpfDisplay.Serialization;

using Transform = IFSEngine.Model.Transform;

namespace WpfDisplay.ViewModels;

public sealed partial class WelcomeViewModel : ObservableObject
{
    private readonly IReadOnlyCollection<string> _includeSources;
    private readonly IReadOnlyCollection<Transform> _loadedTransforms;

    public RelayCommand? ContinueCommand { get; set; }
    public WelcomeWorkflow SelectedWorkflow { get; private set; } = WelcomeWorkflow.FromScratch;
    public IFS ExploreParams { get; private set; } = new IFS();
    public string? SelectedFilePath { get; private set; }

    public string SelectedExpander
    {
        get => Settings.Default.WelcomeExpanderState;
        set
        {
            Settings.Default.WelcomeExpanderState = value;
            Settings.Default.Save();
            OnPropertyChanged(nameof(SelectedExpander));
        }
    }
    [ObservableProperty] private ImageSource? _exploreThumbnail;
    [ObservableProperty] private List<KeyValuePair<IFS, ImageSource>> _templates = [];
    [ObservableProperty] private List<KeyValuePair<IFS, ImageSource>> _recentFiles = [];

    public WelcomeViewModel(IReadOnlyCollection<string> includeSources, IReadOnlyCollection<Transform> loadedTransforms)
    {
        _includeSources = includeSources;
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
        await renderer.Initialize(_includeSources, _loadedTransforms);
        await renderer.SetWorkgroupCount(100);

        ExploreParams = new Generator(_loadedTransforms).GenerateOne(new());
        ExploreThumbnail = RenderThumbnail(renderer, ExploreParams);

        //load template files and recent files.
        List<Task<IFS>> templateFilesTasks = [];
        List<Task<IFS>> recentFilesTasks = [];
        try
        {
            templateFilesTasks = Directory
                .GetFiles(App.TemplatesDirectoryPath, "*.ifsjson")
                .Select(templatePath => IfsNodesSerializer.LoadJsonFileAsync(templatePath, _loadedTransforms, true))
                .ToList();
            var recentFilePaths = Settings.Default.RecentFiles.Cast<string>();
            recentFilesTasks = recentFilePaths
                .Select(recentPath => IfsNodesSerializer.LoadJsonFileAsync(recentPath, _loadedTransforms, true))
                .Reverse()
                .ToList();
            await Task.WhenAll(templateFilesTasks.Concat(recentFilesTasks));
        }
        catch (System.Runtime.Serialization.SerializationException) { /*Ignore broken files*/ }
        catch (FileNotFoundException) { /*Ignore deleted recent files*/ }

        //render thumbnails
        Templates = templateFilesTasks
            .Where(t => t.IsCompletedSuccessfully).Select(t => t.Result)
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
        if (RecentFiles.Any(f => f.Key == ifs))
        {
            SelectedWorkflow = WelcomeWorkflow.LoadRecent;
            SelectedFilePath = Settings.Default.RecentFiles[RecentFiles.Count - 1 - RecentFiles.IndexOf(RecentFiles.First(f => f.Key == ifs))];//TODO: ugh
        }
        else
            SelectedWorkflow = WelcomeWorkflow.Explore;
        if (ifs is not null)
            ExploreParams = ifs;
        ContinueCommand?.Execute(null);
    }

    [RelayCommand]
    private void PasteFromClipboard()
    {
        try
        {
            string jsonData = System.Windows.Clipboard.GetText();
            IFS ifs = IfsNodesSerializer.DeserializeJsonString(jsonData, _loadedTransforms, true);
            SelectExploreWorkflow(ifs);
        }
        catch (SerializationException) { /* Ignore when Clipboard contains no params */ }
    }

    [RelayCommand]
    private void DisableStartup()
    {
        Settings.Default.IsWelcomeShownOnStartup = false;
        Settings.Default.Save();
        ContinueCommand?.Execute(null);
    }
}
