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
                workspace.Renderer.InvalidateAccumulation();
            }
        }

        public double DepthOfField
        {
            get => workspace.IFS.DepthOfField;
            set
            {
                workspace.IFS.DepthOfField = value;
                OnPropertyChanged(nameof(DepthOfField));
                workspace.Renderer.InvalidateAccumulation();
            }
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

        public double FocusArea
        {
            get => workspace.IFS.FocusArea;
            set
            {
                workspace.IFS.FocusArea = value;
                OnPropertyChanged(nameof(FocusArea));
                workspace.Renderer.InvalidateAccumulation();
            }
        }

        public double FogEffect
        {
            get => workspace.IFS.FogEffect;
            set
            {
                workspace.IFS.FogEffect = value;
                OnPropertyChanged(nameof(FogEffect));
                workspace.Renderer.InvalidateAccumulation();
            }
        }

    }
}
