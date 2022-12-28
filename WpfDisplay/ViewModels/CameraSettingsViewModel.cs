using IFSEngine.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Numerics;
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
        _workspace.LoadedParamsChanged += (s, e) => OnPropertyChanged(string.Empty);
    }

    private ValueSliderViewModel _xPosViewModel;
    public ValueSliderViewModel XPosViewModel => _xPosViewModel ??= new ValueSliderViewModel(_workspace)
    {
        ToolTip = "Camera position on X axis.",
        DefaultValue = IFS.Default.Camera.Position.X,
        GetV = () => _workspace.Ifs.Camera.Position.X,
        SetV = (value) => {
            _workspace.Ifs.Camera.Position = new Vector3((float)value, _workspace.Ifs.Camera.Position.Y, _workspace.Ifs.Camera.Position.Z);
            _workspace.Renderer.InvalidateHistogramBuffer();
            OnPropertyChanged(nameof(XPosViewModel));
        },
        Increment = 0.01,
        AnimationPath = "Camera.Position.X",
        ValueWillChange = _workspace.TakeSnapshot,
    };

    private ValueSliderViewModel _yPosViewModel;
    public ValueSliderViewModel YPosViewModel => _yPosViewModel ??= new ValueSliderViewModel(_workspace)
    {
        ToolTip = "Camera position on Y axis.",
        DefaultValue = IFS.Default.Camera.Position.Y,
        GetV = () => _workspace.Ifs.Camera.Position.Y,
        SetV = (value) => {
            _workspace.Ifs.Camera.Position = new Vector3(_workspace.Ifs.Camera.Position.X, (float)value, _workspace.Ifs.Camera.Position.Z);
            _workspace.Renderer.InvalidateHistogramBuffer();
            OnPropertyChanged(nameof(YPosViewModel));
        },
        Increment = 0.01,
        AnimationPath = "Camera.Position.Y",
        ValueWillChange = _workspace.TakeSnapshot,
    };

    private ValueSliderViewModel _zPosViewModel;
    public ValueSliderViewModel ZPosViewModel => _zPosViewModel ??= new ValueSliderViewModel(_workspace)
    {
        ToolTip = "Camera position on Z axis.",
        DefaultValue = IFS.Default.Camera.Position.Z,
        GetV = () => _workspace.Ifs.Camera.Position.Z,
        SetV = (value) => {
            _workspace.Ifs.Camera.Position = new Vector3(_workspace.Ifs.Camera.Position.X, _workspace.Ifs.Camera.Position.Y, (float)value);
            _workspace.Renderer.InvalidateHistogramBuffer();
            OnPropertyChanged(nameof(ZPosViewModel));
        },
        Increment = 0.01,
        AnimationPath = "Camera.Position.Z",
        ValueWillChange = _workspace.TakeSnapshot,
    };

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

    [RelayCommand]
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

    [RelayCommand]
    private void AnimateCamera()
    {
        _workspace.TakeSnapshot();
        var main = (MainViewModel)System.Windows.Application.Current.MainWindow.DataContext;//ugh
        main.AnimationViewModel.AddOrUpdateChannel("Camera-X", "Camera.Position.X", _workspace.Ifs.Camera.Position.X);
        main.AnimationViewModel.AddOrUpdateChannel("Camera-Y", "Camera.Position.Y", _workspace.Ifs.Camera.Position.Y);
        main.AnimationViewModel.AddOrUpdateChannel("Camera-Z", "Camera.Position.Z", _workspace.Ifs.Camera.Position.Z);
        main.AnimationViewModel.AddOrUpdateChannel("Orientation-X", "Camera.Orientation.X", _workspace.Ifs.Camera.Orientation.X);
        main.AnimationViewModel.AddOrUpdateChannel("Orientation-Y", "Camera.Orientation.Y", _workspace.Ifs.Camera.Orientation.Y);
        main.AnimationViewModel.AddOrUpdateChannel("Orientation-Z", "Camera.Orientation.Z", _workspace.Ifs.Camera.Orientation.Z);
        main.AnimationViewModel.AddOrUpdateChannel("Orientation-W", "Camera.Orientation.W", _workspace.Ifs.Camera.Orientation.W);
    }

    public void RaisePropertyChanged() => OnPropertyChanged(string.Empty);///?
}
