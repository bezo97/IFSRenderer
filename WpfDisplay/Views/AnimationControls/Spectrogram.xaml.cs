using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfDisplay.ViewModels;
using WpfDisplay.Helper;

namespace WpfDisplay.Views.AnimationControls;

/// <summary>
/// Interaction logic for Spectrogram.xaml
/// </summary>
public partial class Spectrogram : UserControl
{
    private AnimationViewModel vm => ((MainViewModel)Application.Current.MainWindow.DataContext).AnimationViewModel;//ugh
    public Spectrogram()
    {
        InitializeComponent();

        DataContextChanged += (s, e) =>
        {
            vm.PropertyChanged += (ss, ee) =>
            {
                if (ee.PropertyName == nameof(vm.LoadedAudioClip))
                {
                    var sampler = (double t) => CavernHelper.CavernSampler(vm.LoadedAudioClip, vm.AudioClipCache, t);
                    DrawSpectrogram(sampler, 50.0);
                }
            };
        };

    }

    public void DrawSpectrogram(Func<double, float[]> sampler, double viewScale)
    {
        grid.Width = viewScale * vm.LoadedAudioClip!.Length;

        int wres = (int)(10 * vm.LoadedAudioClip!.Length);
        byte[] pxs = new byte[wres * 256];
        for (int t = 0; t < wres; t++)
        {
            var samples = sampler(t / (double)wres * vm.LoadedAudioClip!.Length);
            samples = samples.Skip(samples.Length / 2).ToArray();
            for (int f = 0; f < samples.Length; f++)
            {
                var s = samples[f];
                var c = (byte)(255*Math.Clamp(s,0,1));
                pxs[t + f * wres] = c;
            }
        }
        var bmp = BitmapSource.Create(wres, 256, 0, 0, PixelFormats.Indexed8, BitmapPalettes.Gray256, pxs, wres);
        bmp.Freeze();
        brush.Dispatcher.Invoke(() =>
        {
            brush.Source = bmp;
        });
    }

    //public void DrawSpectrogram(Func<double, float[]> sampler, double viewScale)
    //{
    //    double bars_resolution = 1.0 / viewScale * 10.0;
    //    //const double bars_offset = bars_resolution * 50.0/*view scale*/;
    //    var wb = new WriteableBitmap(1000,512,72,72, PixelFormats.Gray8, BitmapPalettes.Gray256);
    //    byte[,] pixels = new byte[1000,512];
    //    //for (double t = 0.0; t < vm.LoadedAudioClip!.Length; t += bars_resolution)
    //    for (int t = 0; t < 1000; t++)
    //    {
    //        var samples = sampler(t/1000.0 * vm.LoadedAudioClip!.Length);
    //        for (int f = 0; f < samples.Length; f++)
    //        {
    //            var s = samples[f];
    //            var c = (byte)(t % 255);//(byte)(128+((t+f)/(1000.0*512.0)) * 128);//(byte)(s * 255);
    //            //pixels[t,f] = c;
    //        }
    //    }
    //    int stride = (wb.PixelWidth * wb.Format.BitsPerPixel + 7) / 8;
    //    wb.WritePixels(new Int32Rect(0, 0, 1000, 512), pixels, stride, 0);
    //    wb.Freeze();
    //    brush.Dispatcher.Invoke(() =>
    //    {
    //        brush.Source = wb;
    //    });
    //}

    //public void DrawSpectrogram(Func<double, float[]> sampler, double viewScale)
    //{
    //    double bars_resolution = 1.0/viewScale * 10.0;
    //    //const double bars_offset = bars_resolution * 50.0/*view scale*/;
    //    var g = new DrawingGroup();
    //    for (double t = 0.0; t < vm.LoadedAudioClip!.Length; t += bars_resolution)
    //    {
    //        var samples = sampler(t);
    //        for (int f = 0; f < samples.Length; f++)
    //        {
    //            var s = samples[f];
    //            var c = (byte)(s * 255);
    //            var d = new GeometryDrawing
    //            {
    //                Brush = new SolidColorBrush(Color.FromRgb(c, c, c)),
    //                Geometry = new RectangleGeometry(new System.Windows.Rect(t * 10.0, f * 10.0, 10.0, 10.0))
    //            };
    //            g.Children.Add(d);
    //        }
    //    }
    //    var spectrogramImage = new DrawingImage(g);
    //    spectrogramImage.Freeze();
    //    brush.Dispatcher.Invoke(() =>
    //    {
    //        brush.ImageSource = spectrogramImage;
    //    });
    //}
}
