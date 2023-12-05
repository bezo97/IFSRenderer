#nullable enable
using Cavern.Channels;
using Cavern.Format;
using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFSEngine.Model;
using IFSEngine.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfDisplay.Helper;
using WpfDisplay.Models;
using Clip = Cavern.Clip;

namespace WpfDisplay.ViewModels;

public partial class AnimationViewModel : ObservableObject
{
    public readonly Workspace Workspace;
    private readonly Timer _realtimePlayer;
    public HashSet<KeyframeViewModel> SelectedKeyframes { get; } = [];

    private MediaPlayer? _audioPlayer;
    public CavernAudio? Audio { get; private set; } = null;
    [ObservableProperty] private ReferenceChannel[] _loadedAudioChannels = [];
    [ObservableProperty] private string? _audioClipTitle = null;
    [ObservableProperty] private double? _keyframeInsertPosition = null;//location of the context menu over the channel
    [ObservableProperty] private bool _isRenderingFrames = false;
    private string? _saveFramesPath = null;

    public TimeOnly CurrentTime { get; private set; } = TimeOnly.MinValue;

    public ObservableCollection<ChannelViewModel> Channels { get; } = [];
    [ObservableProperty] private ChannelViewModel? _editedChannel = null;

    /// <summary>
    /// pixels per second
    /// </summary>
    [ObservableProperty] private float _viewScale = 50.0f;

    public float SheetWidth => (float)Workspace.Ifs.Dopesheet.Length.TotalSeconds * 50.0f/*view scale*/;

    public double KeyframeRepositionOffset { get; internal set; }

    [ObservableProperty] private double _currentTimeIncrement = 1.0 / IFS.Default.Dopesheet.Fps;

    public AnimationViewModel(Workspace workspace)
    {
        this.Workspace = workspace;
        workspace.LoadedParamsChanged += (s, e) =>
        {
            EditedChannel = null;
            SelectedKeyframes.Clear();
            Channels.Clear();
            Workspace.Ifs.Dopesheet.Channels.ToList()
                .Select(a => new ChannelViewModel(this, a.Key, a.Value)).ToList().ForEach(Channels.Add);
            OnPropertyChanged(string.Empty);
        };
        workspace.Renderer.TargetIterationReached += OnFrameFinishedRendering;
        _realtimePlayer = new Timer(TimeSpan.FromSeconds(1.0 / workspace.Ifs.Dopesheet.Fps).TotalMilliseconds);
        _realtimePlayer.Elapsed += OnPlayerTick;
        _realtimePlayer.AutoReset = true;

    }

    public float CurrentTimeScrollPosition => (float)CurrentTime.ToTimeSpan().TotalSeconds * 50.0f /* * ViewScale */;

    public double ClipFps
    {
        get => Workspace.Ifs.Dopesheet.Fps;
        set
        {
            Workspace.Ifs.Dopesheet.Fps = (int)value;
            _realtimePlayer.Interval = TimeSpan.FromSeconds(1.0 / Workspace.Ifs.Dopesheet.Fps).TotalMilliseconds;
            CurrentTimeIncrement = 1 / value;
        }
    }

    private ValueSliderSettings? _fpsSlider;
    public ValueSliderSettings FpsSlider => _fpsSlider ??= new()
    {
        Label = "🎞 Framerate (fps)",
        ToolTip = "Frames per second",
        DefaultValue = 30,
        Increment = 1,
        MinValue = 1,
        ValueWillChange = Workspace.TakeSnapshot
    };

    public double CurrentTimeSeconds
    {
        get => CurrentTime.ToTimeSpan().TotalSeconds;
        set => JumpToTime(value);
    }

    private ValueSliderSettings? _currentTimeSlider;
    public ValueSliderSettings CurrentTimeSlider => _currentTimeSlider ??= new()
    {
        Label = "⏱️ Time (s)",
        ToolTip = "Current time in seconds",
        DefaultValue = 0.0,
        Increment = 1.0 / Workspace.Ifs.Dopesheet.Fps,
        MinValue = 0,
        MaxValue = Workspace.Ifs.Dopesheet.Length.TotalSeconds
    };

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

    public bool IsChannelBeingModified(string path, double value)
    {
        if (!Workspace.Ifs.Dopesheet.Channels.TryGetValue(path, out var channel))
            return false;
        return Math.Abs(channel.EvaluateAt(CurrentTimeSeconds) - value) > 0.001;
    }

