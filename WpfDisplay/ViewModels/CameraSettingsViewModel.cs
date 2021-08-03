using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Windows.Input;
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

        public double Aperture
        {
            get => workspace.IFS.Camera.Aperture;
            set
            {
                workspace.IFS.Camera.Aperture = value;
                OnPropertyChanged(nameof(Aperture));
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

        private RelayCommand _takeSnapshotCommand;
        public RelayCommand TakeSnapshotCommand =>
            _takeSnapshotCommand ??= new RelayCommand(workspace.TakeSnapshot);

        private RelayCommand resetCameraCommand;
        public ICommand ResetCameraCommand => resetCameraCommand ??= new RelayCommand(ResetCamera);

        private void ResetCamera()
        {
            workspace.TakeSnapshot();
            workspace.IFS.Camera = new IFSEngine.Model.Camera();
            FieldOfView = 60;
            Aperture = 0.0;
            FocusDistance = 10.0;
            DepthOfField = 0.25;
            workspace.Renderer.InvalidateHistogramBuffer();
        }
    }
}
