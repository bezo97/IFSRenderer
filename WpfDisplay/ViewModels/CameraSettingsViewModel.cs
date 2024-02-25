using System;
using System.Numerics;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using IFSEngine.Model;

using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

public partial class CameraSettingsViewModel : ObservableObject
{
    private readonly Workspace _workspace;
    public Camera Camera => _workspace.Ifs.Camera;

    public CameraSettingsViewModel(Workspace workspace)
    {
        _workspace = workspace;
        _workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
    }

    public static Array ProjectionTypes => Enum.GetValues(typeof(ProjectionType));

    public double XPos
    {
        get => _workspace.Ifs.Camera.Position.X;
        set => _workspace.Ifs.Camera.Position = new Vector3((float)value, _workspace.Ifs.Camera.Position.Y, _workspace.Ifs.Camera.Position.Z);
    }
    private ValueSliderSettings _xPosSlider;
    public ValueSliderSettings XPosSlider => _xPosSlider ??= new()
    {
        Label = "Camera-X",
        IsLabelShown = false,
        ToolTip = "Camera position on X axis.",
        DefaultValue = IFS.Default.Camera.Position.X,
        Increment = 0.01,
        AnimationPath = "Camera.Position.X",
        ValueWillChange = _workspace.TakeSnapshot,
        ValueChanged = (v) => _workspace.Renderer.InvalidateHistogramBuffer(),
    };

    public double YPos
    {
        get => _workspace.Ifs.Camera.Position.Y;
        set => _workspace.Ifs.Camera.Position = new Vector3(_workspace.Ifs.Camera.Position.X, (float)value, _workspace.Ifs.Camera.Position.Z);
    }
    private ValueSliderSettings _yPosSlider;
    public ValueSliderSettings YPosSlider => _yPosSlider ??= new()
    {
        Label = "Camera-Y",
        IsLabelShown = false,
        ToolTip = "Camera position on Y axis.",
        DefaultValue = IFS.Default.Camera.Position.Y,
        Increment = 0.01,
        AnimationPath = "Camera.Position.Y",
        ValueWillChange = _workspace.TakeSnapshot,
        ValueChanged = (v) => _workspace.Renderer.InvalidateHistogramBuffer(),
    };

    public double ZPos
    {
        get => _workspace.Ifs.Camera.Position.Z;
        set => _workspace.Ifs.Camera.Position = new Vector3(_workspace.Ifs.Camera.Position.X, _workspace.Ifs.Camera.Position.Y, (float)value);
    }
    private ValueSliderSettings _zPosSlider;
    public ValueSliderSettings ZPosSlider => _zPosSlider ??= new()
    {
        Label = "Camera-Z",
        IsLabelShown = false,
        ToolTip = "Camera position on Z axis.",
        DefaultValue = IFS.Default.Camera.Position.Z,
        Increment = 0.01,
        AnimationPath = "Camera.Position.Z",
        ValueWillChange = _workspace.TakeSnapshot,
        ValueChanged = (v) => _workspace.Renderer.InvalidateHistogramBuffer(),
    };

    public double XOrientation
    {
        get => _workspace.Ifs.Camera.Orientation.X;
        set => _workspace.Ifs.Camera.Orientation = new Quaternion((float)value, _workspace.Ifs.Camera.Orientation.Y, _workspace.Ifs.Camera.Orientation.Z, _workspace.Ifs.Camera.Orientation.W);
    }
    private ValueSliderSettings _xOrientationSlider;
    public ValueSliderSettings XOrientationSlider => _xOrientationSlider ??= new()
    {
        Label = "Orientation-X",
        IsLabelShown = false,
        ToolTip = "Camera rotation around X axis.",
        DefaultValue = IFS.Default.Camera.Orientation.X,
        Increment = 0.001,
        AnimationPath = "Camera.Orientation.X",
        ValueWillChange = _workspace.TakeSnapshot,
        ValueChanged = (v) => _workspace.Renderer.InvalidateHistogramBuffer(),
    };

