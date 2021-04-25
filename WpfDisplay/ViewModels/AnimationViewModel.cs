using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFSEngine.Animation;
using WpfDisplay.Models;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Toolkit.Mvvm.Input;

namespace WpfDisplay.ViewModels
{
    public class AnimationViewModel  : ObservableObject
    {
        private Workspace workspace;
        public AnimationViewModel(Workspace workspace)
        {
            this.workspace = workspace;
            workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
        }
        public AnimationManager AnimationManager => workspace.AnimationManager;

        private ICommand playCommand;
        public ICommand PlayCommand => playCommand ??= new RelayCommand(Play);

        public void Play()
        {
            var t = 0f;
            var startTime = DateTime.Now;
            var endTime = startTime.AddSeconds(AnimationManager.AnimationLength);
            var length = (endTime - startTime).Ticks;
            Task.Run(() =>
            {
                while (t < 1f)
                {
                    //Dispatcher.CurrentDispatcher.Invoke(() =>
                    {
                        t = (float)(DateTime.Now - startTime).Ticks / (float)length;
                        AnimationManager.EvaluateAt(t);
                    }//);
                }
            });
        }
    }
}
