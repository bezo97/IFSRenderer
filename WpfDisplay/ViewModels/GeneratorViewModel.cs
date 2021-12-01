﻿using IFSEngine.Generation;
using IFSEngine.Model;
using IFSEngine.Utility;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class GeneratorViewModel
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
    public double MutationChance { get => _options.MutationChance; set => SetProperty(ref _options.MutationChance, value); }
    public double MutationStrength { get => _options.MutationStrength; set => SetProperty(ref _options.MutationStrength, value); }

    //n-wide grid gallery of images
    public IEnumerable<IEnumerable<KeyValuePair<IFS, ImageSource>>> PinnedIFSThumbnails =>
        _workspace.PinnedIFS.Select(s => new KeyValuePair<IFS, ImageSource>(s, _workspace.Thumbnails.TryGetValue(s, out var thumb) ? thumb : null)).Chunk(1);
    public IEnumerable<IEnumerable<KeyValuePair<IFS, ImageSource>>> GeneratedIFSThumbnails =>
        _workspace.GeneratedIFS.Select(s => new KeyValuePair<IFS, ImageSource>(s, _workspace.Thumbnails.TryGetValue(s, out var thumb) ? thumb : null)).Chunk(7);

    /// <summary>
    /// Call <see cref="Initialize"/> before using
    /// </summary>
    /// <param name="mainvm"></param>
    public GeneratorViewModel(MainViewModel mainvm)
    {
        _mainvm = mainvm;
        _workspace = new GeneratorWorkspace(mainvm.workspace.LoadedTransforms);
        _workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);//tmp hack
    }

    public async Task Initialize() => await _workspace.Initialize();

    [ICommand]
    private void SendToMain(IFS generated_params)
    {
        IFS param = generated_params.DeepClone();
        param.ImageResolution = new System.Drawing.Size(1920, 1080);
        _mainvm.workspace.LoadParams(param);
    }

    [ICommand]
    private void GenerateRandomBatch()
    {
        _workspace.GenerateNewRandomBatch(_options);
        //TODO: do not start if already processing
        _workspace.processQueue();
        OnPropertyChanged(nameof(GeneratedIFSThumbnails));
    }

    [ICommand]
    private void Pin(IFS param)
    {
        if (param == null)//pin ifs from main if commandparam not provided
            param = _mainvm.workspace.Ifs.DeepClone();
        _workspace.PinIFS(param);
        _workspace.processQueue();
        SendToMainCommand.Execute(param);
        //TODO: do not start if already processing
        OnPropertyChanged(nameof(PinnedIFSThumbnails));
    }

    [ICommand]
    private void Unpin(IFS param)
    {
        _workspace.UnpinIFS(param);
        OnPropertyChanged(nameof(PinnedIFSThumbnails));
    }
}
