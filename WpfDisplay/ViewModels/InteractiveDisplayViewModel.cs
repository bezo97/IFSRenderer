using Microsoft.Toolkit.Mvvm.ComponentModel;
using System.Numerics;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels
{
    public class InteractiveDisplayViewModel : ObservableObject
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
                OnPropertyChanged(nameof(FocusDistance));
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
