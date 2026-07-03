using System;
using System.Collections.Generic;
using System.Numerics;

using IFSEngine.Model;

namespace IFSEngine.Generation;

/// <summary>
/// Generates palettes using Inigo Quilez's cosine palette technique.
/// The generator produces color keys at evenly-spaced positions from the IQ cosine function.
/// </summary>
/// <remarks>
/// Based on: <a href="https://iquilezles.org/www/articles/palettes/palettes.htm">iquilezles.org</a>
/// </remarks>
public static class IqPaletteGenerator
{
    /// <summary>
    /// Generate a palette from explicit IQ cosine parameters.
    /// </summary>
    /// <param name="bias">Center point of each channel's oscillation (0-1 per channel).</param>
    /// <param name="mult">Amplitude of each channel's oscillation (0-1 per channel).</param>
    /// <param name="freq">Frequency of each channel's oscillation (0-4 per channel).</param>
    /// <param name="phase">Phase offset of each channel's oscillation (0-1 per channel).</param>
    /// <param name="interpolationMode">The interpolation mode to assign to the palette.</param>
    /// <param name="keyCount">Number of color keys to sample (4-20).</param>
    /// <returns>A new ColorPalette with keys sampled from the IQ function.</returns>
    public static ColorPalette Generate(Vector4 bias, Vector4 mult, Vector4 freq, Vector4 phase, InterpolationMode interpolationMode, int keyCount)
    {
        keyCount = Math.Clamp(keyCount, 4, 20);

        var gradientKeys = new SortedList<double, Vector4>();

        for (int i = 0; i < keyCount; i++)
        {
            float t = (float)i / (keyCount - 1);
            Vector4 c = bias + mult * new Vector4(
                (float)Math.Cos(2 * Math.PI * (t * freq.X + phase.X)),
                (float)Math.Cos(2 * Math.PI * (t * freq.Y + phase.Y)),
                (float)Math.Cos(2 * Math.PI * (t * freq.Z + phase.Z)),
                1.0f);
            c = Vector4.Clamp(c, Vector4.Zero, Vector4.One);
            gradientKeys[t] = HsvToRgb(c);
        }

        return new ColorPalette
        {
            Name = "Generated Palette",
            KeyColors = gradientKeys,
            BackgroundColor = null,
            InterpolationMode = interpolationMode
        };
    }

    private static Vector4 HsvToRgb(Vector4 hsv)
    {
        Vector4 rgb = HueToRgb(hsv.X);
        return ((rgb - Vector4.One) * hsv.Y + Vector4.One) * hsv.Z;
    }

    private static Vector4 HueToRgb(float hue)
    {
        double R = Math.Abs(hue * 6 - 3) - 1;
        double G = 2 - Math.Abs(hue * 6 - 2);
        double B = 2 - Math.Abs(hue * 6 - 4);
        R = Math.Clamp(R, 0.0, 1.0);
        G = Math.Clamp(G, 0.0, 1.0);
        B = Math.Clamp(B, 0.0, 1.0);
        return new Vector4((float)R, (float)G, (float)B, 1.0f);
    }
}
