#nullable enable
using Cavern.Format;
using Clip = Cavern.Clip;
using IFSEngine.Animation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    public readonly Workspace Workspace;
    private readonly Timer _realtimePlayer;
    public List<KeyframeViewModel> SelectedKeyframes { get; } = new();

    private MediaPlayer? _audioPlayer;
    public Clip? LoadedAudioClip { get; private set; } = null;
    public FFTCache? AudioClipCache { get; private set; } = null;
    [ObservableProperty] private ReferenceChannel[] _loadedAudioChannels = Array.Empty<ReferenceChannel>();
    [ObservableProperty] public string? _audioClipTitle = null;

    public TimeOnly CurrentTime { get; set; } = TimeOnly.MinValue;

    [ObservableProperty] private ObservableCollection<ChannelViewModel> _channels = new();

    public float SheetWidth => (float)Workspace.Ifs.Dopesheet.Length.TotalSeconds * 50.0f/*view scale*/;

    public AnimationViewModel(Workspace workspace)
    {
        this.Workspace = workspace;
        workspace.LoadedParamsChanged += (s, e) =>
        {
            SelectedKeyframes.Clear();
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
    public ValueSliderViewModel FpsSlider => _fpsSlider ??= new ValueSliderViewModel(Workspace)
    {
        Label = "🎞 Framerate (fps)",
        ToolTip = "Frames per second",
        DefaultValue = 25,
        GetV = () => Workspace.Ifs.Dopesheet.Fps,
        SetV = (value) =>
        {
            Workspace.Ifs.Dopesheet.Fps = (int)value;
            _realtimePlayer.Interval = TimeSpan.FromSeconds(1.0 / Workspace.Ifs.Dopesheet.Fps).TotalMilliseconds;
        },
        Increment = 1,
        MinValue = 1,
        ValueWillChange = Workspace.TakeSnapshot
    };

    private ValueSliderViewModel? _currentTimeSlider;
    public ValueSliderViewModel CurrentTimeSlider => _currentTimeSlider ??= new ValueSliderViewModel(Workspace)
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
        Channels = new(Workspace.Ifs.Dopesheet.Channels.ToList()
            .Select(a => new ChannelViewModel(this, a.Key, a.Value)));
    }

    /// <summary>
    /// null: not animated. false: interpolated. true: keyframe
    /// </summary>
    public bool? GetChannelCurrentState(string path)
    {
        if (!Workspace.Ifs.Dopesheet.Channels.TryGetValue(path, out var channel))
            return null;
        var currentTimeSeconds = CurrentTime.ToTimeSpan().TotalSeconds;
        return channel.Keyframes.Any(kf => kf.t == currentTimeSeconds);
    }

    private ValueSliderViewModel? _lengthSlider;
    public ValueSliderViewModel LengthSlider => _lengthSlider ??= new ValueSliderViewModel(Workspace)
    {
        Label = "↔️ Length (s)",
        ToolTip = "Animation length in seconds",
        DefaultValue = 10,
        GetV = () => Workspace.Ifs.Dopesheet.Length.TotalSeconds,
        SetV = (value) =>
        {
            Workspace.Ifs.Dopesheet.SetLength(TimeSpan.FromSeconds(value));
            CurrentTimeSlider.MaxValue = value;
            OnPropertyChanged(nameof(SheetWidth));
        },
        Increment = 1,
        MinValue = 1,
        ValueWillChange = Workspace.TakeSnapshot
    };

    [RelayCommand]
    public void PlayPause()
    {
        _realtimePlayer.Enabled = !_realtimePlayer.Enabled;
        if (_realtimePlayer.Enabled)
            _audioPlayer?.Play();
        else
            _audioPlayer?.Pause();
    }

    [RelayCommand]
    public void JumpToStart()
    {
        _realtimePlayer.Stop();
        _audioPlayer?.Stop();
        JumpToTime(0);
    }

    [RelayCommand]
    public void JumpToKeyframe(KeyframeViewModel kfv)
    {
        JumpToTime(kfv._k.t);
    }

    private void JumpToTime(double t)
    {
        CurrentTime = TimeOnly.FromTimeSpan(TimeSpan.FromSeconds(t));
        Workspace.Ifs.Dopesheet.EvaluateAt(Workspace.Ifs, CurrentTime);
        Workspace.Renderer.InvalidateParamsBuffer();
        if (_audioPlayer is not null && !_realtimePlayer.Enabled)//hack
            _audioPlayer.Position = CurrentTime.ToTimeSpan();
        CurrentTimeSlider.RaiseValueChanged();
        OnPropertyChanged(nameof(CurrentTimeScrollPosition));
        Workspace.RaiseAnimationFrameChanged();
    }

    public void AddOrUpdateChannel(string name, string path, double value)
    {
        var vm = Channels.FirstOrDefault(c => c.Path == path);
        if (vm is null)
        {//add new channel with single keyframe
            Workspace.Ifs.Dopesheet.AddOrUpdateChannel(name, path, CurrentTime, value);
            Channels.Add(new ChannelViewModel(this, path, Workspace.Ifs.Dopesheet.Channels[path]));
        }
        else
        {
            //remove existing keyframe if value equals
            var currentTimeSeconds = CurrentTime.ToTimeSpan().TotalSeconds;
            var keyOnFrame = vm.Keyframes.FirstOrDefault(kf => kf._k.Value == value && kf._k.t == currentTimeSeconds);
            if (keyOnFrame is not null)
            {
                if(vm.Keyframes.Count == 1)
                {
                    Workspace.Ifs.Dopesheet.RemoveChannel(vm.Path, CurrentTime);
                    Channels.Remove(vm);
                }
                else
                    vm.RemoveKeyframe(keyOnFrame);
            }
            else
                Workspace.Ifs.Dopesheet.AddOrUpdateChannel(name, path, CurrentTime, value);

            vm.UpdateKeyframes();
        }
    }

    [RelayCommand]
    public void RemoveKeyframe(KeyframeViewModel kfv)
    {
        Workspace.TakeSnapshot();
        kfv._cvm.RemoveKeyframe(kfv);
        Workspace.Ifs.Dopesheet.EvaluateAt(Workspace.Ifs, CurrentTime);
        Workspace.Renderer.InvalidateParamsBuffer();
        Workspace.RaiseAnimationFrameChanged();
    }

    [RelayCommand]
    public void RemoveChannel(ChannelViewModel cvm)
    {
        Workspace.TakeSnapshot();
        Workspace.Ifs.Dopesheet.RemoveChannel(cvm.Path, CurrentTime);
        Channels.Remove(cvm);
    }

    [RelayCommand]
    public void AddToSelection(KeyframeViewModel kvm)
    {
        SelectedKeyframes.Add(kvm);
        kvm.IsSelected = true;
    }

    [RelayCommand]
    public void MoveSelectedKeyframes(double offset)
    {
        Workspace.TakeSnapshot();
        foreach (var kf in SelectedKeyframes)
        {
            kf._k.t += offset / 50.0/* / ViewScale */;
            kf.PositionMoveOffset = 0.0;
            kf.IsSelected = false;
        }
        Workspace.Renderer.InvalidateParamsBuffer();
        SelectedKeyframes.Clear();
    }

    private void OnPlayerTick(object? sender, ElapsedEventArgs e)
    {
        var nextTimestep = CurrentTime.Add(TimeSpan.FromSeconds(1.0 / Workspace.Ifs.Dopesheet.Fps));
        if (nextTimestep.ToTimeSpan() > Workspace.Ifs.Dopesheet.Length)
        {
            nextTimestep = TimeOnly.MinValue;
            _audioPlayer?.Dispatcher.Invoke(() => _audioPlayer.Position = TimeSpan.Zero);
        }
        JumpToTime(nextTimestep.ToTimeSpan().TotalSeconds);
    }

    [RelayCommand]
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
