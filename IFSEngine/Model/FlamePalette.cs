using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using IFSEngine.Utility;

namespace IFSEngine.Model;

/// <summary>
/// Traditional flame fractal color palette.
/// Consists of a gradient that is defined as an array of colors (400 or 512).
/// File formats: *.ugr / *.gradient
/// </summary>
public partial class FlamePalette
{
    public string Name { get; set; } = "Empty Palette";
    public int Rotation { get; set; } = 0;
    public List<Vector4> Colors { get; set; } = [];

    public static FlamePalette Default { get; } = new FlamePalette
    {
        Name = "Default Palette",
        Rotation = 0,
        Colors =
        [
            new Vector4(1, 1, 1, 1),
            new Vector4(1, 1, 1, 1)
        ]
    };

    [GeneratedRegex("{[^{]+}")]
    private static partial Regex paletteMatcher();

    [GeneratedRegex("title=\"([^\"]*)\"\\s")]
    private static partial Regex titleMatcher();

    [GeneratedRegex("rotation=(-?\\d*)\\s")]
    private static partial Regex rotationMatcher();

    [GeneratedRegex("index=(-?\\d+)\\s+color=(\\d+)\\s")]
    private static partial Regex colorIndexMatcher();

    /// <summary>
    /// Parse *.gradient, *.ugr files. Compatible with ChaosHelper, UltraFractal.
    /// </summary>
    public static async Task<List<FlamePalette>> FromFileAsync(string filePath)
    {
        int maxIndices = 400;
        List<FlamePalette> palettes = [];
        string content = await File.ReadAllTextAsync(filePath);
        //find palettes in file
        var paletteTexts = paletteMatcher().Matches(content).Select(m => m.Value).ToList();
        foreach (var paletteText in paletteTexts)
        {
            //parse palette data
            var title = titleMatcher().Match(paletteText).Groups[1].Value;//title="some title"
            int rotation = 0;
            var rotationMatch = rotationMatcher().Match(paletteText);//rotation=-42
            if (rotationMatch.Success)
                rotation = int.Parse(rotationMatch.Groups[1].Value);
            var indexedColors = colorIndexMatcher().Matches(paletteText)//index=-5 color=235434
                .Select(m =>
                {
                    var bytes = BitConverter.GetBytes(int.Parse(m.Groups[2].Value));
                    return (
                     index: int.Parse(m.Groups[1].Value),
                     color: new Vector4(bytes[0] / 255.0f, bytes[1] / 255.0f, bytes[2] / 255.0f, 1.0f));
                }).ToList();

            if (rotation == 0 && indexedColors.Any(ic => ic.index > 400))
                maxIndices = 512;//Hack: some palette editors such as ChaosHelper export with 512 indices, not 400.

            indexedColors = indexedColors.ConvertAll(
                    ic => (index: (ic.index + maxIndices) % maxIndices, ic.color))
                .OrderBy(ic => ic.index)//must be sorted in case indices are rotated around
                .ToList();

            var colors = new Vector4[maxIndices];
            foreach (var (index, color) in indexedColors)
                colors[index] = color;
            //lame linear interpolation in rgb space
            for (int i = 0; i < indexedColors.Count - 1; i++)
            {
                int jStart = indexedColors[i].index + 1;
                int jEnd = indexedColors[i + 1].index;
                for (int j = jStart; j < jEnd; j++)
                {
                    float t = (j - jStart) / (float)(jEnd - jStart);
                    colors[j] = Vector4.Lerp(indexedColors[i].color, indexedColors[i + 1].color, t);
                }
            }
            float edgeIndicesDistance = maxIndices - indexedColors[^1].index + indexedColors[0].index;
            for (int i = 0; i < indexedColors[0].index; i++)
            {
                float t = (maxIndices - indexedColors[^1].index + i) / edgeIndicesDistance;
                colors[i] = Vector4.Lerp(indexedColors[^1].color, indexedColors[0].color, t);
            }
            for (int i = indexedColors[^1].index; i < maxIndices; i++)
            {
                float t = (i - indexedColors[^1].index) / edgeIndicesDistance;
                colors[i] = Vector4.Lerp(indexedColors[^1].color, indexedColors[0].color, t);
            }

            palettes.Add(new FlamePalette
            {
                Name = title,
                Rotation = rotation,
                Colors = colors.ToList()
            });
        }

        return palettes;
    }

    public Vector4 GetColorLerp(float colorIndex)
    {
        float palettepos = colorIndex * (Colors.Count - 1);
        int index = (int)Math.Floor(palettepos);
        Vector4 c1 = Colors[index];
        if (index + 1 == Colors.Count)
            return c1;
        Vector4 c2 = Colors[index + 1];
        float a = palettepos % 1;
        return MathExtensions.Lerp(c1, c2, a);
    }

}
