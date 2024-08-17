using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

namespace IFSEngine.Model;

public class ColorPalette
{
    public string Name { get; set; } = "Unnamed palette";

    public Vector3 BackgroundColor { get; set; } = Vector3.Zero;

    public SortedList<double, Vector4> Gradient { get; set; } = [];

    //public class GradientNode
    //{
    //    public double Position { get; set; }
    //    public Vector4 Color { get; set; }
    //}

    public static ColorPalette Default { get; } = new ColorPalette
    {
        Name = "Default palette",
        BackgroundColor = new Vector3(0, 0, 0),
        Gradient =
        {
            [0] = new Vector4(1, 1, 1, 1) ,
            [1] = new Vector4(1, 1, 1, 1)
        }
    };

    public static ColorPalette FromFlamePalette(FlamePalette flamePalette)
    {
        ColorPalette palette = new ColorPalette
        {
            Name = flamePalette.Name,
            BackgroundColor = Vector3.Zero,
        };
        for (int i = 0; i < flamePalette.Colors.Count; i++)
        {
            var gradientIndex = (double)i / (flamePalette.Colors.Count - 1);
            palette.Gradient.Add(gradientIndex, flamePalette.Colors[i]);
        }
        return palette;
    }

    public static FlamePalette ToFlamePalette(ColorPalette colorPalette)
    {
        return new FlamePalette
        {
            Name = colorPalette.Name,
            Rotation = 0,
            //TODO: sample 400 colors from gradient
            //Colors = colorPalette.Gradient.Select(n => n.Color).ToList()
        };
    }

    //TODO: import/export pack of palettes. new and flame formats.

    public Vector4 SampleGradient(double position)
    {
        if (Gradient.Count == 0)
            return new Vector4(BackgroundColor, 1.0f);

        //TODO: rework with warp

        if (position <= Gradient.Keys[0])
            return Gradient.Values[0];
        if (position >= Gradient.Keys[^1])
            return Gradient.Values[^1];
        var i = Gradient.Keys.ToList().BinarySearch(position);
        if (i >= 0)
            return Gradient.Values[i];

        var pos1 = Gradient.Keys[i - 1];
        var pos2 = Gradient.Keys[i];
        var t = (position - pos1) / (pos2 - pos1);
        return Vector4.Lerp(Gradient.Values[i - 1], Gradient.Values[i], (float)t);

    }


}
