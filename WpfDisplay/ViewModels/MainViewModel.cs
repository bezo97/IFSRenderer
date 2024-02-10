﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using IFSEngine.Model;
using IFSEngine.Serialization;
using IFSEngine.Utility;

using WpfDisplay.Helper;
using WpfDisplay.Models;
using WpfDisplay.Services;

namespace WpfDisplay.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IAsyncDisposable
{
    internal readonly Workspace workspace;
    public IFS Ifs => workspace.Ifs;

    public ToneMappingViewModel ToneMappingViewModel { get; }
    public CameraSettingsViewModel CameraSettingsViewModel { get; }
    public PerformanceViewModel PerformanceViewModel { get; }
    public QualitySettingsViewModel QualitySettingsViewModel { get; }
    public IFSViewModel IFSViewModel { get; }
    public AnimationViewModel AnimationViewModel { get; }

    private bool _transparentBackground;
    public bool TransparentBackground
    {
        get => _transparentBackground;
        set
        {
            workspace.TakeSnapshot();
            if (value)
                IFSViewModel.BackgroundColor = Colors.Black;
            SetProperty(ref _transparentBackground, value);
            OnPropertyChanged(nameof(IsColorPickerEnabled));
        }
    }

    [ObservableProperty] private string _statusBarText = string.Empty;
    [ObservableProperty] private bool _isHintsPanelVisible = true;
    [ObservableProperty] private bool _isGamepadConnected = false;

    public string IsRenderingIcon => workspace.Renderer.IsRendering ? "||" : "▶️";
    public string IterationLevel => BitOperations.Log2(1 + workspace.Renderer.TotalIterations / (ulong)(workspace.Renderer.HistogramWidth * workspace.Renderer.HistogramHeight)).ToString("00.");
    public double IterationProgressPercent
    {
        get
        {
            //this is to avoid interrupting the render thread with gui updates
            Task.Delay(50).ContinueWith(t => OnPropertyChanged(nameof(IterationProgressPercent)));
            return 100.0 * (workspace.Renderer.TotalIterations / (double)(workspace.Renderer.HistogramWidth * workspace.Renderer.HistogramHeight)) / (Math.Pow(2, workspace.Ifs.TargetIterationLevel) - 1);
        }
    }

    public bool IsColorPickerEnabled => !TransparentBackground;
    //Main display settings:
    public bool InvertAxisY => workspace.InvertAxisY;
    public float Sensitivity => (float)workspace.Sensitivity;


    public string WindowTitle => workspace.Ifs is null ? "IFSRenderer" : $"{workspace.Ifs.Title}{(workspace.HasUnsavedChanges ? '*' : string.Empty)} - IFSRenderer";
    public string IFSTitle
    {
        get => workspace.Ifs.Title;
        set
        {
            if (workspace.Ifs.Title != value)
            {
                workspace.TakeSnapshot();
                workspace.Ifs.Title = value;
                OnPropertyChanged(nameof(IFSTitle));
                OnPropertyChanged(nameof(WindowTitle));
            }
        }
    }
    public IEnumerable<Author> AuthorList => workspace.Ifs.Authors;

    public static IEnumerable<string> RecentFilePaths => Workspace.RecentFilePaths.Reverse();
    public static IReadOnlyDictionary<string, string> Templates => Workspace.TemplateFilePaths.ToDictionary(path => path, path => Path.GetFileNameWithoutExtension(path));

