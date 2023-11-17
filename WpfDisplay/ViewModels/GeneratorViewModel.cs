using IFSEngine.Generation;
using IFSEngine.Model;
using IFSEngine.Utility;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

public partial class GeneratorViewModel : ObservableObject
{
    private readonly MainViewModel _mainvm;
    private readonly GeneratorWorkspace _workspace;
    private readonly GeneratorOptions _options = new();
    public GeneratorOptions Options => _options;

    private ValueSliderSettings _mutationChanceSlider;
    public ValueSliderSettings MutationChanceSlider => _mutationChanceSlider ??= new()
    {
        Label = "Mutation chance",
        DefaultValue = 0.5,
        MinValue = 0,
        MaxValue = 1,
        Increment = 0.01,
    };

    private ValueSliderSettings _mutationStrengthSlider;
    public ValueSliderSettings MutationStrengthSlider => _mutationStrengthSlider ??= new()
    {
        Label = "Mutation strength",
        DefaultValue = 1.0,
        MinValue = 0,
        Increment = 0.1,
    };

    private ValueSliderSettings _batchSizeSlider;
    public ValueSliderSettings BatchSizeSlider => _batchSizeSlider ??= new()
    {
        Label = "Batch size",
        ToolTip = "The number of fractals to generate.",
        DefaultValue = 30,
        MinValue = 1,
        MaxValue = 50,
        Increment = 5,
    };

    public IEnumerable<KeyValuePair<IFS, ImageSource>> PinnedIFSThumbnails =>
        _workspace.PinnedIFS.Select(s => 
        new KeyValuePair<IFS, ImageSource>(s, _workspace.Thumbnails.TryGetValue(s, out var thumb) ? thumb : null)).Reverse();

    public IEnumerable<KeyValuePair<IFS, ImageSource>> GeneratedIFSThumbnails =>
        _workspace.GeneratedIFS.Select(s => 
        new KeyValuePair<IFS, ImageSource>(s, _workspace.Thumbnails.TryGetValue(s, out var thumb) ? thumb : null));

    /// <summary>
    /// Call <see cref="Initialize"/> before using
    /// </summary>
    /// <param name="mainvm"></param>
    public GeneratorViewModel(MainViewModel mainvm)
    {
        _mainvm = mainvm;
        _workspace = new GeneratorWorkspace(mainvm.workspace.LoadedTransforms);
        _workspace.PropertyChanged += (s, e) => OnPropertyChanged(nameof(GeneratedIFSThumbnails));//tmp hack
    }

    public async Task Initialize()
    {
        await _workspace.Initialize();
        await GenerateRandomBatch();
    }

    [RelayCommand]
    private void SendToMain(IFS generated_params)
    {
        IFS param = generated_params.DeepClone();
        param.ImageResolution = new System.Drawing.Size(1920, 1080);
        _mainvm.workspace.LoadParams(param, null);
    }

    [RelayCommand]
    private async Task GenerateRandomBatch()
    {
        _workspace.GenerateNewRandomBatch(_options);
        await Task.Run(_workspace.ProcessQueue);
        OnPropertyChanged(nameof(GeneratedIFSThumbnails));
    }

    [RelayCommand]
    private async Task Pin(IFS param)
    {
        param ??= _mainvm.workspace.Ifs.DeepClone();//pin ifs from main if commandparam not provided
        _workspace.PinIFS(param);
        await Task.Run(_workspace.ProcessQueue);
        SendToMainCommand.Execute(param);
        OnPropertyChanged(nameof(PinnedIFSThumbnails));
    }

    [RelayCommand]
    private void Unpin(IFS param)
    {
        _workspace.UnpinIFS(param);
        OnPropertyChanged(nameof(PinnedIFSThumbnails));
    }
}
