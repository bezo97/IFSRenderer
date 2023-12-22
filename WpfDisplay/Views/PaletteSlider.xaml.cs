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

    public FlamePalette Palette
    {
        get => (FlamePalette)GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }
    public static readonly DependencyProperty PaletteProperty =
        DependencyProperty.Register("Palette", typeof(FlamePalette), typeof(PaletteSlider),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnPalettePropertyChanged)));
    private static void OnPalettePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue != null)
            ((PaletteSlider)sender).SetGradient((FlamePalette)e.NewValue);
    }

    private void SetGradient(FlamePalette fp)
    {
        gradientStops.Clear();
        for (int i = 0; i < fp.Colors.Count; i++)
        {
            gradientStops.Add(new GradientStop(
                Color.FromRgb(
                    (byte)(fp.Colors[i].X * 255),
                    (byte)(fp.Colors[i].Y * 255),
                    (byte)(fp.Colors[i].Z * 255)),
                i / (double)fp.Colors.Count));
        }
        UpdateThumbColor();
    }

    private void UpdateThumbColor()
    {
        var track = slider.Template.FindName("PART_Track", slider) as Track;
        var color = Palette.GetColorLerp((float)ColorIndex);
        track.Resources["brush"] = new SolidColorBrush(Color.FromRgb(
            (byte)(color.X * 255),
            (byte)(color.Y * 255),
            (byte)(color.Z * 255)));
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateThumbColor();
}
