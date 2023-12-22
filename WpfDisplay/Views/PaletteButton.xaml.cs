using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using IFSEngine.Model;

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

    public FlamePalette Palette
    {
        get => (FlamePalette)GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }
    public static readonly DependencyProperty PaletteProperty =
        DependencyProperty.Register("Palette", typeof(FlamePalette), typeof(PaletteButton),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnPalettePropertyChanged)));
    private static void OnPalettePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue != null)
            ((PaletteButton)sender).SetGradient((FlamePalette)e.NewValue);
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

    }


}
