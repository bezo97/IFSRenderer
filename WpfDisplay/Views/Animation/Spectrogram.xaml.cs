#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using WpfDisplay.Helper;
using WpfDisplay.Models;

namespace WpfDisplay.Views.Animation;

/// <summary>
/// Interaction logic for Spectrogram.xaml
/// </summary>
public partial class Spectrogram : UserControl
{
    private CancellationTokenSource _drawingCts = new();

    public IAudio Audio
    {
        get => (IAudio)GetValue(AudioProperty);
        set => SetValue(AudioProperty, value);
    }
    public static readonly DependencyProperty AudioProperty =
        DependencyProperty.Register("Audio", typeof(IAudio), typeof(Spectrogram), new FrameworkPropertyMetadata(null, OnSpectrogramDependencyChanged));

    public int SelectedAudioChannelId
    {
        get => (int)GetValue(SelectedAudioChannelIdProperty);
        set => SetValue(SelectedAudioChannelIdProperty, value);
    }
    public static readonly DependencyProperty SelectedAudioChannelIdProperty =
        DependencyProperty.Register("SelectedAudioChannelId", typeof(int), typeof(Spectrogram), new PropertyMetadata(0));

    public float ViewScale
    {
        get => (float)GetValue(ViewScaleProperty);
        set => SetValue(ViewScaleProperty, value);
    }
    public static readonly DependencyProperty ViewScaleProperty =
        DependencyProperty.Register("ViewScale", typeof(float), typeof(Spectrogram), new FrameworkPropertyMetadata(120.0f, OnSpectrogramDependencyChanged));

    private static void OnSpectrogramDependencyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => d.Dispatcher.InvokeAsync(((Spectrogram)d).RedrawSpectrogramVisuals);

    public Spectrogram()
    {
        InitializeComponent();
    }

    private async Task RedrawSpectrogramVisuals()
    {
        if (Audio is not null)
        {
            await _drawingCts.CancelAsync();
            _drawingCts = new CancellationTokenSource();
            var audio = Audio;
            var viewScale = ViewScale;
            var channelId = SelectedAudioChannelId;
            Width = (int)(viewScale * audio.Length);
            spectrogramImage.Source = await Task.Run(() => DrawSpectrogramImage(audio, viewScale, channelId, _drawingCts.Token)) ?? spectrogramImage.Source;
        }
    }

    private static BitmapSource? DrawSpectrogramImage(IAudio audio, float viewScale, int channelId, CancellationToken cancellationToken = default)
    {
        //TODO: propdp?
        const int displayStartFreq = 4;
        const int displayEndFreqMax = 20000;

        var displayEndFreq = Math.Min(displayEndFreqMax, audio.SampleRate * 0.95) / 2.0;

        int wres = (int)(viewScale * audio.Length);
        int hres = CavernHelper.defaultSamplingResolution / 2;
        byte[] pxs = new byte[wres * hres];
        for (int t = 0; t < wres; t++)
        {
            if (cancellationToken.IsCancellationRequested)
                return null;

            double t01 = t / (double)wres * audio.Length;
            var spectrum = audio.GetLogSpectrum(channelId, t01, displayStartFreq, displayEndFreq);
            for (int f = 0; f < spectrum.Length; f++)
            {
                if (cancellationToken.IsCancellationRequested)
                    return null;

                var s = Math.Pow(spectrum[f], 0.1);
                var c = (byte)(255 * Math.Clamp(s, 0, 1));
                pxs[t + (spectrum.Length - 1 - f) * wres] = c;
            }
        }
        var bmp = BitmapSource.Create(wres, hres, 0, 0, PixelFormats.Indexed8, SpectrogramPalettes.Viridis, pxs, wres);
        bmp.Freeze();
        return bmp;
    }

    //TODO: on click: log scale -> linear scale, SweepGenerator.ExponentialFreqs(4, endFreq, samples.Length / 2);

}
