using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfDisplay.Views;

/// <summary>
/// Interaction logic for PaletteButton.xaml
/// </summary>
public partial class PaletteButton : Button
{
    public PaletteButton()
    {
        InitializeComponent();
    }

    public List<Vector4> GradientColors
    {
        get => (List<Vector4>)GetValue(GradientColorsProperty);
        set => SetValue(GradientColorsProperty, value);
    }
    public static readonly DependencyProperty GradientColorsProperty =
        DependencyProperty.Register("GradientColors", typeof(List<Vector4>), typeof(PaletteButton),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnGradientColorsPropertyChanged)));
    private static void OnGradientColorsPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue != null)
            ((PaletteButton)sender).SetGradient((List<Vector4>)e.NewValue);
    }

    private void SetGradient(List<Vector4> colors)
    {
        gradientBrush.GradientStops = new(colors.Select((c, i) => new GradientStop(
            Color.FromRgb(
                (byte)(c.X * 255),
                (byte)(c.Y * 255),
                (byte)(c.Z * 255)),
            i / (double)colors.Count)));

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

    }


}
