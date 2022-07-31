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
using System.Windows.Media;
using Cavern.Remapping;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class AnimationViewModel
{
    private readonly Workspace _workspace;
    private readonly Timer _realtimePlayer;
    private readonly List<KeyframeViewModel> _selectedKeyframes = new();

    private MediaPlayer? _audioPlayer;
    public Clip? LoadedAudioClip { get; private set; } = null;
    public FFTCache? AudioClipCache { get; private set; } = null;
    public ReferenceChannel[] LoadedAudioChannels { get; private set; } = Array.Empty<ReferenceChannel>();
    [ObservableProperty] public string? _audioClipTitle = null;

    public TimeOnly CurrentTime { get; set; } = TimeOnly.MinValue;

    [ObservableProperty] private ObservableCollection<ChannelViewModel> _channels = new();

    public float SheetWidth => (float)_workspace.Ifs.Dopesheet.Length.TotalSeconds * 50.0f/*view scale*/;

    public AnimationViewModel(Workspace workspace)
    {
        this._workspace = workspace;
        workspace.LoadedParamsChanged += (s, e) =>
        {
            _selectedKeyframes.Clear();
            UpdateChannels();
            CurrentTimeSlider.MaxValue = workspace.Ifs.Dopesheet?.Length.TotalSeconds;
            OnPropertyChanged(string.Empty);
        };
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
            JumpToTime(value);
        },
        Increment = 1.0/60,
        MinValue = 0,
    };

    public void UpdateChannels()
    {
        Channels = new(_workspace.Ifs.Dopesheet.Channels.ToList()
            .Select(a => new ChannelViewModel(_workspace, this, a.Key, a.Value, _selectedKeyframes)));
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
            OnPropertyChanged(nameof(SheetWidth));
        },
        Increment = 1,
        MinValue = 1,
        ValueWillChange = _workspace.TakeSnapshot
    };

    [ICommand]
    public void PlayPause()
    {
        _realtimePlayer.Enabled = !_realtimePlayer.Enabled;
        if (_realtimePlayer.Enabled)
            _audioPlayer?.Play();
        else
            _audioPlayer?.Pause();
    }

    [ICommand]
    public void JumpToStart()
    {
        _realtimePlayer.Stop();
        _audioPlayer?.Stop();
        JumpToTime(0);
    }

    [ICommand]
    public void JumpToKeyframe(KeyframeViewModel kfv)
    {
        JumpToTime(kfv._k.t);
    }

    private void JumpToTime(double t)
    {
        CurrentTime = TimeOnly.FromTimeSpan(TimeSpan.FromSeconds(t));
        _workspace.Ifs.Dopesheet.EvaluateAt(_workspace.Ifs, CurrentTime);
        _workspace.Renderer.InvalidateParamsBuffer();
        if (_audioPlayer is not null && !_realtimePlayer.Enabled)//hack
            _audioPlayer.Position = CurrentTime.ToTimeSpan();
        CurrentTimeSlider.RaiseValueChanged();
        OnPropertyChanged(nameof(CurrentTimeScrollPosition));
        _workspace.RaiseAnimationFrameChanged();
    }

    public void AddOrUpdateChannel(string path, double value)
    {
        _workspace.Ifs.Dopesheet.AddOrUpdateChannel(path, CurrentTime, value);
        var vm = Channels.FirstOrDefault(c => c.Name == path);
        if (vm is null)//add vm
            Channels.Add(new ChannelViewModel(_workspace, this, path, _workspace.Ifs.Dopesheet.Channels[path], _selectedKeyframes));
        else
            vm.UpdateKeyframes(_selectedKeyframes);
    }

    [ICommand]
    public void RemoveKeyframe(KeyframeViewModel kfv)
    {
        _workspace.TakeSnapshot();
        kfv._cvm.RemoveKeyframe(kfv);
        _workspace.Ifs.Dopesheet.EvaluateAt(_workspace.Ifs, CurrentTime);
        _workspace.Renderer.InvalidateParamsBuffer();
        _workspace.RaiseAnimationFrameChanged();
    }

    [ICommand]
    public void RemoveChannel(ChannelViewModel cvm)
    {
        _workspace.TakeSnapshot();
        _workspace.Ifs.Dopesheet.RemoveChannel(cvm.Name, CurrentTime);
        Channels.Remove(cvm);
    }

    [ICommand]
    public void AddToSelection(KeyframeViewModel kvm)
    {
        _selectedKeyframes.Add(kvm);
        kvm.IsSelected = true;
    }

    [ICommand]
    public void MoveSelectedKeyframes(double offset)
    {
        _workspace.TakeSnapshot();
        foreach (var kf in _selectedKeyframes)
        {
            kf._k.t += offset / 50.0/* / ViewScale */;
            kf.PositionMoveOffset = 0.0;
            kf.IsSelected = false;
        }
        _workspace.Renderer.InvalidateParamsBuffer();
        _selectedKeyframes.Clear();
    }

    private void OnPlayerTick(object? sender, ElapsedEventArgs e)
    {
        var nextTimestep = CurrentTime.Add(TimeSpan.FromSeconds(1.0 / _workspace.Ifs.Dopesheet.Fps));
        if (nextTimestep.ToTimeSpan() > _workspace.Ifs.Dopesheet.Length)
        {
            nextTimestep = TimeOnly.MinValue;
            if (_audioPlayer is not null)
                _audioPlayer.Dispatcher.Invoke(() => _audioPlayer.Position = TimeSpan.Zero);
        }
        JumpToTime(nextTimestep.ToTimeSpan().TotalSeconds);
    }

    [ICommand]
    public void LoadAudio()
    {
        if (DialogHelper.ShowOpenSoundDialog(out string path))
        {
            var r = new RIFFWaveReader(path);
            LoadedAudioClip = r.ReadClip();
            AudioClipCache = new FFTCache(CavernHelper.defaultSamplingResolution);
            LoadedAudioChannels = ChannelPrototype.GetStandardMatrix(LoadedAudioClip.Channels);
            _audioPlayer = new MediaPlayer();
            _audioPlayer.Open(new Uri(path));
            AudioClipTitle = LoadedAudioClip.Name ?? System.IO.Path.GetFileNameWithoutExtension(path);
            CreateAudioBarsDrawing();
            OnPropertyChanged(nameof(LoadedAudioClip));
            OnPropertyChanged(nameof(LoadedAudioChannels));
        }
    }

    [ObservableProperty] private DrawingImage _audioBarsDrawing = default!;
    private void CreateAudioBarsDrawing()
    {
        var g = new DrawingGroup();
        const double bars_resolution = 0.1;
        //const double bars_offset = bars_resolution * 50.0/*view scale*/;
        for (double t = 0.0; t < LoadedAudioClip!.Length; t += bars_resolution)
        {
            int position = (int)(t * LoadedAudioClip.SampleRate);
            float[] samples = new float[512];//2 hatványa!
            LoadedAudioClip.GetData(samples, 0/*left channel*/, position);
            float barHeight = samples.Max();

            var d = new GeometryDrawing
            {
                Brush = new LinearGradientBrush(Color.FromRgb(55, 55, 55), Color.FromRgb(100, 100, 100), 90.0),
                Geometry = new RectangleGeometry(new System.Windows.Rect(t * 50.0-2.5, 30 - 30 * barHeight, 4.0, 30.0))
            };
            g.Children.Add(d);
        }
        AudioBarsDrawing = new DrawingImage(g);
        AudioBarsDrawing.Freeze();
    }


}
