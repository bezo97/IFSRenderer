﻿#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using IFSEngine.Generation;
using IFSEngine.Model;
using IFSEngine.Rendering;
using IFSEngine.Serialization;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
    private List<Transform> _loadedTransforms = new();

    public event EventHandler<string>? StatusTextChanged;
    public event EventHandler? LoadedParamsChanged;
    public string TransformsDirectoryPath { get; } = Path.Combine(App.AppDataPath, "Transforms");
    public IReadOnlyCollection<Transform> LoadedTransforms => _loadedTransforms;
    public Author CurrentUser { get; set; } = Author.Unknown;
    public bool InvertAxisY;
    public double Sensitivity;
    public bool UseWhiteForBlankParams;
    public bool IsRawFrameExportEnabled { get; private set; }
    public bool IsExportVideoFileEnabled { get; private set; }
    public string FfmpegPath { get; private set; }
    public string FfmpegArgs { get; private set; }
    public string? EditedFilePath { get; private set; }
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
        await LoadTransformLibrary();
        await Renderer.Initialize(_loadedTransforms);

        Ifs = new IFS();
        Renderer.LoadParams(Ifs);
    }

    public async Task ReloadTransforms()
    {
        await LoadTransformLibrary();
        Ifs.ReloadTransforms(LoadedTransforms);
        await Renderer.LoadTransforms(LoadedTransforms);
        Renderer.StartRenderLoop();
        OnPropertyChanged(nameof(LoadedTransforms));
    }

    private async Task LoadTransformLibrary()
    {
        var loadTasks = Directory
            .GetFiles(TransformsDirectoryPath, "*.ifstf", SearchOption.AllDirectories)
            .Select(file => Transform.FromFile(file));
        _loadedTransforms = (await Task.WhenAll(loadTasks)).ToList();
        OnPropertyChanged(nameof(LoadedTransforms));
    }

    public void LoadParams(IFS ifs)
    {
        TakeSnapshot();
        _renderer?.LoadParams(ifs);
        foreach (var i in Ifs.Iterators)//keep node positions for same iterators
            ifs.Iterators.FirstOrDefault(i2 => i2.Id == i.Id)?.SetPosition(i.GetPosition());
        Ifs = ifs;
        EditedFilePath = null;
        HasUnsavedChanges = false;
        if (!Renderer.IsRendering)
            Renderer.StartRenderLoop();
        LoadedParamsChanged?.Invoke(this, EventArgs.Empty);
        OnPropertyChanged(nameof(Ifs));
    }

    public async Task LoadParamsFileAsync(string path)
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
            if(unknownTransformNames.All(transformName => LoadedTransforms.Any(t => t.Name == transformName)))
            {
                ifs = await IfsNodesSerializer.LoadJsonFileAsync(path, LoadedTransforms, true);
                System.Windows.MessageBox.Show($"The loaded file was created using different versions of the following transforms:\r\n{string.Join(", ", unknownTransformNames)}.", "Warning");
            }
            else
                throw;
        }
        LoadParams(ifs);
        EditedFilePath = path;
    }

    public void RaiseAnimationFrameChanged() => OnPropertyChanged("AnimationFrame");

    public void LoadBlankParams()
    {
        var blankParams = new IFS();
        if (!UseWhiteForBlankParams)
            blankParams.Palette = Generator.GenerateRandomIqPalette();
        LoadParams(blankParams);
    }

    public void LoadRandomParams()
    {
        Generator g = new Generator(LoadedTransforms);
        IFS r = g.GenerateOne(new GeneratorOptions());
        r.ImageResolution = new System.Drawing.Size(1920, 1080);
        LoadParams(r);
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
        EditedFilePath = path;
        HasUnsavedChanges = false;
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
        LoadParams(ifs);
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

    public void ClearHistory()
    {
        _tracker.Clear();
    }

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
        CurrentUser = new Author
        {
            Name = Settings.Default.AuthorName,
            Link = Settings.Default.AuthorLink
        };
    }

    public void UpdateStatusText(string statusText)
    {
        StatusTextChanged?.Invoke(this, statusText);
    }

}
