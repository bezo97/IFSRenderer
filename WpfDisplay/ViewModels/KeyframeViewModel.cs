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

    public double EasingPower
    {
        get => _k.EasingPower;
        set => _k.EasingPower = value;
    }

    public double KeyframeTime => _k.t;
    public float TimelinePositon => (float)(_k.t * _avm.ViewScale + _avm.KeyframeRepositionOffset);
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
        avm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(avm.ViewScale))
                NotifyPositionChanged();
        };
    }


    private ValueSliderSettings? _easingPowerSlider;
    public ValueSliderSettings EasingPowerSlider => _easingPowerSlider ??= new()
    {
        ToolTip = $"Easing power. Default value is 1.0",
        DefaultValue = 1.0,
        Increment = 0.1,
        ValueWillChange = _avm.Workspace.TakeSnapshot,
        ValueChanged = (v) => _avm.Workspace.Renderer.InvalidateParamsBuffer()
    };

    [RelayCommand]
    private void RemoveKeyframe() => _avm.RemoveKeyframeCommand.Execute(this);

    public void NotifyPositionChanged() => OnPropertyChanged(nameof(TimelinePositon));

}
