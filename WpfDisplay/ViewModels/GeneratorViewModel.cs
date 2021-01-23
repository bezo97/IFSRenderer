using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using IFSEngine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels
{
    public class GeneratorViewModel : ViewModelBase
    {
        private readonly MainViewModel mainvm;
        private readonly GeneratorWorkspace workspace = new GeneratorWorkspace();

        //Linq magic to split an array into arrays of 3
        //This makes binding the thumbnails to the 3-wide gallery of images easy.
        //based on https://stackoverflow.com/questions/11207526/how-to-split-an-array-into-chunks-of-specific-size
        public IEnumerable<IEnumerable<KeyValuePair<IFS, ImageSource>>> PinnedIFSList => workspace.Thumbnails.ToArray()
            .Select((s, i) => new { Value = s, Index = i })
            .GroupBy(x => x.Index / 3)
            .Select(grp => grp.Select(x => x.Value));

        public GeneratorViewModel(MainViewModel mainvm)
        {
            this.mainvm = mainvm;
            workspace.PropertyChanged += (s,e) => 
                RaisePropertyChanged(() => PinnedIFSList);//

            //debug
            for (int i = 0; i < 20; i++)
            {
                var r = IFS.GenerateRandom(workspace.LoadedTransforms);
                r.ImageResolution = new System.Drawing.Size(1080, 1080);
                workspace.PinIFS(r);
            }

        }

        public void ProcessQueue()
        {
            //TODO: separate thread, make context current
            //Task.Run(async () => { 
            //await
            workspace.processQueue().Wait();
            //});
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


    }
}
