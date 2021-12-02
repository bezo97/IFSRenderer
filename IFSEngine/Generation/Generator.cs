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

    public async IAsyncEnumerable<IFS> GenerateBatch(GeneratorOptions options, int batchSize)
    {
        double max_strength = options.MutationStrength;
        for (int i = 0; i < batchSize; i++)
        {
            options.MutationStrength = (double)i / batchSize * max_strength;
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
                gen.AddIterator(Iterator.RandomIterator(SelectedTransforms), true);
            }
            //TODO: 
            if (options.MutationChance > RandHelper.NextDouble())
            {
                gen.AddIterator(Iterator.RandomIterator(SelectedTransforms), true);
            }
            if (gen.Iterators.Count > 2 && options.MutationChance > RandHelper.NextDouble())
            {
                var iter = gen.Iterators.ElementAt(RandHelper.Next(gen.Iterators.Count));
                gen.RemoveIterator(iter);
            }
        }
        if (options.MutateParameters)
        {
            foreach (Iterator it in gen.Iterators)
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
                    it.WeightTo[itTo] = Math.Clamp(MutateValue(it.WeightTo[itTo], options), 0, 1);
                }
            }
        }
        if (options.MutatePalette)
        {
            //TODO: params
            Vector4 bias = RandomVector(0.2f, 0.8f);
            Vector4 mult = RandomVector(0.4f, 1.2f);
            Vector4 freq = RandomVector(0.1f, 0.5f);
            Vector4 phase = RandomVector(0.0f, 1.0f);
            gen.Palette = PaletteFromIqParams(bias, mult, freq, phase);
        }
        if (options.MutateColoring)
        {
            foreach (var it in gen.Iterators)
            {
                it.ColorIndex = MutateValue(it.ColorIndex, options);
                it.ColorSpeed = MutateValue(it.ColorSpeed, options);
            }
        }
        return gen;
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
            float maxComponent = Math.Max(Math.Max(c.X, c.Y), c.Z);
            c = Vector4.Divide(c, maxComponent);
            c = Vector4.Clamp(c, Vector4.Zero, Vector4.One);
            colors.Add(c);
        }

        return new FlamePalette
        {
            Name = "Generated Palette",
            Colors = colors
        };
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
