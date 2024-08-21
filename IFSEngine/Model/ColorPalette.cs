using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

namespace IFSEngine.Model;

public class ColorPalette
{
    public string Name { get; set; } = "Unnamed palette";

    public Vector3 BackgroundColor { get; set; } = Vector3.Zero;

    public SortedList<double, Vector4> KeyColors { get; set; } = [];

    public List<Vector4> GradientSampleBuffer { get; } = [];

    public static ColorPalette Default { get; } = new ColorPalette
    {
        Name = "Default palette",
        BackgroundColor = new Vector3(0, 0, 0),
        KeyColors =
        {
            [0] = new Vector4(1, 1, 1, 1) ,
            [1] = new Vector4(1, 1, 1, 1)
        }
    };

    //public static ColorPalette FromFlamePalette(FlamePalette flamePalette)
    //{
    //    ColorPalette palette = new ColorPalette
    //    {
    //        Name = flamePalette.Name,
    //        BackgroundColor = Vector3.Zero,
    //    };
    //    for (int i = 0; i < flamePalette.Colors.Count; i++)
    //    {
    //        var gradientIndex = (double)i / (flamePalette.Colors.Count - 1);
    //        palette.KeyColors.Add(gradientIndex, flamePalette.Colors[i]);
    //    }
    //    return palette;
    //}

    //public static FlamePalette ToFlamePalette(ColorPalette colorPalette)
    //{
    //    return new FlamePalette
    //    {
    //        Name = colorPalette.Name,
    //        Rotation = 0,
    //        //TODO: sample 400 colors from gradient
    //        //Colors = colorPalette.Gradient.Select(n => n.Color).ToList()
    //    };
    //}

    public void ComputeGradientSamples(int resolution)
    {
        GradientSampleBuffer.Clear();
        for (int i = 0; i < resolution; i++)
        {
            var position = (double)i / (resolution - 1);
            GradientSampleBuffer.Add(SampleGradient(position));
        }
    }

    //TODO: import/export pack of palettes. new and flame formats.

    public Vector4 SampleGradient(double position)
    {
        if (KeyColors.Count == 0)
            return new Vector4(BackgroundColor, 1.0f);

        //TODO: rework with warp

        if (position <= KeyColors.Keys[0])
            return KeyColors.Values[0];
        if (position >= KeyColors.Keys[^1])
            return KeyColors.Values[^1];
        var i = KeyColors.Keys.ToList().BinarySearch(position);
        if(i < 0)
            i = ~i;//no exact match -> get the closest match using bitwise complement

        var pos1 = KeyColors.Keys[i - 1];
        var pos2 = KeyColors.Keys[i];
        var t = (position - pos1) / (pos2 - pos1);
        return Vector4.Lerp(KeyColors.Values[i - 1], KeyColors.Values[i], (float)t);

    }


}