    public double ClipLength
    {
        get => Workspace.Ifs.Dopesheet.Length.TotalSeconds;
        set
        {
            Workspace.Ifs.Dopesheet.Length = TimeSpan.FromSeconds(value);
            OnPropertyChanged(nameof(ClipLength));
            OnPropertyChanged(nameof(SheetWidth));
        }
    }

    private ValueSliderSettings? _lengthSlider;
    public ValueSliderSettings LengthSlider => _lengthSlider ??= new()
    {
        Label = "↔️ Length (s)",
        ToolTip = "Animation length in seconds",
        DefaultValue = 10,
        Increment = 1,
        MinValue = 1,
        ValueWillChange = Workspace.TakeSnapshot
    };


    public class AnimateValueCommandParameters
    {
        public AnimateValueCommandParameters(string label, string path, double val)
        {
            Label = label;
            Path = path;
            Val = val;
        }

        public string Label { get; }
        public string Path { get; }
        public double Val { get; }
    }
    
    [RelayCommand]
    public void AnimateValue(AnimateValueCommandParameters param)
    {
        Workspace.TakeSnapshot();
        AddOrUpdateChannel(param.Label, param.Path, param.Val);
    }

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

    public void JumpToTime(double t)
    {
        CurrentTime = TimeOnly.FromTimeSpan(TimeSpan.FromSeconds(t));
        Workspace.Renderer.Seed = (uint)(Workspace.Ifs.Dopesheet.Fps * t);//set frame number as seed
        Workspace.Ifs.Dopesheet.EvaluateAt(Workspace.Ifs, CurrentTime);
        Workspace.Renderer.InvalidateParamsBuffer();
        if (_audioPlayer is not null && !_realtimePlayer.Enabled)//hack
            _audioPlayer?.Dispatcher.Invoke(() => _audioPlayer.Position = CurrentTime.ToTimeSpan());
        OnPropertyChanged(nameof(CurrentTimeSeconds));
        OnPropertyChanged(nameof(CurrentTimeScrollPosition));
        Workspace.RaiseAnimationFrameChanged();
    }

    /// <returns>True when there is no next frame.</returns>
    private bool JumpToNextFrame()
    {
        var nextTimestep = CurrentTime.Add(TimeSpan.FromSeconds(1.0 / Workspace.Ifs.Dopesheet.Fps));
        var lastFrame = nextTimestep.ToTimeSpan() > Workspace.Ifs.Dopesheet.Length;
        if (lastFrame)
        {
            nextTimestep = TimeOnly.MinValue;
            _audioPlayer?.Dispatcher.Invoke(() => _audioPlayer.Position = TimeSpan.Zero);
        }
        JumpToTime(nextTimestep.ToTimeSpan().TotalSeconds);
        return lastFrame;
    }

    public void AddOrUpdateChannel(string name, string path, double value) => AddOrUpdateChannel(name, path, value, CurrentTime);

    public void AddOrUpdateChannel(string name, string path, double value, TimeOnly position)
    {
        EditedChannel = null;
        var vm = Channels.FirstOrDefault(c => c.Path == path);
        if (vm is null)
        {//add new channel with single keyframe
            Workspace.Ifs.Dopesheet.AddOrUpdateChannel(name, path, position, value);
            Channels.Add(new ChannelViewModel(this, path, Workspace.Ifs.Dopesheet.Channels[path]));
        }
        else
        {
            //remove existing keyframe if value equals
            var positionSeconds = position.ToTimeSpan().TotalSeconds;
            var keyOnFrame = vm.Keyframes.FirstOrDefault(kf => kf._k.Value == value && kf._k.t == positionSeconds);
            if (keyOnFrame is not null)
            {
                if (vm.Keyframes.Count == 1)
                {
                    Workspace.Ifs.Dopesheet.Channels.Remove(vm.Path);
                    Channels.Remove(vm);
                }
                else
                    vm.RemoveKeyframe(keyOnFrame);
            }
            else
                Workspace.Ifs.Dopesheet.AddOrUpdateChannel(name, path, position, value);

            vm.UpdateKeyframes();
        }
    }

    [RelayCommand]
    public void RemoveKeyframe(KeyframeViewModel kfv)
    {
        Workspace.TakeSnapshot();

        if (kfv._cvm.Keyframes.Count == 1)
        {//remove channel when last keyframe is removed
            Workspace.Ifs.Dopesheet.Channels.Remove(kfv._cvm.Path);
            Channels.Remove(kfv._cvm);
        }
        else
        {
            kfv._cvm.RemoveKeyframe(kfv);
        }
        Workspace.Ifs.Dopesheet.EvaluateAt(Workspace.Ifs, CurrentTime);
        Workspace.Renderer.InvalidateParamsBuffer();
        Workspace.RaiseAnimationFrameChanged();
    }

