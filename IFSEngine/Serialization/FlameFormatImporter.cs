using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using IFSEngine.Model;

namespace IFSEngine.Serialization;

/// <summary>
/// Imports palette collections from flame format files (*.gradient / *.ugr).
/// Extracted from the original FlamePalette parsing logic.
/// </summary>
public static partial class FlameFormatImporter
{
    [GeneratedRegex(@"\{[^{]+\}")]
    private static partial Regex PaletteMatcher();

    [GeneratedRegex(@"title=""([^""]*)""\s")]
    private static partial Regex TitleMatcher();

    [GeneratedRegex(@"rotation=(-?\d*)\s")]
    private static partial Regex RotationMatcher();

    [GeneratedRegex(@"index=(-?\d+)\s+color=(\d+)\s")]
    private static partial Regex ColorIndexMatcher();

    /// <summary>
    /// Parse *.gradient / *.ugr files. Compatible with Flam3, ChaosPro, UltraFractal.
    /// </summary>
    /// <param name="filePath">Path to the flame palette file.</param>
    /// <returns>A new PaletteCollection containing all parsed palettes.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the file contains no parseable palettes.</exception>
    public static async Task<PaletteCollection> ImportFromFileAsync(string filePath)
    {
        string content = await File.ReadAllTextAsync(filePath);
        var palettes = ParsePalettesFromContent(content);

        if (palettes.Count == 0)
            throw new InvalidOperationException("The file contains no parseable palettes.");

        string collectionName = System.IO.Path.GetFileNameWithoutExtension(filePath);
        if (string.IsNullOrWhiteSpace(collectionName))
            collectionName = "Imported collection";

        return new PaletteCollection
        {
            Name = collectionName,
            Description = "",
            Author = Author.Unknown,
            Palettes = palettes
        };
    }

    /// <summary>
    /// Parse palettes from raw flame format content string.
    /// </summary>
    public static List<ColorPalette> ParsePalettesFromContent(string content)
    {
        const int defaultMaxIndices = 400;
        var palettes = new List<ColorPalette>();
        var paletteTexts = PaletteMatcher().Matches(content).Select(m => m.Value).ToList();

        foreach (var paletteText in paletteTexts)
        {
            var title = TitleMatcher().Match(paletteText).Groups[1].Value;

            int rotation = 0;
            var rotationMatch = RotationMatcher().Match(paletteText);
            if (rotationMatch.Success)
                rotation = int.Parse(rotationMatch.Groups[1].Value);

            var indexedColors = ColorIndexMatcher().Matches(paletteText)
                .Select(m =>
                {
                    var bytes = BitConverter.GetBytes(int.Parse(m.Groups[2].Value));
                    return (
                        index: int.Parse(m.Groups[1].Value),
                        color: new Vector4(bytes[0] / 255.0f, bytes[1] / 255.0f, bytes[2] / 255.0f, 1.0f));
                }).ToList();

            if (indexedColors.Count == 0)
                continue;

            int maxIndices = defaultMaxIndices;
            if (rotation == 0 && indexedColors.Any(ic => ic.index > 400))
                maxIndices = 512; // Some palette editors export with 512 indices

            indexedColors = indexedColors
                .ConvertAll(ic => (index: (ic.index + maxIndices) % maxIndices, ic.color))
                .OrderBy(ic => ic.index)
                .ToList();

            // Use explicitly defined color indices as gradient keys
            var palette = new ColorPalette
            {
                Name = string.IsNullOrWhiteSpace(title) ? "Imported palette" : title,
                InterpolationMode = InterpolationMode.LinearRGB,
                BackgroundColor = null,
                KeyColors = []
            };

            foreach (var (index, color) in indexedColors)
            {
                double position = (double)index / (maxIndices - 1);
                // Round to 2 decimal places to avoid floating point issues
                position = Math.Round(position, 2, MidpointRounding.AwayFromZero);
                palette.KeyColors[position] = color;
            }

            // Ensure we have at least 2 keys
            if (palette.KeyColors.Count < 2)
            {
                if (!palette.KeyColors.ContainsKey(0.0))
                    palette.KeyColors[0.0] = indexedColors[0].color;
                if (!palette.KeyColors.ContainsKey(1.0))
                    palette.KeyColors[1.0] = indexedColors[^1].color;
            }

            palettes.Add(palette);
        }

        return palettes;
    }
}
