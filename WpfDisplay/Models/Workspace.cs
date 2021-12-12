using IFSEngine.Generation;
using IFSEngine.Model;
using IFSEngine.Rendering;
using IFSEngine.Serialization;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WpfDisplay.Helper;
using WpfDisplay.Properties;

namespace WpfDisplay.Models;

/// <summary>
/// The main workspace model that contains a <see cref="RendererGL"/> 
/// and an <see cref="IFSEngine.Model.IFS"/> that it is rendering.
/// </summary>
[ObservableObject]
public partial class Workspace
{
    private readonly IFSHistoryTracker _tracker = new();
    private List<Transform> _loadedTransforms = new();

    public event EventHandler<string> StatusTextChanged;
    public string TransformsDirectoryPath { get; } = Path.Combine(App.AppDataPath, "Transforms");
    public IReadOnlyCollection<Transform> LoadedTransforms => _loadedTransforms;
    public Author CurrentUser { get; set; } = Author.Unknown;
    public bool InvertAxisX, InvertAxisY, InvertAxisZ;
    public double Sensitivity;

    private RendererGL _renderer;
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


    [ObservableProperty] private IFS _ifs;

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
        await LoadUserSettings();
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
        Ifs = ifs;
        if (!Renderer.IsRendering)
            Renderer.StartRenderLoop();
    }

    public async Task LoadParamsFileAsync(string path)
    {
        IFS ifs;
        try
        {
            ifs = await IfsSerializer.LoadJsonFileAsync(path, LoadedTransforms, false);
        }
        catch (System.Runtime.Serialization.SerializationException)
        {
            if (System.Windows.MessageBox.Show("Loading params failed. Try again and ignore transform versions?", "Loading failed", System.Windows.MessageBoxButton.OKCancel)
                == System.Windows.MessageBoxResult.OK)
            {
                ifs = await IfsSerializer.LoadJsonFileAsync(path, LoadedTransforms, true);
            }
            else
                throw;
        }
        LoadParams(ifs);
    }

    public void LoadBlankParams()
    {
        LoadParams(new IFS());
    }

    public void LoadRandomParams()
    {
        Generator g = new Generator(LoadedTransforms);
        IFS r = g.GenerateOne(new GeneratorOptions());
        r.ImageResolution = new System.Drawing.Size(1920, 1080);
        LoadParams(r);
    }

    public async Task SaveParamsFileAsync(string path)
    {
        Ifs.AddAuthor(CurrentUser);
        await IfsSerializer.SaveJsonFileAsync(Ifs, path);
    }

    public void UndoHistory()
    {
        //LoadParams without taking snapshot
        Ifs = _tracker.Undo(Ifs, LoadedTransforms);
        _renderer?.LoadParams(Ifs);
    }

    public void RedoHistory()
    {
        //LoadParams without taking snapshot
        Ifs = _tracker.Redo(Ifs, LoadedTransforms);
        _renderer?.LoadParams(Ifs);
    }

    public void ClearHistory()
    {
        _tracker.Clear();
    }

    public void TakeSnapshot()
    {
        _tracker.TakeSnapshot(Ifs);
    }

    public async Task LoadUserSettings()
    {
        if (!ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).HasFile)
        {//migrate user settings from previous version
            Settings.Default.Upgrade();
            Settings.Default.Save();
        }
        Renderer.EnablePerceptualUpdates = Settings.Default.PerceptuallyUniformUpdates;

        await Renderer.SetWorkgroupCount(Settings.Default.WorkgroupCount);

        Renderer.TargetFramerate = Settings.Default.TargetFramerate;
        InvertAxisX = Settings.Default.InvertAxisX;
        InvertAxisY = Settings.Default.InvertAxisY;
        InvertAxisZ = Settings.Default.InvertAxisZ;
        Sensitivity = Settings.Default.Sensitivity;
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
