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

    public bool MutateIterators { get => _options.MutateIterators; set => SetProperty(ref _options.MutateIterators, value); }
    public bool MutateConnections { get => _options.MutateConnections; set => SetProperty(ref _options.MutateConnections, value); }
    public bool MutateConnectionWeights { get => _options.MutateConnectionWeights; set => SetProperty(ref _options.MutateConnectionWeights, value); }
    public bool MutateParameters { get => _options.MutateParameters; set => SetProperty(ref _options.MutateParameters, value); }
    public bool MutatePalette { get => _options.MutatePalette; set => SetProperty(ref _options.MutatePalette, value); }
    public bool MutateColoring { get => _options.MutateColoring; set => SetProperty(ref _options.MutateColoring, value); }
    private ValueSliderViewModel _mutationChance;
    public ValueSliderViewModel MutationChance => _mutationChance ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Mutation chance",
        DefaultValue = 0.5,
        GetV = () => _options.MutationChance,
        SetV = (value) => {
            _options.MutationChance = value;
        },
        MinValue = 0,
        MaxValue = 1,
        Increment = 0.01,
    };

    private ValueSliderViewModel _mutationStrength;
    public ValueSliderViewModel MutationStrength => _mutationStrength ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Mutation strength",
        DefaultValue = 1.0,
        GetV = () => _options.MutationStrength,
        SetV = (value) => {
            _options.MutationStrength = value;
        },
        MinValue = 0,
        Increment = 0.1,
    };

    private ValueSliderViewModel _batchSize;
    public ValueSliderViewModel BatchSize => _batchSize ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Batch size",
        DefaultValue = 30,
        GetV = () => _options.BatchSize,
        SetV = (value) => {
            _options.BatchSize = (int)value;
        },
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
        _mainvm.workspace.LoadParams(param);
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
