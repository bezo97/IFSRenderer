#nullable enable
using Cavern.Channels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFSEngine.Animation;
using IFSEngine.Animation.ChannelDrivers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using WpfDisplay.Helper;

namespace WpfDisplay.ViewModels;

public partial class ChannelViewModel : ObservableObject
{
    public string Name
    {
        get => channel.Name;
        set
        {
            channel.Name = value;
            OnPropertyChanged(nameof(Name));
        }
    }
    public string Path { get; }

    public AnimationViewModel AnimationVM => _vm;

    private readonly AnimationViewModel _vm;
    public readonly Channel channel;
    [ObservableProperty] private ObservableCollection<KeyframeViewModel> _keyframes = new();
    [ObservableProperty] private bool _hasAudioDriver = false;

    [ObservableProperty] private ReferenceChannel _selectedAudioChannelOption = default!;

    private float Sampler(AudioChannelDriver d, double t)
    {
        if (_vm.Audio is null)
            return 0.0f;
        return CavernHelper.CavernSampler(_vm.Audio, d.AudioChannelId, d.MinFrequency, d.MaxFrequency, t);
    }

    public bool IsDriverEnabled
    {
        get => EffectStrength != 0.0;
        set
        {
            _vm.Workspace.TakeSnapshot();
            EffectStrength = value ? 1.0f : 0.0f;
            _vm.Workspace.Renderer.InvalidateParamsBuffer();
            _vm.Workspace.RaiseAnimationFrameChanged();
        }
    }

    public double EffectStrength
    {
        get => channel.AudioChannelDriver?.EffectMultiplier ?? 1;
        set
        {
            if (channel.AudioChannelDriver is not null)
                channel.AudioChannelDriver.EffectMultiplier = value;
            OnPropertyChanged(nameof(EffectStrength));
        }
    }

    private ValueSliderSettings? _effectSlider;
    public ValueSliderSettings EffectSlider => _effectSlider ??= new()
    {
        Label = "Effect strength",
        ToolTip = "Effect strength",
        DefaultValue = 1,
        Increment = 0.01,
        ValueWillChange = _vm.Workspace.TakeSnapshot,
        ValueChanged = (v) => _vm.Workspace.Renderer.InvalidateParamsBuffer()
    };

    public double MinFreq
    {
        get => channel.AudioChannelDriver?.MinFrequency ?? 1;
        set
        {
            if (channel.AudioChannelDriver is not null)
                channel.AudioChannelDriver.MinFrequency = (int)value;
        }
    }

    private ValueSliderSettings? _minFreqSlider;
    public ValueSliderSettings MinFreqSlider => _minFreqSlider ??= new()
    {
        Label = "Min. Frequency",
        ToolTip = "Minimum Frequency",
        DefaultValue = 0,
        Increment = 1,
        MinValue = 0,
        ValueWillChange = _vm.Workspace.TakeSnapshot,
        ValueChanged = (v) => _vm.Workspace.Renderer.InvalidateParamsBuffer()
    };

    public double MaxFreq
    {
        get => channel.AudioChannelDriver?.MaxFrequency ?? 1;
        set
        {
            if (channel.AudioChannelDriver is not null)
                channel.AudioChannelDriver.MaxFrequency = (int)value;
        }
    }

    private ValueSliderSettings? _maxFreqSlider;
    public ValueSliderSettings MaxFreqSlider => _maxFreqSlider ??= new()
    {
        Label = "Max. Frequency",
        ToolTip = "Maximum Frequency",
        DefaultValue = 20000,
        Increment = 1,
        MaxValue = 20000,
        ValueWillChange = _vm.Workspace.TakeSnapshot,
        ValueChanged = (v) => _vm.Workspace.Renderer.InvalidateParamsBuffer()
    };

    public ChannelViewModel(AnimationViewModel vm, string path, Channel c)
    {
        _vm = vm;
        Path = path;
        channel = c;

        SelectedAudioChannelOption = (ReferenceChannel)(c.AudioChannelDriver?.AudioChannelId ?? 0);
        channel.AudioChannelDriver?.SetSamplerFunction(Sampler);
        HasAudioDriver = c.AudioChannelDriver != null;

        UpdateKeyframes();
    }

    public void RemoveKeyframe(KeyframeViewModel kvm)
    {
        Keyframes.Remove(kvm);
        channel.Keyframes.Remove(kvm._k);
    }

    public void UpdateKeyframes()
    {
        var removed = Keyframes.Where(k => !channel.Keyframes.Contains(k._k));
        foreach (var kvm in removed)
            Keyframes.Remove(kvm);
        var newKeyframes = channel.Keyframes.Where(k => !Keyframes.Any(kvm => kvm._k == k));
        foreach (var k in newKeyframes)
            Keyframes.Add(new KeyframeViewModel(_vm, this, k, false));
    }

    [RelayCommand]
    public void InsertKeyframe()
    {
        if (!_vm.KeyframeInsertPosition.HasValue) throw new InvalidOperationException();
        _vm.Workspace.TakeSnapshot();
        var keyframePosition = TimeOnly.FromTimeSpan(TimeSpan.FromSeconds(_vm.KeyframeInsertPosition.Value));
        var eval = channel.EvaluateAt(_vm.KeyframeInsertPosition.Value);
        AnimationVM.AddOrUpdateChannel(Name, Path, eval, keyframePosition);
        _vm.KeyframeInsertPosition = null;
    }

    //TODO: add ChannelDriverEnum Command Parameter
    [RelayCommand]
    public void AddChannelDriver()
    {
        _vm.Workspace.TakeSnapshot();
        channel.AudioChannelDriver ??= new AudioChannelDriver();
        SelectedAudioChannelOption = (ReferenceChannel)channel.AudioChannelDriver.AudioChannelId;
        channel.AudioChannelDriver.SetSamplerFunction(Sampler);
        _vm.Workspace.Renderer.InvalidateParamsBuffer();
        HasAudioDriver = true;
        OnPropertyChanged(string.Empty);
        _vm.Workspace.RaiseAnimationFrameChanged();
    }

    //TODO: add ChannelDriver Command Parameter
    [RelayCommand]
    public void RemoveChannelDriver()
    {
        _vm.Workspace.TakeSnapshot();
        channel.AudioChannelDriver = null;
        _vm.Workspace.Renderer.InvalidateParamsBuffer();
        HasAudioDriver = false;
        OnPropertyChanged(string.Empty);
        _vm.Workspace.RaiseAnimationFrameChanged();
    }


    [RelayCommand]
    public void CloseChannelEditor() => AnimationVM.CloseChannelEditor();

}
