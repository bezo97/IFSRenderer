#nullable enable
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

namespace WpfDisplay.ViewModels;

public class KeyframeViewModel
{
    private readonly Keyframe _k;
    
    public float TimelinePositon => (float)_k.t * 50.0f/* * ViewScale */;//add offset

    public KeyframeViewModel(Keyframe k)
    {
        _k = k;
    }
}

public class ChannelViewModel
{
    public string Name { get; }
    public List<KeyframeViewModel> Keyframes { get; }

    public ChannelViewModel(string name, Channel c)
    {
        Name = name;
        Keyframes = c.Keyframes.Select(k => new KeyframeViewModel(k)).ToList();
    }
}

[ObservableObject]
public partial class AnimationViewModel
{
    private readonly Workspace _workspace;

    private readonly Timer _realtimePlayer;
    public TimeOnly CurrentTime { get; set; } = TimeOnly.MinValue;

    public List<ChannelViewModel> Channels => _workspace.Ifs.Dopesheet.Channels.ToList().Select(a=> new ChannelViewModel(a.Key, a.Value)).ToList();

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
        Label = "🎞 FPS",
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
        Label = "⏱️ Time",
        ToolTip = "Current time in seconds",
        DefaultValue = 0.0,
        GetV = () => CurrentTime.ToTimeSpan().TotalSeconds,
        SetV = (value) =>
        {
            CurrentTime = TimeOnly.FromTimeSpan(TimeSpan.FromSeconds(value));
            Dopesheet.T = CurrentTime.ToTimeSpan().TotalSeconds;
            _workspace.Ifs.Dopesheet.EvaluateAt(_workspace.Ifs, CurrentTime);
            _workspace.Renderer.InvalidateParamsBuffer();
            OnPropertyChanged(nameof(CurrentTimeScrollPosition));
        },
        Increment = 1.0/60,
        MinValue = 0,
    };

    private ValueSliderViewModel? _lengthSlider;
    public ValueSliderViewModel LengthSlider => _lengthSlider ??= new ValueSliderViewModel(_workspace)
    {
        Label = "↔️ Length",
        ToolTip = "Animation length in seconds",
        DefaultValue = 10,
        GetV = () => _workspace.Ifs.Dopesheet.Length.TotalSeconds,
        SetV = (value) =>
        {
            _workspace.Ifs.Dopesheet.SetLength(TimeSpan.FromSeconds(value));
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

        //debug
        OnPropertyChanged(nameof(Channels));
    }

    private void OnPlayerTick(object? sender, ElapsedEventArgs e)
    {
        CurrentTime = CurrentTime.Add(TimeSpan.FromSeconds(1.0 / _workspace.Ifs.Dopesheet.Fps));
        if (CurrentTime.ToTimeSpan() > _workspace.Ifs.Dopesheet.Length)
            CurrentTime = TimeOnly.MinValue;
        Dopesheet.T = CurrentTime.ToTimeSpan().TotalSeconds;
        _workspace.Ifs.Dopesheet.EvaluateAt(_workspace.Ifs, CurrentTime);
        _workspace.Renderer.InvalidateParamsBuffer();

        //raise..
        CurrentTimeSlider.Value = CurrentTimeSlider.GetV();//ugh
        OnPropertyChanged(nameof(CurrentTimeScrollPosition));

    }

}
