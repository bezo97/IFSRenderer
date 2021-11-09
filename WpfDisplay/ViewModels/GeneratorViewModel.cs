using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using IFSEngine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using WpfDisplay.Models;
using IFSEngine.Generation;
using IFSEngine.Utility;

namespace WpfDisplay.ViewModels
{
    public class GeneratorViewModel : ObservableObject
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

        //n-wide grid gallery of images
        public IEnumerable<IEnumerable<KeyValuePair<IFS, ImageSource>>> PinnedIFSThumbnails =>
            workspace.PinnedIFS.Select(s => new KeyValuePair<IFS, ImageSource>(s, workspace.Thumbnails.TryGetValue(s, out var thumb) ? thumb : null)).Chunk(1);
        public IEnumerable<IEnumerable<KeyValuePair<IFS, ImageSource>>> GeneratedIFSThumbnails =>
            workspace.GeneratedIFS.Select(s => new KeyValuePair<IFS, ImageSource>(s, workspace.Thumbnails.TryGetValue(s, out var thumb) ? thumb : null)).Chunk(7);

        public GeneratorViewModel(MainViewModel mainvm)
        {
            this.mainvm = mainvm;
            workspace = new GeneratorWorkspace(mainvm.workspace.LoadedTransforms);
            workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);//tmp hack
        }

        private RelayCommand<IFS> _sendToMainCommand;
        public RelayCommand<IFS> SendToMainCommand =>
            _sendToMainCommand ??= new RelayCommand<IFS>(
            (IFS generated_params) =>
            {
                IFS param = generated_params.DeepClone();
                param.ImageResolution = new System.Drawing.Size(1920, 1080);
                mainvm.workspace.LoadParams(param);
            });


        private RelayCommand _generateRandomBatchCommand;
        public RelayCommand GenerateRandomBatchCommand =>
            _generateRandomBatchCommand ??= new RelayCommand(() => 
            {
                workspace.GenerateNewRandomBatch(options).Wait();
                //TODO: do not start if already processing
                workspace.processQueue();
                OnPropertyChanged(nameof(GeneratedIFSThumbnails));
            });

        private RelayCommand<IFS> _pinGeneratedCommand;
        public RelayCommand<IFS> PinGeneratedCommand => 
            _pinGeneratedCommand ??= new RelayCommand<IFS>((IFS param) =>
            {
                workspace.PinIFS(param);
                //TODO: do not start if already processing
                workspace.processQueue();
                OnPropertyChanged(nameof(PinnedIFSThumbnails));
            });

        public double MutationChance { get => options.MutationChance; set => SetProperty(ref options.MutationChance, value); }
        public double MutationStrength { get => options.MutationStrength; set => SetProperty(ref options.MutationStrength, value); }

    }
}
