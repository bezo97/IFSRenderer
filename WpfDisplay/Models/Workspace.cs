#nullable enable
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using IFSEngine.Generation;
using IFSEngine.Model;
using IFSEngine.Rendering;
using IFSEngine.Serialization;

using WpfDisplay.Helper;
using WpfDisplay.Properties;
using WpfDisplay.Serialization;

namespace WpfDisplay.Models;

/// <summary>
/// The main workspace model that contains a <see cref="RendererGL"/> 
/// and an <see cref="IFSEngine.Model.IFS"/> that it is rendering.
/// </summary>
public partial class Workspace : ObservableObject
{
    private readonly IFSHistoryTracker _tracker = new();
    private List<string> _includeSources = [];
    private List<Transform> _loadedTransforms = [];

    public event EventHandler<string>? StatusTextChanged;
    public event EventHandler? LoadedParamsChanged;
    public IReadOnlyCollection<Transform> LoadedTransforms => _loadedTransforms;
    public IReadOnlyCollection<string> IncludeSources => _includeSources;
    public IReadOnlyDictionary<string, int[]> ResolutionPresets { get; private set; } = new Dictionary<string, int[]>();
    public IReadOnlyDictionary<string, string> FfmpegPresets { get; private set; } = new Dictionary<string, string>();
    public Author CurrentUser { get; set; } = Author.Unknown;
    public bool TransparentBackground { get; set; } = false;
    public bool IsFinalRenderingMode = false;
    public bool InvertAxisY;
    public double Sensitivity;
    public bool UseWhiteForBlankParams;
    public bool IsRawFrameExportEnabled { get; private set; }
    public bool IsExportVideoFileEnabled { get; private set; }
    public string? FfmpegPath { get; private set; }
    public string? FfmpegArgs { get; private set; }
    public bool SaveMetadata { get; private set; }
    public bool IncludeParamsInMetadata { get; private set; }
    public string? EditedFilePath { get; private set; }
    public static IReadOnlyList<string> RecentFilePaths
    {
        get
        {
            var paths = new string[Settings.Default.RecentFiles.Count];
            Settings.Default.RecentFiles.CopyTo(paths, 0);
            return paths;
        }
    }
    [ObservableProperty] private bool _hasUnsavedChanges;

    private RendererGL _renderer = null!;
    public RendererGL Renderer
    {
        get => _renderer;
        set
        {
            SetProperty(ref _renderer, value);
            if (Ifs != null)
                _renderer.LoadParams(Ifs);
        }
    }

    public IFS Ifs { get; private set; } = null!;

    public bool IsHistoryUndoable => _tracker.IsHistoryUndoable;
    public bool IsHistoryRedoable => _tracker.IsHistoryRedoable;

    public static string[] TemplateFilePaths => Directory.GetFiles(App.TemplatesDirectoryPath, "*.ifsjson");

    /// <summary>
    /// Call <see cref="Initialize"/> before using
    /// </summary>
    /// <param name="r"></param>
    public Workspace(RendererGL r)
    {
        Renderer = r;
    }

    public async Task Initialize()
    {
        await ApplyUserSettings();
        await LoadLibrary();
        await Renderer.Initialize(_includeSources, _loadedTransforms);

        Ifs = new IFS();
        Renderer.LoadParams(Ifs);
    }


    private async Task LoadLibrary()
    {
        await LoadSourceLibrary();
        ResolutionPresets = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int[]>>(
            await File.ReadAllTextAsync(Path.Combine(App.LibraryDirectoryPath, "ResolutionPresets.json"))) ?? [];
        FfmpegPresets = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(
            await File.ReadAllTextAsync(Path.Combine(App.LibraryDirectoryPath, "FfmpegPresets.json"))) ?? [];
    }

    private async Task LoadSourceLibrary()
    {
        //load includes
        var includeFiles = Directory.GetFiles(App.IncludesDirectoryPath);
        var loadIncludeTasks = includeFiles.Select(file => File.ReadAllTextAsync(file));
        _includeSources = (await Task.WhenAll(loadIncludeTasks)).ToList();
        //load transforms
        var loadTransformTasks = Directory
            .GetFiles(App.TransformsDirectoryPath, "*.ifstf", SearchOption.AllDirectories)
            .Select(file => Transform.FromFile(file));
        var allTransforms = (await Task.WhenAll(loadTransformTasks)).ToList();
        //skip transforms with missing includes
        var includeNames = includeFiles.Select(i => Path.GetFileName(i)).ToList();
        var skippedTransforms = allTransforms.Where(t => t.IncludeUses.Any(u => !includeNames.Contains(u)));
        //TODO: notify user of skipped transforms and missing includes
        _loadedTransforms = allTransforms.Except(skippedTransforms).ToList();
        OnPropertyChanged(nameof(LoadedTransforms));
    }

    public async Task ReloadTransforms()
    {
        await LoadSourceLibrary();
        Ifs.ReloadTransforms(LoadedTransforms);
        await Renderer.LoadTransforms(_includeSources, LoadedTransforms);
        Renderer.StartRenderLoop();
        OnPropertyChanged(nameof(LoadedTransforms));
    }

    public void LoadParams(IFS ifs, string? loadedFilePath)
    {
        TakeSnapshot();
        _renderer?.LoadParams(ifs);
        foreach (var i in Ifs.Iterators)//keep node positions for same iterators
            ifs.Iterators.FirstOrDefault(i2 => i2.Id == i.Id)?.SetPosition(i.GetPosition());
        Ifs = ifs;
        SetEditedFilePath(loadedFilePath);
        HasUnsavedChanges = false;
        if (!Renderer.IsRendering)
            Renderer.StartRenderLoop();
        LoadedParamsChanged?.Invoke(this, EventArgs.Empty);
        OnPropertyChanged(nameof(Ifs));
    }