    public double YOrientation
    {
        get => _workspace.Ifs.Camera.Orientation.Y;
        set => _workspace.Ifs.Camera.Orientation = new Quaternion(_workspace.Ifs.Camera.Orientation.X, (float)value, _workspace.Ifs.Camera.Orientation.Z, _workspace.Ifs.Camera.Orientation.W);
    }
    private ValueSliderSettings _yOrientationSlider;
    public ValueSliderSettings YOrientationSlider => _yOrientationSlider ??= new()
    {
        Label = "Orientation-Y",
        IsLabelShown = false,
        ToolTip = "Camera rotation around Y axis.",
        DefaultValue = IFS.Default.Camera.Orientation.Y,
        Increment = 0.001,
        AnimationPath = "Camera.Orientation.Y",
        ValueWillChange = _workspace.TakeSnapshot,
        ValueChanged = (v) => _workspace.Renderer.InvalidateHistogramBuffer(),
    };

    public double ZOrientation
    {
        get => _workspace.Ifs.Camera.Orientation.Z;
        set => _workspace.Ifs.Camera.Orientation = new Quaternion(_workspace.Ifs.Camera.Orientation.X, _workspace.Ifs.Camera.Orientation.Y, (float)value, _workspace.Ifs.Camera.Orientation.W);
    }
    private ValueSliderSettings _zOrientationSlider;
    public ValueSliderSettings ZOrientationSlider => _zOrientationSlider ??= new()
    {
        Label = "Orientation-Z",
        IsLabelShown = false,
        ToolTip = "Camera rotation around Z axis.",
        DefaultValue = IFS.Default.Camera.Orientation.Z,
        Increment = 0.001,
        AnimationPath = "Camera.Orientation.Z",
        ValueWillChange = _workspace.TakeSnapshot,
        ValueChanged = (v) => _workspace.Renderer.InvalidateHistogramBuffer(),
    };

    public double WOrientation
    {
        get => _workspace.Ifs.Camera.Orientation.W;
        set => _workspace.Ifs.Camera.Orientation = new Quaternion(_workspace.Ifs.Camera.Orientation.X, _workspace.Ifs.Camera.Orientation.Y, _workspace.Ifs.Camera.Orientation.Z, (float)value);
    }
    private ValueSliderSettings _wOrientationSlider;
    public ValueSliderSettings WOrientationSlider => _wOrientationSlider ??= new()
    {
        Label = "Orientation-W",
        IsLabelShown = false,
        ToolTip = "Camera rotation around W axis.",
        DefaultValue = IFS.Default.Camera.Orientation.W,
        Increment = 0.001,
        AnimationPath = "Camera.Orientation.W",
        ValueWillChange = _workspace.TakeSnapshot,
        ValueChanged = (v) => _workspace.Renderer.InvalidateHistogramBuffer(),
    };

    private ValueSliderSettings _fieldOfViewSlider;
    public ValueSliderSettings FieldOfViewSlider => _fieldOfViewSlider ??= new()
    {
        Label = "🔬 Field of View",
        DefaultValue = IFS.Default.Camera.FieldOfView,
        MinValue = 1,
        MaxValue = 179,
        Increment = 0.1,
        AnimationPath = "Camera.FieldOfView",
        ValueWillChange = _workspace.TakeSnapshot,
        ValueChanged = (c) => _workspace.Renderer.InvalidateHistogramBuffer(),
    };

    private ValueSliderSettings _apertureSlider;
    public ValueSliderSettings ApertureSlider => _apertureSlider ??= new()
    {
        Label = "✨ Aperture",
        DefaultValue = IFS.Default.Camera.Aperture,
        MinValue = 0,
        Increment = 0.0001,
        AnimationPath = "Camera.Aperture",
        ValueWillChange = _workspace.TakeSnapshot,
        ValueChanged = (c) => _workspace.Renderer.InvalidateHistogramBuffer(),
    };