    [RelayCommand]
    public void EditChannel(ChannelViewModel cvm)
    {
        EditedChannel = cvm;
    }

    public void CloseChannelEditor() => EditedChannel = null;

    [RelayCommand]
    public void RemoveChannel(ChannelViewModel cvm)
    {
        Workspace.TakeSnapshot();
        Workspace.Ifs.Dopesheet.Channels.Remove(cvm.Path);
        Channels.Remove(cvm);
        Workspace.Renderer.InvalidateParamsBuffer();
        Workspace.RaiseAnimationFrameChanged();
    }

    public void AddToSelection(KeyframeViewModel kvm)
    {
        SelectedKeyframes.Add(kvm);
        kvm.IsSelected = true;
    }

    public void FlipSelection(KeyframeViewModel kvm)
    {
        if (SelectedKeyframes.Contains(kvm))
        {
            SelectedKeyframes.Remove(kvm);
            kvm.IsSelected = false;
        }
        else
        {
            AddToSelection(kvm);
        }
    }

    [RelayCommand]
    public void ClearSelection()
    {
        foreach (var kvm in SelectedKeyframes)
            kvm.IsSelected = false;
        SelectedKeyframes.Clear();
    }

    public void PreviewRepositionSelectedKeyframes(double offset)
    {
        var timeOffset = offset / 50.0/* / ViewScale */;
        //clamp offset so first and last keyframes dont offset beyond bounds of the animation
        timeOffset = Math.Max(timeOffset, -SelectedKeyframes.Min(k => k.KeyframeTime));
        timeOffset = Math.Min(timeOffset, (float)Workspace.Ifs.Dopesheet.Length.TotalSeconds - SelectedKeyframes.Max(k => k.KeyframeTime));
        KeyframeRepositionOffset = timeOffset * 50/* / ViewScale */;
        foreach (var kf in SelectedKeyframes)
            kf.NotifyPositionChanged();
    }

    public void ApplyRepositionOfSelectedKeyframes()
    {
        Workspace.TakeSnapshot();
        foreach (var kf in SelectedKeyframes)
            kf._k.t += KeyframeRepositionOffset / 50.0/* / ViewScale */;
        KeyframeRepositionOffset = 0.0;
        Workspace.Renderer.InvalidateParamsBuffer();
    }

    private void OnPlayerTick(object? sender, ElapsedEventArgs e)
    {
        JumpToNextFrame();
    }

    [RelayCommand]
    public async Task LoadAudio()
    {
        if (DialogHelper.ShowOpenAudioDialog(out string path))
        {
            _audioPlayer = new MediaPlayer();
            _audioPlayer.Open(new Uri(path));
            await Task.Run(() =>
            {
                //TODO: show loading dialog with cancel
                Audio = new(path);
                LoadedAudioChannels = ChannelPrototype.GetStandardMatrix(Audio.Clip.Channels);
                AudioClipTitle = Audio.Clip.Name ?? System.IO.Path.GetFileNameWithoutExtension(path);

                AudioBarsDrawing = CreateAudioBarsDrawing(Audio.Clip, 50.0/*viewScale*/);

                OnPropertyChanged(nameof(Audio));
                Workspace.UpdateStatusText($"Audio track loaded successfully - {path}");
            });
        }
    }

    [ObservableProperty] private DrawingImage _audioBarsDrawing = default!;
    private static DrawingImage CreateAudioBarsDrawing(Clip audioClip, double viewScale)
    {
        var g = new DrawingGroup();
        const double bars_resolution = 0.1;
        //const double bars_offset = bars_resolution * viewScale;
        for (double t = 0.0; t < audioClip.Length; t += bars_resolution)
        {
            int position = (int)(t * audioClip.SampleRate);
            float[] samples = new float[512];//must be pow of 2
            audioClip.GetData(samples, 0/*left channel*/, position);
            float barHeight = samples.Max();

            var d = new GeometryDrawing
            {
                Brush = new LinearGradientBrush(Color.FromRgb(55, 55, 55), Color.FromRgb(100, 100, 100), 90.0),
                Geometry = new RectangleGeometry(new System.Windows.Rect(t * viewScale - 2.5, 30 - 30 * barHeight, 4.0, 30.0))
            };
            g.Children.Add(d);
        }
        var audioBarsDrawing = new DrawingImage(g);
        audioBarsDrawing.Freeze();
        return audioBarsDrawing;
    }

