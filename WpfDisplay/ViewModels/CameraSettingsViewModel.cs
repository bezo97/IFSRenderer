using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Windows.Input;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class CameraSettingsViewModel
{
    private readonly Workspace workspace;

    public CameraSettingsViewModel(Workspace workspace)
    {
        this.workspace = workspace;
        workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
    }

    public float FieldOfView
    {
        get => workspace.Ifs.Camera.FieldOfView;
        set
        {
            workspace.Ifs.Camera.FieldOfView = value;
            OnPropertyChanged(nameof(FieldOfView));
            workspace.Renderer.InvalidateHistogramBuffer();
        }
    }

    public double Aperture
    {
        get => workspace.Ifs.Camera.Aperture;
        set
        {
            workspace.Ifs.Camera.Aperture = value;
            OnPropertyChanged(nameof(Aperture));
            workspace.Renderer.InvalidateHistogramBuffer();
        }
    }

    public double FocusDistance
    {
        get => workspace.Ifs.Camera.FocusDistance;
        set
        {
            workspace.Ifs.Camera.FocusDistance = value;
            OnPropertyChanged(nameof(FocusDistance));
            workspace.Renderer.InvalidateHistogramBuffer();
        }
    }

    public double DepthOfField
    {
        get => workspace.Ifs.Camera.DepthOfField;
        set
        {
            workspace.Ifs.Camera.DepthOfField = value;
            OnPropertyChanged(nameof(DepthOfField));
            workspace.Renderer.InvalidateHistogramBuffer();
        }
    }

    [ICommand]
    private void TakeSnapshot() => workspace.TakeSnapshot();

    [ICommand]
    private void ResetCamera()
    {
        workspace.TakeSnapshot();
        workspace.Ifs.Camera = new IFSEngine.Model.Camera();
        FieldOfView = 60;
        Aperture = 0.0;
        FocusDistance = 10.0;
        DepthOfField = 0.25;
        workspace.Renderer.InvalidateHistogramBuffer();
    }

    public void RaisePropertyChanged() => OnPropertyChanged(string.Empty);
}
