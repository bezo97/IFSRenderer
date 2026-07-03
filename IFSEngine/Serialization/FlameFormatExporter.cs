using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using IFSEngine.Model;

namespace IFSEngine.Serialization;

/// <summary>
/// Exports palette collections to flame format files (*.gradient / *.ugr).
/// </summary>
public static class FlameFormatExporter
{
    private const int SampleCount = 400;

    /// <summary>
    /// Export a palette collection as a flame format file.
    /// </summary>
    /// <param name="collection">The collection to export.</param>
    /// <param name="filePath">Output file path.</param>
    /// <exception cref="InvalidOperationException">Thrown when the collection has no palettes.</exception>
    public static async Task ExportCollectionAsync(PaletteCollection collection, string filePath)
    {
        if (collection.Palettes.Count == 0)
            throw new InvalidOperationException("Cannot export an empty collection.");

        var sb = new StringBuilder();

        foreach (var palette in collection.Palettes)
        {
            sb.Append($"{{ title=\"{EscapeFlameString(palette.Name)}\" rotation=0");

            // Sample the gradient at 400 positions
            for (int i = 0; i < SampleCount; i++)
            {
                double position = (double)i / (SampleCount - 1);
                Vector4 color = palette.SampleGradient(position);

                // Convert to BGR integer (flame format stores color as BGR packed int)
                int b = (byte)(color.Z * 255);
                int g = (byte)(color.Y * 255);
                int r = (byte)(color.X * 255);
                int packedColor = r | (g << 8) | (b << 16);

                sb.Append($" index={i} color={packedColor}");
            }

            sb.Append("} ");
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// Export a single palette as a flame format file.
    /// </summary>
    public static async Task ExportPaletteAsync(ColorPalette palette, string filePath)
    {
        var collection = new PaletteCollection
        {
            Name = palette.Name,
            Palettes = [palette]
        };
        await ExportCollectionAsync(collection, filePath);
    }

    private static string EscapeFlameString(string input)
    {
        return input.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
