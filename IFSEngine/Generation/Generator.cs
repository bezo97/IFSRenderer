using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using IFSEngine.Model;
using IFSEngine.Utility;

namespace IFSEngine.Generation;

public class Generator
{
    public List<TransformPlugin> SelectedTransforms { get; set; }

    private static readonly HashSet<string> _preferredTransformNames = ["Affine", "Möbius", "Rotate Euler", "Spherical", "Translate"];
    private static readonly HashSet<string> _angleParams = ["angle", "rot", "rotate", "rotation", "orientation", "inclination", "azimuth"];

    // IQ palette generator parameter ranges
    private const float IqBiasMin = 0.4f, IqBiasMax = 0.8f;
    private const float IqMultMin = 0.2f, IqMultMax = 1.2f;
    private const float IqFreqMin = 0.1f, IqFreqMax = 1.0f;
    private const float IqPhaseMin = 0.0f, IqPhaseMax = 1.0f;

    public Generator(IEnumerable<TransformPlugin> transforms)
    {
        SelectedTransforms = transforms.ToList();
    }

    public IEnumerable<IFS> GenerateBatch(GeneratorOptions options)
    {
        for (int i = 0; i < options.BatchSize; i++)
        {
            yield return GenerateOne(options);
        }
    }

    public IFS GenerateOne(GeneratorOptions options)
    {
        IFS gen = options.BaseParams.DeepClone();
        var preferredTranforms = SelectedTransforms.Where(t => _preferredTransformNames.Contains(t.Name)).ToList();
        if (options.MutateIterators)
        {
            while (gen.Iterators.Count < 4)
            {
                gen.AddIterator(CreateIterator(options, preferredTranforms), true);
            }
            if (options.MutationChance > Random.Shared.NextDouble())
            {
                gen.AddIterator(CreateRandomOrPreferredIterator(options), true);
            }
            if (gen.Iterators.Count > 4 && options.MutationChance > Random.Shared.NextDouble())
            {
                var iter = gen.Iterators.ElementAt(Random.Shared.Next(gen.Iterators.Count));
                gen.RemoveIterator(iter);
            }
        }
        if (options.MutateParameters)
        {
            foreach (var iterator in gen.Iterators)
                MutateIteratorParams(iterator, options);
        }
        if (options.MutateConnections)
        {//add/remove
            foreach (var it in gen.Iterators)
            {
                foreach (var itTo in gen.Iterators)
                {
                    if (!it.WeightTo.TryGetValue(itTo, out _))
                        it.WeightTo[itTo] = 0.0;//hack
                    if (options.MutationChance * 0.5 > Random.Shared.NextDouble()) //TODO: separate chance?
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
                    it.WeightTo[itTo] = Math.Max(0, MutateValue(it.WeightTo[itTo], options.MutationChance, options.MutationStrength));
                }
            }
        }
        if (options.MutatePalette)
        {
            gen.Palette = GenerateRandomIqPalette(options.PaletteInterpolationMode);
        }
        if (options.MutateColoring)
        {
            foreach (var it in gen.Iterators)
            {
                it.ColorIndex = Math.Clamp(MutateValue(it.ColorIndex, options.MutationChance, options.MutationStrength), 0.0, 1.0);
                it.ColorSpeed = MutateValue(it.ColorSpeed, options.MutationChance, options.MutationStrength);
            }
        }
        return gen;
    }

    /// <summary>
    /// Generate a random IQ palette with randomized parameters within the default ranges.
    /// </summary>
    private static ColorPalette GenerateRandomIqPalette(InterpolationMode interpolationMode)
    {
        var bias = RandomVector(IqBiasMin, IqBiasMax);
        var mult = RandomVector(IqMultMin, IqMultMax);
        var freq = RandomVector(IqFreqMin, IqFreqMax);
        var phase = RandomVector(IqPhaseMin, IqPhaseMax);
        return IqPaletteGenerator.Generate(bias, mult, freq, phase, interpolationMode, 10);
    }

    private static Vector4 RandomVector(float min, float max)
    {
        float range = max - min;
        return new Vector4(
            min + range * (float)Random.Shared.NextDouble(),
            min + range * (float)Random.Shared.NextDouble(),
            min + range * (float)Random.Shared.NextDouble(),
            1.0f);
    }

    private Iterator CreateRandomOrPreferredIterator(GeneratorOptions options)
    {
        Iterator newIterator;
        //50% chance the new iterator is preferred
        if (Random.Shared.NextDouble() < 0.5)
            newIterator = CreateIterator(options, SelectedTransforms);
        else
        {
            var preferredTranforms = SelectedTransforms.Where(t => _preferredTransformNames.Contains(t.Name)).ToList();
            newIterator = CreateIterator(options, preferredTranforms);
        }
        return newIterator;
    }

    private static Iterator CreateIterator(GeneratorOptions options, List<TransformPlugin> transforms)
    {
        var selectedTransform = transforms[Random.Shared.Next(transforms.Count)];

        var iterator = new Iterator(selectedTransform)
        {
            BaseWeight = 0.5 + Random.Shared.NextDouble(),
            StartWeight = 1.0,
            ColorIndex = Random.Shared.NextDouble(),
            ColorSpeed = 0.25 + 0.5 * Random.Shared.NextDouble(),
            Opacity = (Random.Shared.Next(3) == 0) ? 0 : Random.Shared.NextDouble(),
            ShadingMode = (Random.Shared.Next(10) == 0) ? ShadingMode.DeltaPSpeed : ShadingMode.Default
        };

        //consider tags to use plugins the right way
        if (iterator.Transform.Tags.Contains("shape"))
        {
            iterator.Opacity = 0.0;
            iterator.Add = 1.0;
            iterator.ColorSpeed = 0.0;
        }

        MutateIteratorParams(iterator, options);
        return iterator;
    }

    private static double MutateValue(double val, double chance, double strength)
    {
        if (chance > Random.Shared.NextDouble())
            return val + -strength + 2.0 * strength * Random.Shared.NextDouble();
        else
            return val;
    }
    private static Vector3 MutateVec3(Vector3 val, double chance, double strength)
    {
        if (chance > Random.Shared.NextDouble())
        {
            Vector3 v = val;
            v.X = (float)(v.X + -strength + 2.0 * strength * Random.Shared.NextDouble());
            v.Y = (float)(v.Y + -strength + 2.0 * strength * Random.Shared.NextDouble());
            v.Z = (float)(v.Z + -strength + 2.0 * strength * Random.Shared.NextDouble());
            return v;
        }
        else
            return val;
    }
    private static void MutateIteratorParams(Iterator iterator, GeneratorOptions options)
    {
        foreach (var v in iterator.RealParams)
        {
            iterator.RealParams[v.Key] = MutateValue(iterator.RealParams[v.Key], options.MutationChance, options.MutationStrength * (IsAngleParameter(v.Key) ? 360 : 1));
        }
        foreach (var v in iterator.Vec3Params)
        {
            iterator.Vec3Params[v.Key] = MutateVec3(iterator.Vec3Params[v.Key], options.MutationChance, options.MutationStrength * (IsAngleParameter(v.Key) ? 360 : 1));
        }
    }

    private static bool IsAngleParameter(string paramName)
    {
        var lc = paramName.ToLowerInvariant();
        return lc == "r" || _angleParams.Any(lc.Contains);
    }
}
