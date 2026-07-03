using System.Collections.Generic;
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
    private const int GradientSampleCount = 256;

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
        Dispatcher.Invoke(() =>
        {
            var stops = new List<GradientStop>();
            for (int i = 0; i < GradientSampleCount; i++)
            {
                double position = (double)i / (GradientSampleCount - 1);
                var color = palette.SampleGradient(position);
                stops.Add(new GradientStop(
                    Color.FromRgb(
                        (byte)(color.X * 255),
                        (byte)(color.Y * 255),
                        (byte)(color.Z * 255)),
                    position));
            }
            gradientBrush.GradientStops = new GradientStopCollection(stops);
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
