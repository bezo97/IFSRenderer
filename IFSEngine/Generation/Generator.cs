using IFSEngine.Model;
using IFSEngine.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace IFSEngine.Generation;

public class Generator
{
    public List<Transform> SelectedTransforms { get; set; }

    public Generator(IEnumerable<Transform> transforms)
    {
        SelectedTransforms = transforms.ToList();
    }

    public IEnumerable<IFS> GenerateBatch(GeneratorOptions options)
    {
        double max_strength = options.MutationStrength;
        for (int i = 0; i < options.BatchSize; i++)
        {
            options.MutationStrength = (double)i / options.BatchSize * max_strength;
            yield return GenerateOne(options);
        }
        options.MutationStrength = max_strength;
    }

    public IFS GenerateOne(GeneratorOptions options)
    {
        IFS gen = options.baseParams.DeepClone();
        if (options.MutateIterators)
        {
            while (gen.Iterators.Count < 4)
            {
                gen.AddIterator(newIterator(), true);
            }
            //TODO: 
            if (options.MutationChance > RandHelper.NextDouble())
            {
                gen.AddIterator(newIterator(), true);
            }
            if (gen.Iterators.Count > 4 && options.MutationChance > RandHelper.NextDouble())
            {
                var iter = gen.Iterators.ElementAt(RandHelper.Next(gen.Iterators.Count));
                gen.RemoveIterator(iter);
            }
        }
        if (options.MutateParameters)
        {
            foreach (var it in gen.Iterators)
            {
                foreach (var v in it.RealParams)
                {
                    it.RealParams[v.Key] = MutateValue(it.RealParams[v.Key], options);
                }
                foreach (var v in it.Vec3Params)
                {
                    it.Vec3Params[v.Key] = MutateVec3(it.Vec3Params[v.Key], options);
                }
            }
        }
        if (options.MutateConnections)
        {//add/remove
            foreach (var it in gen.Iterators)
            {
                foreach (var itTo in gen.Iterators)
                {
                    if (!it.WeightTo.TryGetValue(itTo, out _))
                        it.WeightTo[itTo] = 0.0;//hack
                    if (options.MutationChance > RandHelper.NextDouble())
                    {
                        it.WeightTo[itTo] = 1.0 - (it.WeightTo[itTo] > 0.0 ? 1.0 : 0.0);
                    }
                }
            }
        }
        if (options.MutateConnectionWeights)
        {
            foreach (var it in gen.Iterators)
            {
                foreach (var itTo in gen.Iterators)
                {
                    if (it.WeightTo[itTo] == 0.0)
                        continue;
                    it.WeightTo[itTo] = Math.Max(0, MutateValue(it.WeightTo[itTo], options));
                }
            }
        }
        if (options.MutatePalette)
        {
            //TODO: params
            var bias = RandomVector(0.4f, 0.8f);
            var mult = RandomVector(0.2f, 1.2f);
            var freq = RandomVector(0.1f, 1.0f);
            var phase = RandomVector(0.0f, 1.0f);
            gen.Palette = PaletteFromIqParams(bias, mult, freq, phase);
        }
        if (options.MutateColoring)
        {
            foreach (var it in gen.Iterators)
            {
                it.ColorIndex = Math.Clamp(MutateValue(it.ColorIndex, options), 0.0, 1.0);
                it.ColorSpeed = MutateValue(it.ColorSpeed, options);
            }
        }
        return gen;
    }

    private Iterator newIterator()
    {
        Iterator newIterator;
        //50% chance the new iterator is an affine
        if (RandHelper.NextDouble() < 0.5)
            newIterator = Iterator.RandomIterator(SelectedTransforms);
        else
            newIterator = Iterator.RandomIterator(SelectedTransforms.Where(t => t.Name == "Affine").ToList());
        //consider tags to use plugins the right way
        if(newIterator.Transform.Tags.Contains("shape"))
        {
            newIterator.Opacity = 0.0;
            newIterator.Add = 1.0;
            newIterator.ColorSpeed = 0.0;
        }
        return newIterator;
    }

    private static double MutateValue(double val, GeneratorOptions o)
    {
        if (o.MutationChance > RandHelper.NextDouble())
            return val + -o.MutationStrength / 2 + o.MutationStrength * RandHelper.NextDouble();
        else
            return val;
    }
    private static Vector3 MutateVec3(Vector3 val, GeneratorOptions o)
    {
        if (o.MutationChance > RandHelper.NextDouble())
        {
            Vector3 v = val;
            v.X = (float)(v.X + -o.MutationStrength / 2 + o.MutationStrength * RandHelper.NextDouble());
            v.Y = (float)(v.Y + -o.MutationStrength / 2 + o.MutationStrength * RandHelper.NextDouble());
            v.Z = (float)(v.Z + -o.MutationStrength / 2 + o.MutationStrength * RandHelper.NextDouble());
            return v;
        }
        else
            return val;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Based on this article from Inigo Quilez: <a href="https://iquilezles.org/www/articles/palettes/palettes.htm">iquilezles.org</a>
    /// </remarks>
    /// <returns></returns>
    private static FlamePalette PaletteFromIqParams(Vector4 bias, Vector4 mult, Vector4 freq, Vector4 phase)
    {
        List<Vector4> colors = new List<Vector4>();

        for (float t = 0; t < 1.0; t += 0.1f)
        {//could be based on freq
            Vector4 c = bias + mult * new Vector4(
                (float)Math.Cos(2 * Math.PI * (t * freq.X + phase.X)),
                (float)Math.Cos(2 * Math.PI * (t * freq.Y + phase.Y)),
                (float)Math.Cos(2 * Math.PI * (t * freq.Z + phase.Z)),
                1.0f);
            c = Vector4.Clamp(c, Vector4.Zero, Vector4.One);
            colors.Add(HsvToRgb(c));
        }

        return new FlamePalette
        {
            Name = "Generated Palette",
            Colors = colors
        };
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
    private static Vector4 HsvToRgb(Vector4 hsv)
    {
        Vector4 rgb = HueToRgb(hsv.X);
        return ((rgb - Vector4.One) * hsv.Y + Vector4.One) * hsv.Z;
    }

    private static Vector4 RandomVector(float min, float max)
    {
        float range = max - min;
        return new Vector4(
            min + range * (float)RandHelper.NextDouble(),
            min + range * (float)RandHelper.NextDouble(),
            min + range * (float)RandHelper.NextDouble(),
            1.0f);
    }

}
