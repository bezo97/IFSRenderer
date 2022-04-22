#nullable enable
using Cavern.Format;
using Clip = Cavern.Clip;
using IFSEngine.Animation;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;
using System.Windows.Input;
using WpfDisplay.Models;
using WpfDisplay.Helper;
using Cavern.Utilities;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class KeyframeViewModel
{
    public readonly Keyframe _k;

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

    public KeyframeViewModel(Keyframe k, bool isSelected)
    {
        _k = k;
        _isSelected = isSelected;
    }
}

[ObservableObject]
public partial class ChannelViewModel
{
    public string Name { get; }
    public List<KeyframeViewModel> Keyframes { get; }
    public readonly Channel channel;

    public ChannelViewModel(string name, Channel c, List<Keyframe> selectedKeyframes)
    {
        Name = name;
        Keyframes = c.Keyframes.Select(k => new KeyframeViewModel(k.Value, selectedKeyframes.Contains(k.Value))).ToList();
        channel = c;
    }
}

[ObservableObject]
public partial class AnimationViewModel
{
    private readonly Workspace _workspace;
    private readonly Timer _realtimePlayer;
    private readonly List<Keyframe> _selectedKeyframes = new();
    public Clip? LoadedAudioClip { get; private set; } = null;
    public FFTCache AudioClipCache { get; private set; }
    [ObservableProperty] public string? _audioClipTitle = null;

    public TimeOnly CurrentTime { get; set; } = TimeOnly.MinValue;

    public List<ChannelViewModel> Channels => _workspace.Ifs.Dopesheet.Channels.ToList().Select(a=> new ChannelViewModel(a.Key, a.Value, _selectedKeyframes)).ToList();

    public AnimationViewModel(Workspace workspace)
    {
        this._workspace = workspace;
        workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
        _realtimePlayer = new Timer(TimeSpan.FromSeconds(1.0/workspace.Ifs.Dopesheet.Fps).TotalMilliseconds);
        _realtimePlayer.Elapsed += OnPlayerTick;
        _realtimePlayer.AutoReset = true;
    }

    public float CurrentTimeScrollPosition => (float)CurrentTime.ToTimeSpan().TotalSeconds * 50.0f /* * ViewScale */;

    private ValueSliderViewModel? _fpsSlider;
    public ValueSliderViewModel FpsSlider => _fpsSlider ??= new ValueSliderViewModel(_workspace)
    {
        Label = "🎞 Framerate (fps)",
        ToolTip = "Frames per second",
        DefaultValue = 25,
        GetV = () => _workspace.Ifs.Dopesheet.Fps,
        SetV = (value) =>
        {
            _workspace.Ifs.Dopesheet.Fps = (int)value;
            _realtimePlayer.Interval = TimeSpan.FromSeconds(1.0 / _workspace.Ifs.Dopesheet.Fps).TotalMilliseconds;
        },
        Increment = 1,
        MinValue = 1,
        ValueWillChange = _workspace.TakeSnapshot
    };

    private ValueSliderViewModel? _currentTimeSlider;
    public ValueSliderViewModel CurrentTimeSlider => _currentTimeSlider ??= new ValueSliderViewModel(_workspace)
    {
        Label = "⏱️ Time (s)",
        ToolTip = "Current time in seconds",
        DefaultValue = 0.0,
        GetV = () => CurrentTime.ToTimeSpan().TotalSeconds,
        SetV = (value) =>
        {
            CurrentTime = TimeOnly.FromTimeSpan(TimeSpan.FromSeconds(value));
            _workspace.Ifs.Dopesheet.EvaluateAt(_workspace.Ifs, CurrentTime);

            _workspace.Renderer.InvalidateParamsBuffer();
            OnPropertyChanged(nameof(CurrentTimeScrollPosition));
        },
        Increment = 1.0/60,
        MinValue = 0,
    };

    public void RaiseChannelsPropertyChanged()
    {
        OnPropertyChanged(nameof(Channels));
    }

    private ValueSliderViewModel? _lengthSlider;
    public ValueSliderViewModel LengthSlider => _lengthSlider ??= new ValueSliderViewModel(_workspace)
    {
        Label = "↔️ Length (s)",
        ToolTip = "Animation length in seconds",
        DefaultValue = 10,
        GetV = () => _workspace.Ifs.Dopesheet.Length.TotalSeconds,
        SetV = (value) =>
        {
            _workspace.Ifs.Dopesheet.SetLength(TimeSpan.FromSeconds(value));
            CurrentTimeSlider.MaxValue = value;
            CurrentTimeSlider.Value = CurrentTimeSlider.Value;//TODO: ugh, raise..
        },
        Increment = 1,
        MinValue = 1,
        ValueWillChange = _workspace.TakeSnapshot
    };

    [ICommand]
    public void PlayPause()
    {
        _realtimePlayer.Enabled = !_realtimePlayer.Enabled;
    }

    [ICommand]
    public void JumpToStart()
    {
        _realtimePlayer.Stop();
        CurrentTime = TimeOnly.MinValue;
        CurrentTimeSlider.Value = CurrentTimeSlider.GetV();//ugh
        OnPropertyChanged(nameof(CurrentTimeScrollPosition));
    }

    [ICommand]
    public void RemoveChannel(ChannelViewModel cvm)
    {
        _workspace.TakeSnapshot();
        _workspace.Ifs.Dopesheet.RemoveChannel(cvm.Name, CurrentTime);
        RaiseChannelsPropertyChanged();
    }

    [ICommand]
    public void AddToSelection(KeyframeViewModel kvm)
    {
        _selectedKeyframes.Add(kvm._k);
        kvm.IsSelected = true;
    }

    [ICommand]
    public void MoveSelectedKeyframes(double offset)
    {
        _workspace.TakeSnapshot();
        foreach (var kf in _selectedKeyframes)
        {
            kf.t += offset / 50.0/* / ViewScale */;
        }
        _selectedKeyframes.Clear();
        RaiseChannelsPropertyChanged();
    }

    private void OnPlayerTick(object? sender, ElapsedEventArgs e)
    {
        CurrentTime = CurrentTime.Add(TimeSpan.FromSeconds(1.0 / _workspace.Ifs.Dopesheet.Fps));
        if (CurrentTime.ToTimeSpan() > _workspace.Ifs.Dopesheet.Length)
            CurrentTime = TimeOnly.MinValue;
        _workspace.Ifs.Dopesheet.EvaluateAt(_workspace.Ifs, CurrentTime);

        _workspace.Renderer.InvalidateParamsBuffer();

        //raise..
        CurrentTimeSlider.Value = CurrentTimeSlider.GetV();//ugh
        OnPropertyChanged(nameof(CurrentTimeScrollPosition));

    }

    [ICommand]
    public void LoadAudio()
    {
        if (DialogHelper.ShowOpenSoundDialog(out string path))
        {
            var r = new RIFFWaveReader(path);
            LoadedAudioClip = r.ReadClip();
            AudioClipCache = new FFTCache(512);//TODO: user setting?
            AudioClipTitle = LoadedAudioClip.Name ?? System.IO.Path.GetFileNameWithoutExtension(path);
        }
    }


}
