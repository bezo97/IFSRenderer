using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;

using IFSEngine.Model;

namespace WpfDisplay.ViewModels;

public partial class ColorPaletteViewModel : ObservableObject
{
    private const int PreviewSampleCount = 256;

    public Color? BackgroundColor
    {
        get
        {
            if (!Palette.BackgroundColor.HasValue)
                return null;
            var bg = Palette.BackgroundColor.Value;
            return Color.FromRgb(
                (byte)(bg.X * 255),
                (byte)(bg.Y * 255),
                (byte)(bg.Z * 255));
        }
    }

    public GradientStopCollection GradientStops
    {
        get
        {
            var stops = new List<GradientStop>();
            for (int i = 0; i < PreviewSampleCount; i++)
            {
                double position = i / (double)(PreviewSampleCount - 1);
                Vector4 color = Palette.SampleGradient(position);
                stops.Add(new GradientStop(
                    Color.FromRgb(
                        (byte)(color.X * 255),
                        (byte)(color.Y * 255),
                        (byte)(color.Z * 255)),
                    position));
            }
            return new GradientStopCollection(stops);
        }
    }

    [ObservableProperty]
    private ColorPalette _palette;

    [ObservableProperty]
    private bool _isFavorite;
}
