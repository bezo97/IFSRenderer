using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Windows.Input;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class CameraSettingsViewModel
{
    private readonly Workspace _workspace;

    public CameraSettingsViewModel(Workspace workspace)
    {
        _workspace = workspace;
        _workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
    }

    public float FieldOfView
    {
        get => _workspace.Ifs.Camera.FieldOfView;
        set
        {
            _workspace.Ifs.Camera.FieldOfView = value;
            OnPropertyChanged(nameof(FieldOfView));
            _workspace.Renderer.InvalidateHistogramBuffer();
        }
    }

    public double Aperture
    {
        get => _workspace.Ifs.Camera.Aperture;
        set
        {
            _workspace.Ifs.Camera.Aperture = value;
            OnPropertyChanged(nameof(Aperture));
            _workspace.Renderer.InvalidateHistogramBuffer();
        }
    }

    public double FocusDistance
    {
        get => _workspace.Ifs.Camera.FocusDistance;
        set
        {
            _workspace.Ifs.Camera.FocusDistance = value;
            OnPropertyChanged(nameof(FocusDistance));
            _workspace.Renderer.InvalidateHistogramBuffer();
        }
    }

    public double DepthOfField
    {
        get => _workspace.Ifs.Camera.DepthOfField;
        set
        {
            _workspace.Ifs.Camera.DepthOfField = value;
            OnPropertyChanged(nameof(DepthOfField));
            _workspace.Renderer.InvalidateHistogramBuffer();
        }
    }

    [ICommand]
    private void TakeSnapshot() => _workspace.TakeSnapshot();

    [ICommand]
    private void ResetCamera()
    {
        _workspace.TakeSnapshot();
        _workspace.Ifs.Camera = new IFSEngine.Model.Camera();
        FieldOfView = 60;
        Aperture = 0.0;
        FocusDistance = 10.0;
        DepthOfField = 0.25;
        _workspace.Renderer.InvalidateHistogramBuffer();
    }

    public void RaisePropertyChanged() => OnPropertyChanged(string.Empty);
}
