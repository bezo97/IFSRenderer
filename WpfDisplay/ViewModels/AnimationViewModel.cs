#nullable enable
using Cavern.Format;
using Cavern.QuickEQ;
using Cavern.Remapping;
using Cavern.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFSEngine.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfDisplay.Helper;
using WpfDisplay.Models;
using Clip = Cavern.Clip;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class AnimationViewModel
{
    public readonly Workspace Workspace;
    private readonly Timer _realtimePlayer;
    public HashSet<KeyframeViewModel> SelectedKeyframes { get; } = new();

    private MediaPlayer? _audioPlayer;
    public Clip? LoadedAudioClip { get; private set; } = null;
    public FFTCache? AudioClipCache { get; private set; } = null;
    [ObservableProperty] private ReferenceChannel[] _loadedAudioChannels = Array.Empty<ReferenceChannel>();
    [ObservableProperty] public string? _audioClipTitle = null;
    [ObservableProperty] private double? _keyframeInsertPosition = null;//location of the context menu over the channel
    [ObservableProperty] public bool _isRenderingFrames = false;
    private string? _saveFramesPath = null;
    private string _framesExtension = "exr";

    public TimeOnly CurrentTime { get; private set; } = TimeOnly.MinValue;

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
        workspace.Renderer.RenderingFinished += OnFrameFinishedRendering;
        _realtimePlayer = new Timer(TimeSpan.FromSeconds(1.0 / workspace.Ifs.Dopesheet.Fps).TotalMilliseconds);
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
        Increment = 1.0 / 60,
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

    public void JumpToTime(double t)
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
                if (vm.Keyframes.Count == 1)
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

        if (kfv._cvm.Keyframes.Count == 1)
        {//remove channel when last keyframe is removed
            Workspace.Ifs.Dopesheet.RemoveChannel(kfv._cvm.Path, CurrentTime);
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
    public void RemoveChannel(ChannelViewModel cvm)
    {
        Workspace.TakeSnapshot();
        Workspace.Ifs.Dopesheet.RemoveChannel(cvm.Path, CurrentTime);
        Channels.Remove(cvm);
        Workspace.Renderer.InvalidateParamsBuffer();
        Workspace.RaiseAnimationFrameChanged();
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
            //kf.IsSelected = false;
        }
        Workspace.Renderer.InvalidateParamsBuffer();
        //SelectedKeyframes.Clear();
    }

    private void OnPlayerTick(object? sender, ElapsedEventArgs e)
    {
        JumpToNextFrame();
    }

    [RelayCommand]
    public async Task LoadAudio()
    {
        if (DialogHelper.ShowOpenSoundDialog(out string path))
        {
            _audioPlayer = new MediaPlayer();
            _audioPlayer.Open(new Uri(path));
            await Task.Run(() =>
            {
                //TODO: show loading dialog with cancel
                var r = new RIFFWaveReader(path);
                LoadedAudioClip = r.ReadClip();
                AudioClipCache = new FFTCache(CavernHelper.defaultSamplingResolution);
                LoadedAudioChannels = ChannelPrototype.GetStandardMatrix(LoadedAudioClip.Channels);
                AudioClipTitle = LoadedAudioClip.Name ?? System.IO.Path.GetFileNameWithoutExtension(path);

                AudioBarsDrawing = CreateAudioBarsDrawing(LoadedAudioClip, 50.0/*viewScale*/);
                var sampler = (double t) => CavernHelper.CavernSampler(LoadedAudioClip, AudioClipCache, 0/*TODO*/, t);
                SpectrogramBitmap = DrawSpectrogram(LoadedAudioClip, sampler, 50.0/*viewScale*/);

                OnPropertyChanged(nameof(LoadedAudioClip));
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
            float[] samples = new float[512];//2 hatványa!
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

    [ObservableProperty] private BitmapSource _spectrogramBitmap = default!;
    private static BitmapSource DrawSpectrogram(Clip audioClip, Func<double, float[]> sampler, double viewScale)
    {
        const int displayStartFreq = 4;
        const int displayEndFreqMax = 20000;

        var displayEndFreq = Math.Min(displayEndFreqMax, audioClip.SampleRate * 0.95) / 2.0;

        int wres = (int)(viewScale * audioClip.Length);
        int hres = CavernHelper.defaultSamplingResolution / 2;
        byte[] pxs = new byte[wres * hres];
        for (int t = 0; t < wres; t++)
        {
            var samples = sampler(t / (double)wres * audioClip.Length);
            //convert to log scale
            samples = GraphUtils.ConvertToGraph(samples, displayStartFreq, displayEndFreq, audioClip.SampleRate, samples.Length / 2);
            for (int f = 0; f < samples.Length; f++)
            {
                var s = Math.Pow(samples[f], 0.1);
                var c = (byte)(255 * Math.Clamp(s, 0, 1));
                pxs[t + (samples.Length - 1 - f) * wres] = c;
            }
        }
        var bmp = BitmapSource.Create(wres, hres, 0, 0, PixelFormats.Indexed8, SpectrogramPalettes.Viridis, pxs, wres);
        bmp.Freeze();
        return bmp;
    }

    private bool _canExecuteStart => !IsRenderingFrames;
    [RelayCommand(/*CanExecute = nameof(_canExecuteStart)*/)]
    public async Task StartRenderingFrames()
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
        Workspace.UpdateStatusText($"Rendering animation frames...");
    }

    private void OnFrameFinishedRendering(object? sender, EventArgs e)
    {
        if(IsRenderingFrames && _saveFramesPath is not null)
        {
            var frameNr = (int)(CurrentTime.ToTimeSpan().TotalSeconds * Workspace.Ifs.Dopesheet.Fps);
            var framePath = Path.Combine(_saveFramesPath, $"{Workspace.Ifs.Title}-{frameNr:D6}.{_framesExtension}"); //TODO: filter invalid chars

            if (_framesExtension == "png")
            {
                var bitmap = Workspace.Renderer.GetExportBitmapSource(false).Result;
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmap));
                using var fstream = File.Create(framePath);
                enc.Save(fstream);
            }
            else if(_framesExtension == "exr")
            {
                var histogramData = Workspace.Renderer.ReadHistogramData().Result;
                using var fstream = File.Create(framePath);
                OpenEXR.WriteStream(fstream, histogramData);
            }
            else throw new NotSupportedException();

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
                Workspace.Renderer.StartRenderLoop();
            }
        }
    }

    [RelayCommand(/*CanExecute = nameof(IsRenderingFrames)*/)]
    public async Task StopRenderingFrames()
    {
        IsRenderingFrames = false;
        _saveFramesPath = null;
        if(Workspace.Renderer.IsRendering)
            await Workspace.Renderer.StopRenderLoop();
        Workspace.UpdateStatusText($"Rendering animation frames stopped.");
    }

    private async Task RunFfmpegProcess()
    {
        var videoExtension = _framesExtension switch
        {
            "png" => "mp4", 
            "exr" => "mov", 
            _ => throw new NotSupportedException(), 
        };
        StringBuilder argsBuilder = new();
        argsBuilder.AppendJoin(' ',
            $"-y",//overwrite
            $"-nostdin",//disable inout
            $"-r {Workspace.Ifs.Dopesheet.Fps}",//fps
            $"-i \"{_saveFramesPath}\\{Workspace.Ifs.Title}-%06d.{_framesExtension}\"",//input files
            $"-vf \"pad=ceil(iw/2)*2:ceil(ih/2)*2\"",//divisible by 2, as required by some codecs
            Workspace.FfmpegArgs,
            $"{Workspace.Ifs.Title}.{videoExtension}");//user args
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
