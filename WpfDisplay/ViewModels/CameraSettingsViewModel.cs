using IFSEngine.Model;
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

    private ValueSliderViewModel _fieldOfView;
    public ValueSliderViewModel FieldOfView => _fieldOfView ??= new ValueSliderViewModel(_workspace)
    {
        Label = "🔬 Field of View",
        DefaultValue = IFS.Default.Camera.FieldOfView,
        GetV = () => _workspace.Ifs.Camera.FieldOfView,
        SetV = (value) => {
            _workspace.Ifs.Camera.FieldOfView = value;
            _workspace.Renderer.InvalidateHistogramBuffer();
        },
        MinValue = 1,
        MaxValue = 179,
        Increment = 0.1,
        AnimationPath = "Camera.FieldOfView",
        ValueWillChange = _workspace.TakeSnapshot,
    };

    private ValueSliderViewModel _aperture;
    public ValueSliderViewModel Aperture => _aperture ??= new ValueSliderViewModel(_workspace)
    {
        Label = "✨ Aperture",
        DefaultValue = IFS.Default.Camera.Aperture,
        GetV = () => _workspace.Ifs.Camera.Aperture,
        SetV = (value) => {
            _workspace.Ifs.Camera.Aperture = value;
            _workspace.Renderer.InvalidateHistogramBuffer();
        },
        MinValue = 0,
        Increment = 0.0001,
        AnimationPath = "Camera.Aperture",
        ValueWillChange = _workspace.TakeSnapshot,
    };

    private ValueSliderViewModel _focusDistance;
    public ValueSliderViewModel FocusDistance => _focusDistance ??= new ValueSliderViewModel(_workspace)
    {
        Label = "📏 Focus Distance",
        DefaultValue = IFS.Default.Camera.FocusDistance,
        GetV = () => _workspace.Ifs.Camera.FocusDistance,
        SetV = (value) => {
            _workspace.Ifs.Camera.FocusDistance = value;
            _workspace.Renderer.InvalidateHistogramBuffer();
        },
        AnimationPath = "Camera.FocusDistance",
        ValueWillChange = _workspace.TakeSnapshot,
    };

    private ValueSliderViewModel _depthOfField;
    public ValueSliderViewModel DepthOfField => _depthOfField ??= new ValueSliderViewModel(_workspace)
    {
        Label = "🔾 Depth of Field",
        ToolTip = "a.k.a. Range of focus",
        DefaultValue = IFS.Default.Camera.DepthOfField,
        GetV = () => _workspace.Ifs.Camera.DepthOfField,
        SetV = (value) => {
            _workspace.Ifs.Camera.DepthOfField = value;
            _workspace.Renderer.InvalidateHistogramBuffer();
        },
        MinValue = 0,
        AnimationPath = "Camera.DepthOfField",
        ValueWillChange = _workspace.TakeSnapshot,
    };

    [ICommand]
    private void ResetCamera()
    {
        _workspace.TakeSnapshot();
        _workspace.Ifs.Camera = new Camera();
        FieldOfView.Value = FieldOfView.DefaultValue;
        Aperture.Value = Aperture.DefaultValue;
        FocusDistance.Value = FocusDistance.DefaultValue;
        DepthOfField.Value = DepthOfField.DefaultValue;
        _workspace.Renderer.InvalidateHistogramBuffer();
    }

    public void RaisePropertyChanged() => OnPropertyChanged(string.Empty);///?
}
