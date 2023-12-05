using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfDisplay.Helper;
using WpfDisplay.Models;

namespace WpfDisplay.Views.AnimationControls;

/// <summary>
/// Interaction logic for Spectrogram.xaml
/// </summary>
public partial class Spectrogram : UserControl
{
    public IAudio Audio
    {
        get { return (IAudio)GetValue(AudioProperty); }
        set { SetValue(AudioProperty, value); }
    }
    public static readonly DependencyProperty AudioProperty =
        DependencyProperty.Register("Audio", typeof(IAudio), typeof(Spectrogram), new FrameworkPropertyMetadata(null, OnSpectrogramDependencyChanged));

    public int SelectedAudioChannelId
    {
        get { return (int)GetValue(SelectedAudioChannelIdProperty); }
        set { SetValue(SelectedAudioChannelIdProperty, value); }
    }
    public static readonly DependencyProperty SelectedAudioChannelIdProperty =
        DependencyProperty.Register("SelectedAudioChannelId", typeof(int), typeof(Spectrogram), new PropertyMetadata(0));

    public float ViewScale
    {
        get { return (float)GetValue(ViewScaleProperty); }
        set { SetValue(ViewScaleProperty, value); }
    }
    public static readonly DependencyProperty ViewScaleProperty =
        DependencyProperty.Register("ViewScale", typeof(float), typeof(Spectrogram), new FrameworkPropertyMetadata(50.0f, OnSpectrogramDependencyChanged));

    private static void OnSpectrogramDependencyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((Spectrogram)d).DrawSpectrogram();

    public Spectrogram()
    {
        InitializeComponent();
    }

    private void DrawSpectrogram()
    {
        if (Audio is null)
            return;

        //TODO: propdp?
        const int displayStartFreq = 4;
        const int displayEndFreqMax = 20000;

        var displayEndFreq = Math.Min(displayEndFreqMax, Audio.SampleRate * 0.95) / 2.0;

        int wres = (int)(ViewScale * Audio.Length);
        int hres = CavernHelper.defaultSamplingResolution / 2;
        byte[] pxs = new byte[wres * hres];
        for (int t = 0; t < wres; t++)
        {
            double t01 = t / (double)wres * Audio.Length;
            var spectrum = Audio.GetLogSpectrum(SelectedAudioChannelId, t01, displayStartFreq, displayEndFreq);
            for (int f = 0; f < spectrum.Length; f++)
            {
                var s = Math.Pow(spectrum[f], 0.1);
                var c = (byte)(255 * Math.Clamp(s, 0, 1));
                pxs[t + (spectrum.Length - 1 - f) * wres] = c;
            }
        }
        var bmp = BitmapSource.Create(wres, hres, 0, 0, PixelFormats.Indexed8, SpectrogramPalettes.Viridis, pxs, wres);
        bmp.Freeze();
        spectrogramImage.Source = bmp;
        Width = bmp.Width;
    }

    //TODO: on click: log scale -> linear scale, SweepGenerator.ExponentialFreqs(4, endFreq, samples.Length / 2);

}
