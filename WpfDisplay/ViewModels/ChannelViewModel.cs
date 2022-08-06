#nullable enable
using IFSEngine.Animation;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using WpfDisplay.Models;
using WpfDisplay.Helper;
using IFSEngine.Animation.ChannelDrivers;
using Cavern.Remapping;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class ChannelViewModel
{
    public string Name { get; }

    private readonly Workspace _workspace;
    private readonly AnimationViewModel _vm;
    public readonly Channel channel;
    [ObservableProperty] private ObservableCollection<KeyframeViewModel> _keyframes = new();
    [ObservableProperty] private bool _isEditing = false; 
    private bool _hasDetails = false;
    public bool HasDetails
    { 
        get => _hasDetails;
        set
        {
            if (!value)//Remove audio channel
            {
                channel.AudioChannelDriver = null;
            }
            else
            {
                channel.AudioChannelDriver ??= new AudioChannelDriver();
                channel.AudioChannelDriver.AudioChannelId = (int)SelectedAudioChannelOption;
                channel.AudioChannelDriver.SetSamplerFunction(Sampler);
                EffectSlider.Value = EffectSlider.Value;//ugh
                MinFreqSlider.Value = MinFreqSlider.Value;//ugh
                MaxFreqSlider.Value = MaxFreqSlider.Value;//ugh
            }
            SetProperty(ref _hasDetails, value);
        }
    }
    [ObservableProperty] private List<ReferenceChannel> _audioChannelOptions = new();
    [ObservableProperty] private ReferenceChannel _selectedAudioChannelOption = default!;

    private float Sampler(AudioChannelDriver d, double t)
    {
        if (_vm.LoadedAudioClip is null)
            return 0.0f;
        return CavernHelper.CavernSampler(_vm.LoadedAudioClip, _vm.AudioClipCache!, d.AudioChannelId, d.MinFrequency, d.MaxFrequency, t);
    }

    private ValueSliderViewModel? _effectSlider;
    public ValueSliderViewModel EffectSlider => _effectSlider ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Effect strength",
        ToolTip = "Effect strength",
        DefaultValue = 1,
        GetV = () => channel.AudioChannelDriver?.EffectMultiplier ?? 1,
        SetV = (value) =>
        {
            if (channel.AudioChannelDriver is not null)
                channel.AudioChannelDriver.EffectMultiplier = value;
            _workspace.Renderer.InvalidateParamsBuffer();
        },
        Increment = 0.01,
        ValueWillChange = _workspace.TakeSnapshot
    };

    private ValueSliderViewModel? _minFreqSlider;
    public ValueSliderViewModel MinFreqSlider => _minFreqSlider ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Min. Frequency",
        ToolTip = "Minimum Frequency",
        DefaultValue = 0,
        GetV = () => channel.AudioChannelDriver?.MinFrequency ?? 0,
        SetV = (value) =>
        {
            if(channel.AudioChannelDriver is not null)
                channel.AudioChannelDriver.MinFrequency = (int)value;
            _workspace.Renderer.InvalidateParamsBuffer();
        },
        Increment = 1,
        MinValue = 0,
        ValueWillChange = _workspace.TakeSnapshot
    };

    private ValueSliderViewModel? _maxFreqSlider;
    public ValueSliderViewModel MaxFreqSlider => _maxFreqSlider ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Max. Frequency",
        ToolTip = "Maximum Frequency",
        DefaultValue = 20000,
        GetV = () => channel.AudioChannelDriver?.MaxFrequency ?? 0,
        SetV = (value) =>
        {
            if (channel.AudioChannelDriver is not null)
                channel.AudioChannelDriver.MaxFrequency = (int)value;
            _workspace.Renderer.InvalidateParamsBuffer();
        },
        Increment = 1,
        MaxValue = 20000,
        ValueWillChange = _workspace.TakeSnapshot
    };

    public ChannelViewModel(Workspace workspace, AnimationViewModel vm, string name, Channel c, List<KeyframeViewModel> selectedKeyframes)
    {
        _workspace = workspace;
        _vm = vm;
        Name = name;
        channel = c;

        AudioChannelOptions = vm.LoadedAudioChannels.ToList();
        SelectedAudioChannelOption = (ReferenceChannel)(c.AudioChannelDriver?.AudioChannelId ?? 0);
        HasDetails = c.AudioChannelDriver is not null;

        UpdateKeyframes(selectedKeyframes);
    }

    public void RemoveKeyframe(KeyframeViewModel kfv)
    {
        Keyframes.Remove(kfv);
        channel.Keyframes.Remove(kfv._k);
    }

    public void UpdateKeyframes(List<KeyframeViewModel> selectedKeyframes)
    {
        var sk = selectedKeyframes.Select(kvm => kvm._k).ToList();
        Keyframes = new ObservableCollection<KeyframeViewModel>(channel.Keyframes
            .Select(k => new KeyframeViewModel(this, k, sk.Contains(k))));
    }

    [ICommand]
    public void EditChannel()
    {
        IsEditing = !IsEditing;
    }

}
