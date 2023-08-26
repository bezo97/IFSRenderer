#nullable enable
using IFSEngine.Animation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WpfDisplay.ViewModels;

public partial class KeyframeViewModel : ObservableObject
{
    private readonly AnimationViewModel _avm;
    public readonly ChannelViewModel _cvm;
    public readonly Keyframe _k;

    public double KeyframeTime => _k.t;
    public float TimelinePositon => (float)_k.t * 50.0f/* * ViewScale */ + _offset;//add offset
    private float _offset;
    public double PositionMoveOffset
    {
        get => _offset;
        set
        {
            _offset = (float)value;
            OnPropertyChanged(nameof(TimelinePositon));
        }
    }
    [ObservableProperty] private bool _isSelected;

    public EasingDirection SelectedEasingDirection
    {
        get => _k.EasingDirection;
        set
        {
            _k.EasingDirection = value;
            OnPropertyChanged(nameof(SelectedEasingDirection));
            _avm.Workspace.Renderer.InvalidateParamsBuffer();
        }
    }

    public InterpolationMode SelectedInterpolationMode
    {
        get => _k.InterpolationMode;
        set
        {
            _k.InterpolationMode = value;
            OnPropertyChanged(nameof(SelectedInterpolationMode));
            _avm.Workspace.Renderer.InvalidateParamsBuffer();
        }
    }

    public KeyframeViewModel(AnimationViewModel avm, ChannelViewModel cvm, Keyframe k, bool isSelected)
    {
        _avm = avm;
        _cvm = cvm;
        _k = k;
        _isSelected = isSelected;
    }


    private ValueSliderViewModel? _easingPowerSlider;
    public ValueSliderViewModel EasingPowerSlider => _easingPowerSlider ??= new ValueSliderViewModel(_avm.Workspace)
    {
        ToolTip = $"Easing power. Default value is 1.0",
        DefaultValue = 1.0,
        GetV = () => _k.EasingPower,
        SetV = (value) =>
        {
            _k.EasingPower = value;
            _avm.Workspace.Renderer.InvalidateParamsBuffer();
        },
        Increment = 0.1,
    };

    [RelayCommand]
    private void RemoveKeyframe() => _avm.RemoveKeyframeCommand.Execute(this);

}