    private ValueSliderSettings _focusDistanceSlider;
    public ValueSliderSettings FocusDistanceSlider => _focusDistanceSlider ??= new()
    {
        Label = "📏 Focus Distance",
        DefaultValue = IFS.Default.Camera.FocusDistance,
        Increment = 0.01,
        AnimationPath = "Camera.FocusDistance",
        ValueWillChange = () => _workspace.TakeSnapshot(),
        ValueChanged = (c) => _workspace.Renderer.InvalidateHistogramBuffer(),
        ValueDraggingStarted = () => _workspace.Renderer.MarkAreaInFocus = true,
        ValueDraggingCompleted = (c) =>
        {
            _workspace.Renderer.MarkAreaInFocus = false;
            _workspace.Renderer.InvalidateHistogramBuffer();
        }
    };

    private ValueSliderSettings _depthOfFieldSlider;
    public ValueSliderSettings DepthOfFieldSlider => _depthOfFieldSlider ??= new()
    {
        Label = "🔾 Depth of Field",
        ToolTip = "a.k.a. Range of focus",
        DefaultValue = IFS.Default.Camera.DepthOfField,
        MinValue = 0,
        Increment = 0.01,
        AnimationPath = "Camera.DepthOfField",
        ValueWillChange = () => _workspace.TakeSnapshot(),
        ValueChanged = (c) => _workspace.Renderer.InvalidateHistogramBuffer(),
        ValueDraggingStarted = () => _workspace.Renderer.MarkAreaInFocus = true,
        ValueDraggingCompleted = (c) =>
        {
            _workspace.Renderer.MarkAreaInFocus = false;
            _workspace.Renderer.InvalidateHistogramBuffer();
        }
    };

    public ProjectionType ProjectionType
    {
        get => _workspace.Ifs.Camera.Projection;
        set
        {
            _workspace.TakeSnapshot();
            _workspace.Ifs.Camera.Projection = value;

            //some projection types only work properly with specific camera settings
            if (value is ProjectionType.Equirectangular)
            {
                _workspace.Ifs.Camera.FieldOfView = 90;
                _workspace.SetFinalResolution(_workspace.Ifs.ImageResolution.Width, _workspace.Ifs.ImageResolution.Width / 2);//2:1 aspect ratio
            }
            else if (value is ProjectionType.Fisheye)
            {
                _workspace.Ifs.Camera.FieldOfView = 90;
                _workspace.SetFinalResolution(_workspace.Ifs.ImageResolution.Width, _workspace.Ifs.ImageResolution.Width);//1:1 aspect ratio
            }

            _workspace.Renderer.InvalidateHistogramBuffer();
            OnPropertyChanged(nameof(ProjectionType));
            OnPropertyChanged(nameof(Camera.FieldOfView));
        }
    }

    [RelayCommand]
    private void ResetCamera()
    {
        _workspace.TakeSnapshot();
        _workspace.Ifs.Camera = new Camera();
        _workspace.Renderer.InvalidateHistogramBuffer();
        OnPropertyChanged(string.Empty);
    }

    [RelayCommand]
    private void AnimateCamera()
    {
        _workspace.TakeSnapshot();
        var vm = ((MainViewModel)System.Windows.Application.Current.MainWindow.DataContext).AnimationViewModel;//ugh
        vm.AddOrUpdateChannel(XPosSlider.Label, XPosSlider.AnimationPath, XPos);
        vm.AddOrUpdateChannel(YPosSlider.Label, YPosSlider.AnimationPath, YPos);
        vm.AddOrUpdateChannel(ZPosSlider.Label, ZPosSlider.AnimationPath, ZPos);
        vm.AddOrUpdateChannel(XOrientationSlider.Label, XOrientationSlider.AnimationPath, XOrientation);
        vm.AddOrUpdateChannel(YOrientationSlider.Label, YOrientationSlider.AnimationPath, YOrientation);
        vm.AddOrUpdateChannel(ZOrientationSlider.Label, ZOrientationSlider.AnimationPath, ZOrientation);
        vm.AddOrUpdateChannel(WOrientationSlider.Label, WOrientationSlider.AnimationPath, WOrientation);
    }


    public void RaiseCameraParamsChanged() => OnPropertyChanged(string.Empty);

}