    public MainViewModel(Workspace workspace, WelcomeWorkflow workflow)
    {
        this.workspace = workspace;
        workspace.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(this.workspace.HasUnsavedChanges): OnPropertyChanged(nameof(WindowTitle)); break;
            }
        };
        workspace.StatusTextChanged += (s, e) => StatusBarText = e;
        PerformanceViewModel = new PerformanceViewModel(workspace);
        QualitySettingsViewModel = new QualitySettingsViewModel(workspace);
        IFSViewModel = new IFSViewModel(workspace);
        AnimationViewModel = new AnimationViewModel(workspace);
        CameraSettingsViewModel = new CameraSettingsViewModel(workspace);
        CameraSettingsViewModel.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName);
        ToneMappingViewModel = new ToneMappingViewModel(workspace);
        ToneMappingViewModel.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName);

        if (workflow == WelcomeWorkflow.ShowFileDialog)
            ShowLoadParamsDialogCommand?.Execute(this);

        workspace.Renderer.DisplayFramebufferUpdated += (s, e) => OnPropertyChanged(nameof(IterationLevel));

        workspace.UpdateStatusText($"Initialized");
    }

    [RelayCommand]
    private async Task StartStopRendering()
    {
        if (workspace.Renderer.IsRendering)
        {
            await workspace.Renderer.StopRenderLoop();
            workspace.UpdateStatusText($"Stopped rendering");
        }
        else
        {
            workspace.Renderer.StartRenderLoop();
            workspace.UpdateStatusText($"Started rendering");
        }
        OnPropertyChanged(nameof(IsRenderingIcon));
    }

    [RelayCommand]
    private void New()
    {
        workspace.LoadBlankParams();
        workspace.UpdateStatusText($"Blank parameters loaded");
        OnPropertyChanged(nameof(IsRenderingIcon));
    }

    [RelayCommand]
    private void LoadRandom()
    {
        workspace.LoadRandomParams();
        workspace.UpdateStatusText($"Randomly generated parameters loaded");
        OnPropertyChanged(nameof(IsRenderingIcon));
    }

    [RelayCommand]
    private async Task SaveParams()
    {
        if (workspace.EditedFilePath is not null)
        {
            await workspace.SaveParamsAsync();
            workspace.UpdateStatusText($"Parameters saved to {workspace.EditedFilePath}");
        }
        else
            await SaveParamsAs();
    }

    [RelayCommand]
    private async Task SaveParamsAs()
    {
        if (DialogHelper.ShowSaveParamsDialog(workspace.Ifs.Title, out string path))
        {
            if (IFSTitle == "Untitled")//Set the file name as title
                IFSTitle = Path.GetFileNameWithoutExtension(path);

            try
            {
                await workspace.SaveParamsAsAsync(path);
                workspace.UpdateStatusText($"Parameters saved to {path}");
            }
            catch (Exception)
            {
                workspace.UpdateStatusText($"ERROR - Failed to save params.");
            }
        }
    }

    [RelayCommand]
    private async Task ShowLoadParamsDialog()
    {
        if (DialogHelper.ShowOpenParamsDialog(out string path))
        {
            await LoadParamsFromFile(path, false);
        }
    }

    [RelayCommand]
    private void CopyClipboardParams()
    {
        workspace.CopyToClipboard();
        workspace.UpdateStatusText("Parameters copied to Clipboard");
    }

    [RelayCommand]
    private void PasteClipboardParams()
    {
        try
        {
            workspace.PasteFromClipboard();
            workspace.UpdateStatusText("Parameters pasted from Clipboard");
        }
        catch (SerializationException)
        {
            workspace.UpdateStatusText("ERROR - Failed to paste params from Clipboard");
        }
        OnPropertyChanged(nameof(IsRenderingIcon));
    }

    /// <summary>
    /// Handle file from a drag & drop operation.
    /// </summary>
    [RelayCommand]
    private async Task DropFile(IDataObject dataObject)
    {
        if (FileDropHandler.IsDropSupported(dataObject, out var fileType, out var filePath))
        {
            switch (fileType)
            {
                case DroppedFile.Params:
                    await LoadParamsFromFile(filePath, false);
                    break;
                case DroppedFile.Image:
                    var pngImge = BitmapFrame.Create(new Uri(filePath));
                    if (PngMetadataHelper.TryExtractParamsFromImage(pngImge, workspace.LoadedTransforms, out var ifs))
                        workspace.LoadParams(ifs, null);
                    break;
                case DroppedFile.Palette:
                    IFSViewModel.DropPaletteCommand.Execute(filePath);
                    break;
                case DroppedFile.Transform:
                    File.Copy(filePath, Path.Combine(App.TransformsDirectoryPath, Path.GetFileName(filePath)), true);
                    IFSViewModel.ReloadTransformsCommand.Execute(null);
                    break;
            }
        }
    }

    [RelayCommand]
    private async Task LoadTemplate(string path) => await LoadParamsFromFile(path, true);

    private async Task LoadParamsFromFile(string path, bool isTemplate)
    {
        try
        {
            await workspace.LoadParamsFileAsync(path, isTemplate);
            workspace.UpdateStatusText($"Parameters loaded from {path}");
        }
        catch (SerializationException ex)
            when (ex.InnerException is AggregateException exs)
        {//missing transforms
            var unknownTransforms = exs.InnerExceptions.Select(e => (UnknownTransformException)e);
            MessageBox.Show($"Failed to load params due to missing transforms:\r\n{string.Join("\r\n", unknownTransforms.Select(t => $"{t.TransformName} ({t.TransformVersion})"))}", "Error");
            workspace.UpdateStatusText($"ERROR - Missing transforms: {string.Join(", ", unknownTransforms.Select(t => t.TransformName))}");
        }
        catch (SerializationException ex)
        {
            string logFilePath = App.LogException(ex);
            workspace.UpdateStatusText($"ERROR - Failed to load params: {path}. See log: {logFilePath}");
        }
        OnPropertyChanged(nameof(IsRenderingIcon));
    }

    [RelayCommand]
    private async Task ExportToClipboard()
    {
        BitmapSource bs = await workspace.Renderer.GetExportBitmapSource(TransparentBackground);
        //remark: sadly, Windows Clipboard does not keep metadata, so there's no point in creating a png frame here
        Clipboard.SetImage(bs);
        //TODO: somehow alpha channel is not copied

        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect();
        workspace.UpdateStatusText($"Image exported to clipboard");
    }

    [RelayCommand]
    private async Task SaveImage()
    {
        workspace.UpdateStatusText($"Exporting...");
        var makeBitmapTask = workspace.Renderer.GetExportBitmapSource(TransparentBackground);

        if (DialogHelper.ShowExportImageDialog(workspace.Ifs.Title, out string path))
        {
            //create metadata
            workspace.Ifs.AddAuthor(workspace.CurrentUser);//make sure to include current user even when params haven't been saved yet.
            BitmapMetadata? metadata = null;
            if (workspace.SaveMetadata)
                metadata = PngMetadataHelper.CreateMetadataFromParams(workspace.Ifs, includeSerializedParams: workspace.IncludeParamsInMetadata);
            //create frame
            BitmapSource bs = await makeBitmapTask;
            var frame = BitmapFrame.Create(bs, null, metadata, null);
            //save png
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(frame);
            using (FileStream stream = new FileStream(path, FileMode.Create))
                encoder.Save(stream);

            //open the image for viewing. TODO: optional..
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            workspace.UpdateStatusText($"Image exported to {path}");
        }
        else
            workspace.UpdateStatusText(string.Empty);

        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect();
    }

    [RelayCommand]
    private async Task SaveExr()
    {
        workspace.UpdateStatusText($"Exporting...");
        Task<float[][][]> getDataTask = Task.Run(workspace.Renderer.ReadHistogramData);

        if (DialogHelper.ShowExportExrDialog(workspace.Ifs.Title, out string path))
        {
            var histogramData = await getDataTask;
            using var fstream = File.Create(path);
            OpenEXR.WriteStream(fstream, histogramData);
            workspace.UpdateStatusText($"Image exported to {path}");
        }
        else
            workspace.UpdateStatusText(string.Empty);

        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect();
    }

    /// <summary>
    /// Used by InteractiveDisplay
    /// </summary>
    [RelayCommand]
    private void TakeSnapshot()
    {
        IsHintsPanelVisible = false;
        workspace.TakeSnapshot();
    }

    [RelayCommand]
    private void InteractionFinished() => CameraSettingsViewModel.RaiseCameraParamsChanged();

    [RelayCommand]
    private static void ExitApplication() => Application.Current.MainWindow.Close();

    public async ValueTask DisposeAsync() => await workspace.Renderer.DisposeAsync();

    [RelayCommand]
    private static void VisitIssues()
    {
        //Open the Issues page in user's default browser
        string link = "https://github.com/bezo97/IFSRenderer/issues/";
        Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
    }

    [RelayCommand]
    private static void VisitForum()
    {
        //Open the Discussions page in user's default browser
        string link = "https://github.com/bezo97/IFSRenderer/discussions";
        Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
    }

    [RelayCommand]
    private static void ReportBug()
    {
        //Open the bug report template in user's default browser
        string link = "https://github.com/bezo97/IFSRenderer/issues/new?assignees=&labels=&template=bug_report.md";
        Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
    }

    [RelayCommand]
    private static void CheckUpdates()
    {
        //Open the Releases page in user's default browser
        string link = "https://github.com/bezo97/IFSRenderer/releases";
        Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
    }

    [RelayCommand]
    private static void VisitWiki()
    {
        //Open the Wiki page in user's default browser
        string link = "https://github.com/bezo97/IFSRenderer/wiki";
        Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
    }

}
