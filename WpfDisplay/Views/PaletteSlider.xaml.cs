using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

using IFSEngine.Model;

namespace WpfDisplay.Views;

/// <summary>
/// Interaction logic for PaletteSlider.xaml
/// </summary>
public partial class PaletteSlider : UserControl
{
    public PaletteSlider()
    {
        InitializeComponent();
    }

    public double ColorIndex
    {
        get => (double)GetValue(ColorIndexProperty);
        set => SetValue(ColorIndexProperty, value);
    }
    public static readonly DependencyProperty ColorIndexProperty =
        DependencyProperty.Register("ColorIndex", typeof(double), typeof(PaletteSlider), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public ColorPalette Palette
    {
        get => (ColorPalette)GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }
    public static readonly DependencyProperty PaletteProperty =
        DependencyProperty.Register("Palette", typeof(ColorPalette), typeof(PaletteSlider),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnPalettePropertyChanged)));
    private static void OnPalettePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue != null)
            ((PaletteSlider)sender).SetGradient((ColorPalette)e.NewValue);
    }

    private void SetGradient(ColorPalette palette)
    {
        Dispatcher.Invoke(()=>{

        gradientBrush.GradientStops = new(palette.GradientSampleBuffer.ToList().Select((c, i) => new GradientStop(
            Color.FromRgb(
                (byte)(c.X * 255),
                (byte)(c.Y * 255),
                (byte)(c.Z * 255)),
            i / (double)palette.GradientSampleBuffer.Count)));

        //TODO: dispatch?
        //gradientStops.Clear();
        //for (int i = 0; i < colors.Count; i++)
        //{
        //    gradientStops.Add(new GradientStop(
        //        Color.FromRgb(
        //            (byte)(colors[i].X * 255),
        //            (byte)(colors[i].Y * 255),
        //            (byte)(colors[i].Z * 255)),
        //        i / (double)colors.Count));
        //}
        UpdateThumbColor();
        });
    }

    private void UpdateThumbColor()
    {
        var track = slider.Template.FindName("PART_Track", slider) as Track;
        var color = Palette.SampleGradient((float)ColorIndex);
        track.Resources["brush"] = new SolidColorBrush(Color.FromRgb(
            (byte)(color.X * 255),
            (byte)(color.Y * 255),
            (byte)(color.Z * 255)));
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateThumbColor();
}
