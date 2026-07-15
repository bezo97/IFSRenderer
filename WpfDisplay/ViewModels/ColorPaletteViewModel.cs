using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;

using IFSEngine.Model;

namespace WpfDisplay.ViewModels;

/// <summary>
/// Viewmodel wrapping a <see cref="ColorPalette"/> for palette browser display.
/// Provides gradient stops for swatch rendering, background color, and favorite state.
/// </summary>
public partial class ColorPaletteViewModel : ObservableObject
{
    public ColorPalette Palette { get; }
    public GradientStopCollection GradientStops { get; }

    [ObservableProperty]
    public partial bool IsFavorite { get; set; }

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

    /// <summary>
    /// Average hue angle of the palette (0-360). Used for hue sorting.
    /// </summary>
    public double DominantHue { get; }

    /// <summary>
    /// Warmth score (0-1, higher = warmer). Used for warmth sorting.
    /// </summary>
    public double Warmth { get; }

    /// <summary>
    /// Average brightness (0-1). Used for brightness sorting.
    /// </summary>
    public double Brightness { get; }

    /// <summary>
    /// Average saturation (0-1). Used for saturation sorting.
    /// </summary>
    public double Saturation { get; }

    public int KeyCount => Palette.KeyColors.Count;

    public ColorPaletteViewModel() { }
    public ColorPaletteViewModel(ColorPalette palette)
    {
        Palette = palette;
        (DominantHue, Warmth, Brightness, Saturation) = ComputeColorMetrics(palette);
        GradientStops = ComputeGradientStops(palette);
    }

    private static GradientStopCollection ComputeGradientStops(ColorPalette palette)
    {
        // use existing keys, insert midpoint sample only where
        // adjacent keys are far apart, because of different color interpolation.
        var keys = palette.KeyColors;
        int estimatedCount = keys.Count * 2;
        var stops = new List<GradientStop>(estimatedCount);

        for (int i = 0; i < keys.Count; i++)
        {
            double pos = keys.Keys[i];
            Vector4 color = keys.Values[i];
            stops.Add(new GradientStop(Color.FromRgb((byte)(color.X * 255), (byte)(color.Y * 255), (byte)(color.Z * 255)), pos));

            if (i < keys.Count - 1)
            {
                double nextPos = keys.Keys[i + 1];
                if (nextPos - pos > 0.1)
                {
                    double midPos = (pos + nextPos) * 0.5;
                    Vector4 midColor = palette.SampleGradient(midPos);
                    stops.Add(new GradientStop(Color.FromRgb((byte)(midColor.X * 255), (byte)(midColor.Y * 255), (byte)(midColor.Z * 255)), midPos));
                }
            }
        }

        return new GradientStopCollection(stops);
    }

    private static (double hue, double warmth, double brightness, double saturation) ComputeColorMetrics(ColorPalette palette)
    {
        double totalHue = 0, totalWarmth = 0, totalBrightness = 0, totalSaturation = 0;
        int count = 0;

        foreach (var color in palette.KeyColors.Values)
        {
            var (h, s, v) = RgbToHsv(color.X, color.Y, color.Z);
            totalHue += h;
            totalWarmth += ComputeWarmth(h);
            totalBrightness += v;
            totalSaturation += s;
            count++;
        }

        if (count == 0)
            return (0, 0, 0, 0);

        return (totalHue / count, totalWarmth / count, totalBrightness / count, totalSaturation / count);
    }

    private static double ComputeWarmth(double hue)
    {
        // Warm colors: red (0), orange (30), yellow (60)
        // Cool colors: cyan (180), blue (240), purple (300)
        // Warmth is highest at 0-60, lowest at 180-240
        double normalizedHue = hue / 360.0;
        return 1.0 - (Math.Abs(normalizedHue - 0.083) / 0.5); // peak at red/orange
    }

    private static (double h, double s, double v) RgbToHsv(double r, double g, double b)
    {
        double max = System.Math.Max(r, System.Math.Max(g, b));
        double min = System.Math.Min(r, System.Math.Min(g, b));
        double delta = max - min;

        double h = 0;
        if (delta != 0)
        {
            if (max == r)
                h = 60 * (((g - b) / delta) % 6);
            else if (max == g)
                h = 60 * (((b - r) / delta) + 2);
            else
                h = 60 * (((r - g) / delta) + 4);
        }

        if (h < 0) h += 360;
        double s = max == 0 ? 0 : delta / max;
        return (h, s, max);
    }
}
