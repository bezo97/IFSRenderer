using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFSEngine.Animation;
using WpfDisplay.Models;

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
    }
}
