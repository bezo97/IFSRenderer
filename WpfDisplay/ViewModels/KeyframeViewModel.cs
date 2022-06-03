#nullable enable
using IFSEngine.Animation;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class KeyframeViewModel
{
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

    public KeyframeViewModel(ChannelViewModel cvm, Keyframe k, bool isSelected)
    {
        _cvm = cvm;
        _k = k;
        _isSelected = isSelected;
    }
}
