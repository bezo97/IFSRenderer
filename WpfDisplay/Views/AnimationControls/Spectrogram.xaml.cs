using Cavern.QuickEQ;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfDisplay.Helper;
using WpfDisplay.ViewModels;

namespace WpfDisplay.Views.AnimationControls;

/// <summary>
/// Interaction logic for Spectrogram.xaml
/// </summary>
public partial class Spectrogram : UserControl
{
    private AnimationViewModel vm => ((MainViewModel)Application.Current.MainWindow.DataContext).AnimationViewModel;//ugh
    private const int DisplayStartFreq = 4;
    private const int DisplayEndFreqMax = 20000;


    public Spectrogram()
    {
        InitializeComponent();

        DataContextChanged += (s, e) =>
        {
            vm.PropertyChanged += (ss, ee) =>
            {
                if (ee.PropertyName == nameof(vm.LoadedAudioClip))
                {
                    var sampler = (double t) => CavernHelper.CavernSampler(vm.LoadedAudioClip, vm.AudioClipCache, 0/*TODO*/, t);
                    DrawSpectrogram(sampler, 50.0);
                }
            };
        };

    }

    public void DrawSpectrogram(Func<double, float[]> sampler, double viewScale)
    {
        var displayEndFreq = Math.Min(DisplayEndFreqMax, vm.LoadedAudioClip.SampleRate * 0.95) / 2.0;

        int wres = (int)(viewScale * vm.LoadedAudioClip!.Length);
        grid.Width = wres;
        int hres = CavernHelper.defaultSamplingResolution / 2;
        byte[] pxs = new byte[wres * hres];
        for (int t = 0; t < wres; t++)
        {
            var samples = sampler(t / (double)wres * vm.LoadedAudioClip!.Length);
            //convert to log scale
            samples = GraphUtils.ConvertToGraph(samples, DisplayStartFreq, displayEndFreq, vm.LoadedAudioClip.SampleRate, samples.Length/2);
            for (int f = 0; f < samples.Length; f++)
            {
                var s = Math.Pow(samples[f], 0.1);
                var c = (byte)(255*Math.Clamp(s, 0, 1));
                pxs[t + (samples.Length - 1 - f) * wres] = c;
            }
        }
        var bmp = BitmapSource.Create(wres, hres, 0, 0, PixelFormats.Indexed8, SpectrogramPalettes.Viridis, pxs, wres);
        bmp.Freeze();
        brush.Dispatcher.Invoke(() =>
        {
            brush.Source = bmp;
        });
    }


    //TODO: on click: log scale -> linear scale, SweepGenerator.ExponentialFreqs(4, endFreq, samples.Length / 2);

}
