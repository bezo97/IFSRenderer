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
    private readonly MainViewModel mainvm;
    private readonly GeneratorWorkspace workspace;
    private readonly GeneratorOptions options = new();

    public bool MutateIterators { get => options.MutateIterators; set => SetProperty(ref options.MutateIterators, value); }
    public bool MutateConnections { get => options.MutateConnections; set => SetProperty(ref options.MutateConnections, value); }
    public bool MutateConnectionWeights { get => options.MutateConnectionWeights; set => SetProperty(ref options.MutateConnectionWeights, value); }
    public bool MutateParameters { get => options.MutateParameters; set => SetProperty(ref options.MutateParameters, value); }
    public bool MutatePalette { get => options.MutatePalette; set => SetProperty(ref options.MutatePalette, value); }
    public bool MutateColoring { get => options.MutateColoring; set => SetProperty(ref options.MutateColoring, value); }
    public double MutationChance { get => options.MutationChance; set => SetProperty(ref options.MutationChance, value); }
    public double MutationStrength { get => options.MutationStrength; set => SetProperty(ref options.MutationStrength, value); }

    //n-wide grid gallery of images
    public IEnumerable<IEnumerable<KeyValuePair<IFS, ImageSource>>> PinnedIFSThumbnails =>
        workspace.PinnedIFS.Select(s => new KeyValuePair<IFS, ImageSource>(s, workspace.Thumbnails.TryGetValue(s, out var thumb) ? thumb : null)).Chunk(1);
    public IEnumerable<IEnumerable<KeyValuePair<IFS, ImageSource>>> GeneratedIFSThumbnails =>
        workspace.GeneratedIFS.Select(s => new KeyValuePair<IFS, ImageSource>(s, workspace.Thumbnails.TryGetValue(s, out var thumb) ? thumb : null)).Chunk(7);

    /// <summary>
    /// Call <see cref="Initialize"/> before using
    /// </summary>
    /// <param name="mainvm"></param>
    public GeneratorViewModel(MainViewModel mainvm)
    {
        this.mainvm = mainvm;
        workspace = new GeneratorWorkspace(mainvm.workspace.LoadedTransforms);
        workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);//tmp hack
    }

    public async Task Initialize() => await workspace.Initialize();

    [ICommand]
    private void SendToMain(IFS generated_params)
    {
        IFS param = generated_params.DeepClone();
        param.ImageResolution = new System.Drawing.Size(1920, 1080);
        mainvm.workspace.LoadParams(param);
    }

    [ICommand]
    private void GenerateRandomBatch()
    {
        workspace.GenerateNewRandomBatch(options);
        //TODO: do not start if already processing
        workspace.processQueue();
        OnPropertyChanged(nameof(GeneratedIFSThumbnails));
    }

    [ICommand]
    private void Pin(IFS param)
    {
        if (param == null)//pin ifs from main if commandparam not provided
            param = mainvm.workspace.Ifs.DeepClone();
        workspace.PinIFS(param);
        workspace.processQueue();
        SendToMainCommand.Execute(param);
        //TODO: do not start if already processing
        OnPropertyChanged(nameof(PinnedIFSThumbnails));
    }

    [ICommand]
    private void Unpin(IFS param)
    {
        workspace.UnpinIFS(param);
        OnPropertyChanged(nameof(PinnedIFSThumbnails));
    }
}
