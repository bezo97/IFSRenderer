using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;

using IFSEngine.Model;

namespace WpfDisplay.ViewModels;

public partial class ColorPaletteViewModel : ObservableObject
{
    public Color BackgroundColor => Color.FromRgb(
        (byte)(Palette.BackgroundColor.X * 255),
        (byte)(Palette.BackgroundColor.Y * 255),
        (byte)(Palette.BackgroundColor.Z * 255));

    public GradientStopCollection GradientStops => new(Palette.GradientSampleBuffer.Select((c, i) => new GradientStop(
        Color.FromRgb(
            (byte)(c.X * 255),
            (byte)(c.Y * 255),
            (byte)(c.Z * 255)),
        i / (double)Palette.GradientSampleBuffer.Count)));

    [ObservableProperty]
    private ColorPalette _palette;

    [ObservableProperty]
    private bool _isFavorite;
}