    /// <param name="isTemplate">Whether to consider the loaded file as a template so it cannot be saved over. Pass false to consider it being edited.</param>
    public async Task LoadParamsFileAsync(string path, bool isTemplate)
    {
        IFS ifs;
        try
        {
            ifs = await IfsNodesSerializer.LoadJsonFileAsync(path, LoadedTransforms, false);
        }
        catch (System.Runtime.Serialization.SerializationException ex)
            when (ex.InnerException is AggregateException exs)
        {
            var unknownTransformNames = exs.InnerExceptions.Select(e => ((UnknownTransformException)e).TransformName);
            if (unknownTransformNames.All(transformName => LoadedTransforms.Any(t => t.Name == transformName)))
            {
                ifs = await IfsNodesSerializer.LoadJsonFileAsync(path, LoadedTransforms, true);
                System.Windows.MessageBox.Show($"The loaded file was created using different versions of the following transforms:\r\n{string.Join(", ", unknownTransformNames)}.", "Warning");
            }
            else
                throw;
        }
        LoadParams(ifs, isTemplate ? null : path);
    }

    public void RaiseAnimationFrameChanged() => OnPropertyChanged("AnimationFrame");

    public void LoadBlankParams()
    {
        var blankParams = new IFS();
        if (!UseWhiteForBlankParams)
            blankParams.Palette = Generator.GenerateRandomIqPalette();
        LoadParams(blankParams, null);
    }

    public void LoadRandomParams()
    {
        Generator g = new Generator(LoadedTransforms);
        IFS r = g.GenerateOne(new GeneratorOptions());
        r.ImageResolution = new System.Drawing.Size(1920, 1080);
        LoadParams(r, null);
    }

    public async Task SaveParamsAsync()
    {
        if (EditedFilePath == null)
            throw new InvalidOperationException();
        await SaveParamsAsAsync(EditedFilePath);
    }

    public async Task SaveParamsAsAsync(string path)
    {
        Ifs.AddAuthor(CurrentUser);
        await IfsNodesSerializer.SaveJsonFileAsync(Ifs, path);
        SetEditedFilePath(path);
        HasUnsavedChanges = false;
    }

    private void SetEditedFilePath(string? path)
    {
        EditedFilePath = path;
        if (path != null)
        {//update recent files list
            var paths = Settings.Default.RecentFiles;
            if (paths.Contains(path))
                paths.Remove(path);
            paths.Add(path);
            if (paths.Count > 5)
                paths.RemoveAt(0);
            Settings.Default.Save();
            OnPropertyChanged(nameof(RecentFilePaths));
        }
        OnPropertyChanged(nameof(EditedFilePath));
    }

    public void CopyToClipboard()
    {
        string jsonData = IfsNodesSerializer.SerializeJsonString(Ifs);
        System.Windows.Clipboard.SetText(jsonData);
    }

    public void PasteFromClipboard()
    {
        string jsonData = System.Windows.Clipboard.GetText();
        IFS ifs = IfsNodesSerializer.DeserializeJsonString(jsonData, LoadedTransforms, true);
        LoadParams(ifs, null);
    }

    public void UndoHistory()
    {
        //LoadParams without taking snapshot
        Ifs = _tracker.Undo(Ifs, LoadedTransforms);
        _renderer?.LoadParams(Ifs);
        LoadedParamsChanged?.Invoke(this, EventArgs.Empty);
        OnPropertyChanged(nameof(Ifs));
    }

    public void RedoHistory()
    {
        //LoadParams without taking snapshot
        Ifs = _tracker.Redo(Ifs, LoadedTransforms);
        _renderer?.LoadParams(Ifs);
        LoadedParamsChanged?.Invoke(this, EventArgs.Empty);
        OnPropertyChanged(nameof(Ifs));
    }

    public void ClearHistory() => _tracker.Clear();

    public void TakeSnapshot()
    {
        _tracker.TakeSnapshot(Ifs);
        HasUnsavedChanges = true;
    }

    public async Task ApplyUserSettings()
    {
        if (!ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).HasFile)
        {//migrate user settings from previous version
            Settings.Default.Upgrade();
            Settings.Default.Save();
        }
        Renderer.EnablePerceptualUpdates = Settings.Default.PerceptuallyUniformUpdates;

        await Renderer.SetWorkgroupCount(Settings.Default.WorkgroupCount);

        Renderer.TargetFramerate = Settings.Default.TargetFramerate;
        InvertAxisY = Settings.Default.InvertAxisY;
        Sensitivity = Settings.Default.Sensitivity;
        UseWhiteForBlankParams = Settings.Default.UseWhiteForBlankParams;
        IsRawFrameExportEnabled = Settings.Default.IsRawFrameExportEnabled;
        IsExportVideoFileEnabled = Settings.Default.IsExportVideoFileEnabled;
        FfmpegPath = Settings.Default.FfmpegPath;
        FfmpegArgs = Settings.Default.FfmpegArgs;
        SaveMetadata = Settings.Default.SaveMetadata;
        IncludeParamsInMetadata = Settings.Default.IncludeParamsInMetadata;
        CurrentUser = new Author
        {
            Name = Settings.Default.AuthorName,
            Link = Settings.Default.AuthorLink
        };
    }

    public void SetFinalResolution(int imageWidth, int imageHeight)
    {
        Ifs.ImageResolution = new System.Drawing.Size(imageWidth, imageHeight);
        if (IsFinalRenderingMode)
            Renderer.SetHistogramScale(1.0);
        else
            Renderer.SetHistogramScaleToDisplay();
        OnPropertyChanged(nameof(Ifs.ImageResolution));
    }

    public void UpdateStatusText(string statusText) => StatusTextChanged?.Invoke(this, statusText);

}
