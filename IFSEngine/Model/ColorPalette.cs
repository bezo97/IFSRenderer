using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace IFSEngine.Model;

public class ColorPalette
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Unnamed palette";
    /// <summary>
    /// Background color associated with this palette (RGB). 
    /// <c>null</c> means fully transparent background.
    /// </summary>
    public Vector3? BackgroundColor { get; set; } = null;
    public SortedList<double, Vector4> KeyColors { get; set; } = [];
    public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.LinearRGB;
    /// <summary>
    /// Author information, using the existing <see cref="Author"/> class.
    /// </summary>
    public Author Author { get; set; }
    public List<string> Tags { get; set; } = [];

    public static ColorPalette Default { get; } = new ColorPalette
    {
        Name = "Default palette",
        BackgroundColor = new Vector3(0, 0, 0),
        InterpolationMode = InterpolationMode.LinearRGB,
        KeyColors =
        {
            [0.0] = new Vector4(1, 1, 1, 1),
            [1.0] = new Vector4(1, 1, 1, 1)
        }
    };

    /// <summary>
    /// Sample the gradient at the given position (0.0–1.0) using the palette's interpolation mode.
    /// </summary>
    public Vector4 SampleGradient(double position)
    {
        if (KeyColors.Count == 0)
            return new Vector4(BackgroundColor ?? Vector3.Zero, 1.0f);

        if (position <= KeyColors.Keys[0])
            return KeyColors.Values[0];
        if (position >= KeyColors.Keys[^1])
            return KeyColors.Values[^1];

        int i = Array.BinarySearch(KeyColors.Keys.ToArray(), position);
        if (i < 0)
            i = ~i; // no exact match -> get the closest match using bitwise complement

        var pos1 = KeyColors.Keys[i - 1];
        var pos2 = KeyColors.Keys[i];
        var t = (float)((position - pos1) / (pos2 - pos1));

        var c1 = KeyColors.Values[i - 1];
        var c2 = KeyColors.Values[i];

        return InterpolationMode switch
        {
            InterpolationMode.LinearRGB => Vector4.Lerp(c1, c2, t),
            InterpolationMode.Srgb => SrgbLerp(c1, c2, t),
            InterpolationMode.Mixbox => MixboxLerp(c1, c2, t),
            _ => Vector4.Lerp(c1, c2, t)
        };
    }

    /// <summary>
    /// Decode sRGB to linear, interpolate, re-encode to sRGB.
    /// </summary>
    private static Vector4 SrgbLerp(Vector4 c1, Vector4 c2, float t)
    {
        var l1 = SrgbToLinear(c1);
        var l2 = SrgbToLinear(c2);
        var lerp = Vector4.Lerp(l1, l2, t);
        return LinearToSrgb(lerp);
    }

    /// <summary>
    /// sRGB to linear (piecewise approximation of the sRGB gamma curve).
    /// </summary>
    private static Vector4 SrgbToLinear(Vector4 c)
    {
        return new Vector4(
            SrgbChannelToLinear(c.X),
            SrgbChannelToLinear(c.Y),
            SrgbChannelToLinear(c.Z),
            c.W);
    }

    private static float SrgbChannelToLinear(float c)
    {
        return c <= 0.04045f ? c / 12.92f : (float)Math.Pow((c + 0.055f) / 1.055f, 2.4);
    }

    /// <summary>
    /// Linear to sRGB.
    /// </summary>
    private static Vector4 LinearToSrgb(Vector4 c)
    {
        return new Vector4(
            LinearChannelToSrgb(c.X),
            LinearChannelToSrgb(c.Y),
            LinearChannelToSrgb(c.Z),
            c.W);
    }

    private static float LinearChannelToSrgb(float c)
    {
        return c <= 0.0031308f ? c * 12.92f : 1.055f * (float)Math.Pow(c, 1.0 / 2.4) - 0.055f;
    }

    /// <summary>
    /// Pigment-based mixing using the Mixbox library.
    /// </summary>
    private static Vector4 MixboxLerp(Vector4 c1, Vector4 c2, float t)
    {
        var src = new[] { c1.X, c1.Y, c1.Z, c1.W };
        var dst = new[] { c2.X, c2.Y, c2.Z, c2.W };
        Scrtwpns.Mixbox.Mixbox.LerpFloat(src, dst, t);
        return new Vector4(src[0], src[1], src[2], 1.0f);
    }
}
