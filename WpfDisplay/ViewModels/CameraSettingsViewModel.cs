using GalaSoft.MvvmLight;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels
{
    public class CameraSettingsViewModel : ViewModelBase
    {
        private readonly Workspace workspace;

        public CameraSettingsViewModel(Workspace workspace)
        {
            this.workspace = workspace;
            workspace.PropertyChanged += (s, e) => RaisePropertyChanged(string.Empty);
        }

        public float FieldOfView {
            get => workspace.IFS.Camera.FieldOfView;
            set
            {
                workspace.IFS.Camera.FieldOfView = value;
                RaisePropertyChanged(() => FieldOfView);
                workspace.Renderer.InvalidateAccumulation();
            }
        }

        public double DepthOfField
        {
            get => workspace.IFS.Camera.DepthOfField;
            set
            {
                workspace.IFS.Camera.DepthOfField = value;
                RaisePropertyChanged(() => DepthOfField);
                workspace.Renderer.InvalidateAccumulation();
            }
        }

        public double FocusDistance
        {
            get => workspace.IFS.Camera.FocusDistance;
            set
            {
                workspace.IFS.Camera.FocusDistance = value;
                RaisePropertyChanged(() => FocusDistance);
                workspace.Renderer.InvalidateAccumulation();
            }
        }

        public double FocusArea
        {
            get => workspace.IFS.Camera.FocusArea;
            set
            {
                workspace.IFS.Camera.FocusArea = value;
                RaisePropertyChanged(() => FocusArea);
                workspace.Renderer.InvalidateAccumulation();
            }
        }

    }
}
