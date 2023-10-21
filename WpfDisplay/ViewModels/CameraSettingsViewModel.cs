using IFSEngine.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Numerics;
using System.Windows.Input;
using WpfDisplay.Models;
using System;

namespace WpfDisplay.ViewModels;

public partial class CameraSettingsViewModel : ObservableObject
{
    private readonly Workspace _workspace;

    public CameraSettingsViewModel(Workspace workspace)
    {
        _workspace = workspace;
        _workspace.LoadedParamsChanged += (s, e) => RaiseCameraParamsChanged();
    }

    public static Array ProjectionTypes => Enum.GetValues(typeof(ProjectionType));

    private ValueSliderViewModel _xPosViewModel;
    public ValueSliderViewModel XPosViewModel => _xPosViewModel ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Camera-X",
        IsLabelShown = false,
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
        Label = "Camera-Y",
        IsLabelShown = false,
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
        Label = "Camera-Z",
        IsLabelShown = false,
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

    private ValueSliderViewModel _xOrientationViewModel;
    public ValueSliderViewModel XOrientationViewModel => _xOrientationViewModel ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Orientation-X",
        IsLabelShown = false,
        ToolTip = "Camera rotation around X axis.",
        DefaultValue = IFS.Default.Camera.Orientation.X,
        GetV = () => _workspace.Ifs.Camera.Orientation.X,
        SetV = (value) => {
            var o = _workspace.Ifs.Camera.Orientation;
            _workspace.Ifs.Camera.Orientation = new Quaternion((float)value, o.Y, o.Z, o.W);
            _workspace.Renderer.InvalidateHistogramBuffer();
            OnPropertyChanged(nameof(XOrientationViewModel));
        },
        Increment = 0.001,
        AnimationPath = "Camera.Orientation.X",
        ValueWillChange = _workspace.TakeSnapshot,
    };

    private ValueSliderViewModel _yOrientationViewModel;
    public ValueSliderViewModel YOrientationViewModel => _yOrientationViewModel ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Orientation-Y",
        IsLabelShown = false,
        ToolTip = "Camera rotation around Y axis.",
        DefaultValue = IFS.Default.Camera.Orientation.Y,
        GetV = () => _workspace.Ifs.Camera.Orientation.Y,
        SetV = (value) => {
            var o = _workspace.Ifs.Camera.Orientation;
            _workspace.Ifs.Camera.Orientation = new Quaternion(o.X, (float)value, o.Z, o.W);
            _workspace.Renderer.InvalidateHistogramBuffer();
            OnPropertyChanged(nameof(YOrientationViewModel));
        },
        Increment = 0.001,
        AnimationPath = "Camera.Orientation.Y",
        ValueWillChange = _workspace.TakeSnapshot,
    };

    private ValueSliderViewModel _zOrientationViewModel;
    public ValueSliderViewModel ZOrientationViewModel => _zOrientationViewModel ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Orientation-Z",
        IsLabelShown = false,
        ToolTip = "Camera rotation around Z axis.",
        DefaultValue = IFS.Default.Camera.Orientation.Z,
        GetV = () => _workspace.Ifs.Camera.Orientation.Z,
        SetV = (value) => {
            var o = _workspace.Ifs.Camera.Orientation;
            _workspace.Ifs.Camera.Orientation = new Quaternion(o.X, o.Y, (float)value, o.W);
            _workspace.Renderer.InvalidateHistogramBuffer();
            OnPropertyChanged(nameof(ZOrientationViewModel));
        },
        Increment = 0.001,
        AnimationPath = "Camera.Orientation.Z",
        ValueWillChange = _workspace.TakeSnapshot,
    };

    private ValueSliderViewModel _wOrientationViewModel;
    public ValueSliderViewModel WOrientationViewModel => _wOrientationViewModel ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Orientation-W",
        IsLabelShown = false,
        ToolTip = "Camera rotation around W axis.",
        DefaultValue = IFS.Default.Camera.Orientation.W,
        GetV = () => _workspace.Ifs.Camera.Orientation.W,
        SetV = (value) => {
            var o = _workspace.Ifs.Camera.Orientation;
            _workspace.Ifs.Camera.Orientation = new Quaternion(o.X, o.Y, o.Z, (float)value);
            _workspace.Renderer.InvalidateHistogramBuffer();
            OnPropertyChanged(nameof(WOrientationViewModel));
        },
        Increment = 0.001,
        AnimationPath = "Camera.Orientation.W",
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

    public ProjectionType ProjectionType
    {
        get => _workspace.Ifs.Camera.Projection;
        set
        {
            _workspace.TakeSnapshot();
            _workspace.Ifs.Camera.Projection = value; 
            _workspace.Renderer.InvalidateHistogramBuffer();
            OnPropertyChanged(nameof(ProjectionType));
        }
    }

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
        RaiseCameraParamsChanged();
    }

    [RelayCommand]
    private void AnimateCamera()
    {
        _workspace.TakeSnapshot();
        var vm = ((MainViewModel)System.Windows.Application.Current.MainWindow.DataContext).AnimationViewModel;//ugh
        vm.AddOrUpdateChannel(XPosViewModel.Label, XPosViewModel.AnimationPath, _workspace.Ifs.Camera.Position.X);
        XPosViewModel.IsAnimated = true;
        vm.AddOrUpdateChannel(YPosViewModel.Label, YPosViewModel.AnimationPath, _workspace.Ifs.Camera.Position.Y);
        YPosViewModel.IsAnimated = true;
        vm.AddOrUpdateChannel(ZPosViewModel.Label, ZPosViewModel.AnimationPath, _workspace.Ifs.Camera.Position.Z);
        ZPosViewModel.IsAnimated = true;
        vm.AddOrUpdateChannel(XOrientationViewModel.Label, XOrientationViewModel.AnimationPath, _workspace.Ifs.Camera.Orientation.X);
        XOrientationViewModel.IsAnimated = true;
        vm.AddOrUpdateChannel(YOrientationViewModel.Label, YOrientationViewModel.AnimationPath, _workspace.Ifs.Camera.Orientation.Y);
        YOrientationViewModel.IsAnimated = true;
        vm.AddOrUpdateChannel(ZOrientationViewModel.Label, ZOrientationViewModel.AnimationPath, _workspace.Ifs.Camera.Orientation.Z);
        ZOrientationViewModel.IsAnimated = true;
        vm.AddOrUpdateChannel(WOrientationViewModel.Label, WOrientationViewModel.AnimationPath, _workspace.Ifs.Camera.Orientation.W);
        WOrientationViewModel.IsAnimated = true;
    }

    public void RaiseCameraParamsChanged()
    {
        XPosViewModel.RaiseValueChanged();
        YPosViewModel.RaiseValueChanged();
        ZPosViewModel.RaiseValueChanged();
        XOrientationViewModel.RaiseValueChanged();
        YOrientationViewModel.RaiseValueChanged();
        ZOrientationViewModel.RaiseValueChanged();
        WOrientationViewModel.RaiseValueChanged();
        FocusDistance.RaiseValueChanged();

    }

}
