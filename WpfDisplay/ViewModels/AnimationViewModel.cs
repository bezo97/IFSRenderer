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
using IFSEngine.Animation.ChannelDrivers;

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

[ObservableObject]
public partial class ChannelViewModel
{
    public string Name { get; }

    private readonly Workspace _workspace;
    private readonly AnimationViewModel _vm;
    public readonly Channel channel;
    [ObservableProperty] private ObservableCollection<KeyframeViewModel> _keyframes = new();
    [ObservableProperty] private bool _isEditing = false; 
    [ObservableProperty] private bool _hasDetails = false;
    [ObservableProperty] private List<AudioChannelOption> _audioChannelOptions = new();
    private AudioChannelOption _selectedAudioChannelOption = default!;
    public AudioChannelOption SelectedAudioChannelOption {
        get => _selectedAudioChannelOption; 
        set
        {
            _selectedAudioChannelOption = value;
            HasDetails = value.index > -1;
            if (value.index == -1)//Remove audio channel
            {
                channel.AudioChannelDriver = null;
                return;
            }
            channel.AudioChannelDriver ??= new AudioChannelDriver();
            channel.AudioChannelDriver.AudioChannelIndex = value.index;
            channel.AudioChannelDriver.SetSamplerFunction(Sampler);
            EffectSlider.Value = EffectSlider.Value;//ugh
            MinFreqSlider.Value = MinFreqSlider.Value;//ugh
            MaxFreqSlider.Value = MaxFreqSlider.Value;//ugh
        }
    }

    private float Sampler(AudioChannelDriver d, double t)
    {
        if (_vm.LoadedAudioClip is null)
            return 0.0f;
        return CavernHelper.CavernSampler(_vm.LoadedAudioClip, _vm.AudioClipCache!, d.MinFrequency, d.MaxFrequency, t);
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

    public record AudioChannelOption(string name, int index);

    public ChannelViewModel(Workspace workspace, AnimationViewModel vm, string name, Channel c, List<KeyframeViewModel> selectedKeyframes)
    {
        _workspace = workspace;
        _vm = vm;
        Name = name;
        channel = c;

        //TODO: AudioChannelOptions = audioclip.channels + no choice;
        AudioChannelOptions = new List<AudioChannelOption> {
            new AudioChannelOption("None", -1),
            new AudioChannelOption("Stereo Left", 0), 
            new AudioChannelOption("Stereo Right", 1) 
        };
        SelectedAudioChannelOption = AudioChannelOptions[(c.AudioChannelDriver?.AudioChannelIndex+1) ?? 0];
        HasDetails = c.AudioChannelDriver is not null;

        UpdateKeyframes(selectedKeyframes);
    }

    public void RemoveKeyframe(KeyframeViewModel kfv)
    {
        Keyframes.Remove(kfv);
        channel.RemoveKeyframe(kfv._k);
    }

    public void UpdateKeyframes(List<KeyframeViewModel> selectedKeyframes)
    {
        var sk = selectedKeyframes.Select(kvm => kvm._k).ToList();
        Keyframes = new ObservableCollection<KeyframeViewModel>(channel.Keyframes
            .Select(k => new KeyframeViewModel(this, k.Value, sk.Contains(k.Value))));
    }

    [ICommand]
    public void EditChannel()
    {
        IsEditing = !IsEditing;
    }

}

[ObservableObject]
public partial class AnimationViewModel
{
    private readonly Workspace _workspace;
    private readonly Timer _realtimePlayer;
    private readonly List<KeyframeViewModel> _selectedKeyframes = new();

    private MediaPlayer? _audioPlayer;
    public Clip? LoadedAudioClip { get; private set; } = null;
    public FFTCache? AudioClipCache { get; private set; }
    [ObservableProperty] public string? _audioClipTitle = null;

    public TimeOnly CurrentTime { get; set; } = TimeOnly.MinValue;

    [ObservableProperty] private ObservableCollection<ChannelViewModel> _channels = new();

    public float SheetWidth => (float)_workspace.Ifs.Dopesheet.Length.TotalSeconds * 50.0f/*view scale*/;

    public AnimationViewModel(Workspace workspace)
    {
        this._workspace = workspace;
        workspace.PropertyChanged += (s, e) =>
        {
            _selectedKeyframes.Clear();
            UpdateChannels();
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
            CurrentTime = TimeOnly.FromTimeSpan(TimeSpan.FromSeconds(value));
            _workspace.Ifs.Dopesheet.EvaluateAt(_workspace.Ifs, CurrentTime);
            _workspace.Renderer.InvalidateParamsBuffer();
            if (_audioPlayer is not null && !_realtimePlayer.Enabled)//hack
                _audioPlayer.Position = TimeSpan.FromSeconds(value);
            OnPropertyChanged(nameof(CurrentTimeScrollPosition));
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
            CurrentTimeSlider.Value = CurrentTimeSlider.Value;//TODO: ugh, raise..
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
        if(_audioPlayer is not null)
            _audioPlayer.Position = TimeSpan.Zero;
        CurrentTime = TimeOnly.MinValue;
        CurrentTimeSlider.Value = CurrentTimeSlider.GetV();//ugh
        OnPropertyChanged(nameof(CurrentTimeScrollPosition));
    }

    [ICommand]
    public void JumpToTime(KeyframeViewModel kfv)
    {
        var keyframeTime = TimeSpan.FromSeconds(kfv._k.t);
        CurrentTime = TimeOnly.FromTimeSpan(keyframeTime);
        if (_audioPlayer is not null)
            _audioPlayer.Position = keyframeTime;
        CurrentTimeSlider.Value = CurrentTimeSlider.GetV();//ugh
        OnPropertyChanged(nameof(CurrentTimeScrollPosition));
    }

    //[ICommand]
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
        kfv._cvm.RemoveKeyframe(kfv);
        _workspace.Ifs.Dopesheet.EvaluateAt(_workspace.Ifs, CurrentTime);
        _workspace.Renderer.InvalidateParamsBuffer();
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
        CurrentTime = CurrentTime.Add(TimeSpan.FromSeconds(1.0 / _workspace.Ifs.Dopesheet.Fps));
        if (CurrentTime.ToTimeSpan() > _workspace.Ifs.Dopesheet.Length)
        {
            CurrentTime = TimeOnly.MinValue;
            if (_audioPlayer is not null)
                _audioPlayer.Dispatcher.Invoke(() => _audioPlayer.Position = TimeSpan.Zero);
        }
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
            _audioPlayer = new MediaPlayer();
            _audioPlayer.Open(new Uri(path));
            AudioClipTitle = LoadedAudioClip.Name ?? System.IO.Path.GetFileNameWithoutExtension(path);
            CreateAudioBarsDrawing();
            OnPropertyChanged(nameof(LoadedAudioClip));
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