    private bool _canExecuteStart => !IsRenderingFrames;
    [RelayCommand(/*CanExecute = nameof(_canExecuteStart)*/)]
    public void StartRenderingFrames()
    {
        if(IsRenderingFrames) throw new InvalidOperationException();

        _realtimePlayer.Stop();
        _audioPlayer?.Stop();

        if (!DialogHelper.ShowAnimationFolderBrowserDialog(out string selectedFolderPath))
            return;
        _saveFramesPath = selectedFolderPath;

        if (!Workspace.Renderer.IsRendering)
            Workspace.Renderer.StartRenderLoop();

        IsRenderingFrames = true;
        Workspace.Renderer.InvalidateParamsBuffer();
        Workspace.UpdateStatusText($"Rendering animation frames...");
    }

    private void OnFrameFinishedRendering(object? sender, EventArgs e)
    {
        if(IsRenderingFrames && _saveFramesPath is not null)
        {
            var frameFileExtension = Workspace.IsRawFrameExportEnabled ? "exr" : "png";
            var frameNr = (int)(CurrentTime.ToTimeSpan().TotalSeconds * Workspace.Ifs.Dopesheet.Fps);
            var framePath = Path.Combine(_saveFramesPath, $"{Workspace.Ifs.Title}-{frameNr:D6}.{frameFileExtension}"); //TODO: filter invalid chars

            if (!Workspace.IsRawFrameExportEnabled)
            {//png
                var bitmap = Workspace.Renderer.GetExportBitmapSource(false).Result;
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmap));
                using var fstream = File.Create(framePath);
                enc.Save(fstream);
            }
            else
            {//exr
                var histogramData = Workspace.Renderer.ReadHistogramData().Result;
                using var fstream = File.Create(framePath);
                OpenEXR.WriteStream(fstream, histogramData);
            }

            if (JumpToNextFrame())
            {//was last frame
                if(Workspace.IsExportVideoFileEnabled)
                {
                    RunFfmpegProcess().Wait();
                }
            }
            else
            {
                var totalFrames = Workspace.Ifs.Dopesheet.Length.TotalSeconds * Workspace.Ifs.Dopesheet.Fps;
                Workspace.UpdateStatusText($"Saved frame '{framePath}' ({frameNr+1}/{totalFrames})");
            }
        }
    }

    [RelayCommand(/*CanExecute = nameof(IsRenderingFrames)*/)]
    public void StopRenderingFrames()
    {
        IsRenderingFrames = false;
        _saveFramesPath = null;
        Workspace.UpdateStatusText($"Rendering animation frames stopped.");
    }

    private async Task RunFfmpegProcess()
    {
        if (!File.Exists(Workspace.FfmpegPath))
            throw new InvalidOperationException();

        var userArgs = Workspace.FfmpegArgs ?? "";
        var frameFileExtension = Workspace.IsRawFrameExportEnabled ? "exr" : "png";
        var videoFileExtension =
            userArgs.Contains("prores") ? "mov" :
            userArgs.Contains("libvpx-vp9") ? "webm" : 
            "mp4";//figure out extension based on codec
        StringBuilder argsBuilder = new();
        argsBuilder.AppendJoin(' ',
            $"-y",//overwrite
            $"-nostdin",//disable inout
            $"-r {Workspace.Ifs.Dopesheet.Fps}",//fps
            $"-i \"{_saveFramesPath}\\{Workspace.Ifs.Title}-%06d.{frameFileExtension}\"",//input frames
            _audioPlayer?.Source is not null ? $"-i {_audioPlayer.Source.AbsolutePath}" : string.Empty,//input audio
            $"-t {Workspace.Ifs.Dopesheet.Length.TotalSeconds}",//restrict video length so audio is cut off if it's longer than the animation
            $"-vf \"pad=ceil(iw/2)*2:ceil(ih/2)*2\"",//divisible by 2, as required by some codecs
            userArgs,
            $"{Workspace.Ifs.Title}.{videoFileExtension}");
        var args = argsBuilder.ToString();

        var ffmpegProc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Workspace.FfmpegPath, args)
        {
            WorkingDirectory = _saveFramesPath
        });
        if(ffmpegProc is null)
        {
            Workspace.UpdateStatusText("Video file creation failed - unable to run ffmpeg.");
            return;
        }
        Workspace.UpdateStatusText("Generating video...");
        await ffmpegProc.WaitForExitAsync();

        Workspace.UpdateStatusText($"Video file created successfully at '{_saveFramesPath}'.");
        IsRenderingFrames = false;
        _saveFramesPath = null;
    }

}
