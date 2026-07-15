#nullable enable

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using WpfDisplay.ViewModels;

namespace WpfDisplay.Views;

/// <summary>
/// Palette swatch control for the browser.
/// Shows a gradient preview with optional background color border.
/// </summary>
public partial class PaletteSwatch : UserControl
{
    public static readonly DependencyProperty PaletteVmProperty =
        DependencyProperty.Register(nameof(PaletteVm), typeof(ColorPaletteViewModel), typeof(PaletteSwatch),
            new PropertyMetadata(null, OnPaletteVmChanged));

    public ColorPaletteViewModel? PaletteVm
    {
        get => (ColorPaletteViewModel?)GetValue(PaletteVmProperty);
        set => SetValue(PaletteVmProperty, value);
    }

    private static void OnPaletteVmChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PaletteSwatch swatch)
            swatch.UpdateSwatch();
    }

    public PaletteSwatch()
    {
        InitializeComponent();
    }

    private void UpdateSwatch()
    {
        var vm = PaletteVm;
        if (vm == null) return;

        // Set gradient brush
        GradientBrush.GradientStops.Clear();
        foreach (var stop in vm.GradientStops)
            GradientBrush.GradientStops.Add(stop);

        // Set tooltip
        ToolTip = vm.Palette.Name;

        // Set background color border
        if (vm.BackgroundColor.HasValue)
        {
            BgBorder.BorderThickness = new Thickness(2);
            BgBorder.BorderBrush = new SolidColorBrush(vm.BackgroundColor.Value);
        }
        else
        {
            BgBorder.BorderThickness = new Thickness(0);
            BgBorder.BorderBrush = null;
        }
    }
}
