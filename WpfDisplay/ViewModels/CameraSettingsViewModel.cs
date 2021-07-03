using Microsoft.Toolkit.Mvvm.ComponentModel;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels
{
    public class CameraSettingsViewModel : ObservableObject
    {
        private readonly Workspace workspace;

        public CameraSettingsViewModel(Workspace workspace)
        {
            this.workspace = workspace;
            workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
        }

        public float FieldOfView {
            get => workspace.IFS.Camera.FieldOfView;
            set
            {
                workspace.IFS.Camera.FieldOfView = value;
                OnPropertyChanged(nameof(FieldOfView));
                workspace.Renderer.InvalidateHistogramBuffer();
            }
        }

        public double DepthOfField
        {
            get => workspace.IFS.Camera.DepthOfField;
            set
            {
                workspace.IFS.Camera.DepthOfField = value;
                OnPropertyChanged(nameof(DepthOfField));
                workspace.Renderer.InvalidateHistogramBuffer();
            }
        }

        public double FocusDistance
        {
            get => workspace.IFS.Camera.FocusDistance;
            set
            {
                workspace.IFS.Camera.FocusDistance = value;
                OnPropertyChanged(nameof(FocusDistance));
                workspace.Renderer.InvalidateHistogramBuffer();
            }
        }

        public double FocusArea
        {
            get => workspace.IFS.Camera.FocusArea;
            set
            {
                workspace.IFS.Camera.FocusArea = value;
                OnPropertyChanged(nameof(FocusArea));
                workspace.Renderer.InvalidateHistogramBuffer();
            }
        }

    }
}
