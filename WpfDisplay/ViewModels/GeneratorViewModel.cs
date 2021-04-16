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

namespace WpfDisplay.ViewModels
{
    public class GeneratorViewModel : ObservableObject
    {
        private readonly MainViewModel mainvm;
        private readonly GeneratorWorkspace workspace = new GeneratorWorkspace();
        private readonly GeneratorOptions options = new GeneratorOptions();

        public bool MutateIterators { get => options.MutateIterators; set => SetProperty(ref options.MutateIterators, value); }
        public bool MutateConnections { get => options.MutateConnections; set => SetProperty(ref options.MutateConnections, value); }
        public bool MutateConnectionWeights { get => options.MutateConnectionWeights; set => SetProperty(ref options.MutateConnectionWeights, value); }
        public bool MutateVariables { get => options.MutateVariables; set => SetProperty(ref options.MutateVariables, value); }
        public bool MutatePalette { get => options.MutatePalette; set => SetProperty(ref options.MutatePalette, value); }
        public bool MutateColoring { get => options.MutateColoring; set => SetProperty(ref options.MutateColoring, value); }

        //Linq magic to split an array into arrays of 3
        //This makes binding the thumbnails to the 3-wide gallery of images easy.
        //based on https://stackoverflow.com/questions/11207526/how-to-split-an-array-into-chunks-of-specific-size
        public IEnumerable<IEnumerable<KeyValuePair<IFS, ImageSource>>> PinnedIFSThumbnails => 
            workspace.PinnedIFS.ToArray()
            .Select((s, i) => new { Value = new KeyValuePair<IFS, ImageSource>(s, workspace.Thumbnails.ContainsKey(s) ? workspace.Thumbnails[s] : null), Index = i })//get thumbnail and index
            .GroupBy(x => x.Index / 3)//3 in a row
            .Select(grp => grp.Select(x => x.Value));

        public IEnumerable<IEnumerable<KeyValuePair<IFS, ImageSource>>> GeneratedIFSThumbnails =>
            workspace.GeneratedIFS.ToArray()
            .Select((s, i) => new { Value = new KeyValuePair<IFS, ImageSource>(s, workspace.Thumbnails.ContainsKey(s) ? workspace.Thumbnails[s] : null), Index = i })//get thumbnail and index
            .GroupBy(x => x.Index / 10)//10 in a row
            .Select(grp => grp.Select(x => x.Value));

        public GeneratorViewModel(MainViewModel mainvm)
        {
            this.mainvm = mainvm;
            workspace.PropertyChanged += (s,e) => OnPropertyChanged(string.Empty);//tmp hack
        }

        private RelayCommand<IFS> _sendToMainCommand;
        public RelayCommand<IFS> SendToMainCommand
        {
            get => _sendToMainCommand ?? (
                _sendToMainCommand = new RelayCommand<IFS>((IFS param) =>
                {
                    //copy?
                    mainvm.LoadParamsToWorkspace(param);
                }));
        }

        private RelayCommand<IFS> _generateRandomBatchCommand;
        public RelayCommand<IFS> GenerateRandomBatchCommand
        {
            get => _generateRandomBatchCommand ?? (
                _generateRandomBatchCommand = new RelayCommand<IFS>((IFS param) =>
                {
                    workspace.GenerateNewRandomBatch(options).Wait();
                    //TODO: do not start if already processing
                    workspace.processQueue();
                    OnPropertyChanged(nameof(GeneratedIFSThumbnails));
                }));
        }

        private RelayCommand<IFS> _pinGeneratedCommand;
        public RelayCommand<IFS> PinGeneratedCommand
        {
            get => _pinGeneratedCommand ?? (
                _pinGeneratedCommand = new RelayCommand<IFS>((IFS param) =>
                {
                    workspace.PinIFS(param);
                    //TODO: do not start if already processing
                    workspace.processQueue();
                    OnPropertyChanged(nameof(PinnedIFSThumbnails));
                }));
        }

    }
}
