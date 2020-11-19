using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels
{
    public class InteractiveDisplayViewModel : ViewModelBase
    {
        private readonly Workspace workspace;

        public InteractiveDisplayViewModel(Workspace workspace)
        {
            this.workspace = workspace;
        }

        public double FocusDistance
        {
            get => workspace.IFS.FocusDistance;
            set
            {
                workspace.IFS.FocusDistance = value;
                RaisePropertyChanged(() => FocusDistance);
                workspace.Renderer.InvalidateAccumulation();
            }
        }

        //TODO: Could use RelayCommands
        public void InvalidateAccumulation() => workspace.Renderer.InvalidateAccumulation();

        public void RotateCommand(Vector3 rotation)
        {
            workspace.IFS.Camera.RotateWithSensitivity(rotation);
            workspace.Renderer.InvalidateAccumulation();
        }
        public void TranslateCommand(Vector3 translation)
        {
            workspace.IFS.Camera.TranslateWithSensitivity(translation);
            workspace.Renderer.InvalidateAccumulation();
        }

}
}
