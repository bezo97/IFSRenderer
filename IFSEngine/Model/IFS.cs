using IFSEngine.Animation;
using IFSEngine.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace IFSEngine.Model;

/// <summary>
/// Iterated Function System.
/// The model is similar to flam3, with additional parameters like DepthOfField and FocusDistance.
/// </summary>
public class IFS
{
    public string Title { get; set; } = "Untitled";
    public IReadOnlyList<Author> Authors => authors;
    public IReadOnlySet<Iterator> Iterators => iterators;

    /// <summary>
    /// Entropy is the probability to reset on each iteration.
    /// </summary>
    /// <remarks>
    /// Based on zy0rg's description, but it applies to the whole system for now.
    /// This replaces the constant 10 000 iteration depth in Flame. Defaults to 0.01.
    /// </remarks>
    public double Entropy { get; set; } = 0.01;

    /// <summary>
    /// Number of iterations to skip plotting after reset.
    /// </summary>
    /// <remarks>
    /// This is needed to avoid seeing the starting random points.
    /// Also known as "fuse count". This is 20 in Flame. Defaults to 0.
    /// </remarks>
    public int Warmup { get; set; } = 0;
    public double Brightness { get; set; } = 1.0;
    public double Gamma { get; set; } = 1.0;
    public double GammaThreshold { get; set; } = 0.0;
    public double Vibrancy { get; set; } = 1.0;
    public double FogEffect { get; set; } = 0.0;
    public Camera Camera { get; set; } = new Camera();
    public Color BackgroundColor { get; set; } = Color.Black;
    public Size ImageResolution { get; set; } = new Size(1920, 1080);
    public FlamePalette Palette { get; set; } = FlamePalette.Default;
    public Dopesheet Dopesheet { get; set; } = new Dopesheet();//null;
    /// <summary>
    /// Rendering stops after this number of iterations (2^Pow). Type is double to allow animation.
    /// </summary>
    public double StoppingIterationPower { get; set; } = 35;

    protected HashSet<Iterator> iterators = new();
    protected List<Author> authors = new();

    public Iterator this[int iteratorId] => Iterators.First(i=> i.Id == iteratorId);

    /// <param name="connect">Whether to connect the new <see cref="Iterator"/> to existing ones.</param>
    public void AddIterator(Iterator newIterator, bool connect)
    {
        iterators.Add(newIterator);
        double weight = connect ? 1.0 : 0.0;
        foreach (var it in iterators)
        {
            newIterator.WeightTo[it] = weight;
            it.WeightTo[newIterator] = weight;
        }
    }

    /// <param name="splitWeights">Whether to adjust the base weights of the original iterator and its duplicate in order to keep a similar look of the fractal. "False" results in the duplicate operation known from flame fractals.</param>
    /// <returns>The created duplicate.</returns>
    public Iterator DuplicateIterator(Iterator original, bool splitWeights)
    {
        //clone first
        Iterator dupe = new(original.Transform)
        {
            BaseWeight = original.BaseWeight,
            ColorIndex = original.ColorIndex,
            ColorSpeed = original.ColorSpeed,
            Opacity = original.Opacity,
            ShadingMode = original.ShadingMode,
            StartWeight = original.StartWeight,
            Mix = original.Mix,
            Add = original.Add,
            Name = original.Name
        };
        //copy parameter values
        foreach (var tv in original.RealParams)
            dupe.RealParams[tv.Key] = tv.Value;
        foreach (var tv in original.Vec3Params)
            dupe.Vec3Params[tv.Key] = tv.Value;
        //add to the ifs
        AddIterator(dupe, false);
        //copy connection weights
        foreach (Iterator it in Iterators)
        {//'from' weights
            if (it.WeightTo.TryGetValue(original, out double w))
                it.WeightTo[dupe] = w;
        }
        foreach (var w in original.WeightTo)
        {//'to' weights
            if (w.Key == original)//if self-connected, do the same on the duplicate
                dupe.WeightTo[dupe] = w.Value;
            else
                dupe.WeightTo[w.Key] = w.Value;
        }
        
        if (splitWeights)
        {//split base weights, no connection between the two
            original.BaseWeight /= 2;
            dupe.BaseWeight = original.BaseWeight;
            dupe.WeightTo[original] = 0.0;
            original.WeightTo[dupe] = 0.0;
        }
        else
        {//connect the original with the duplicate
            dupe.WeightTo[original] = 1.0;
            original.WeightTo[dupe] = 1.0;
        }
        return dupe;
    }

    public void RemoveIterator(Iterator it1)
    {
        if (!iterators.Contains(it1))
            return;
        foreach (var it in iterators)
        {
            it1.WeightTo.Remove(it);
            it.WeightTo.Remove(it1);
        }
        iterators.Remove(it1);
    }

    public void ReloadTransforms(IEnumerable<Transform> transforms)
    {
        foreach (var iterator in iterators.ToList())
        {
            //ignore transform version checking here so they are updated.
            var newtf = transforms.FirstOrDefault(tf => tf.Name == iterator.Transform.Name);
            if (newtf != null)
                iterator.SetTransform(newtf);
            //leave old transform if a newer version is not found
        }
    }

    public void AddAuthor(Author author)
    {
        if (!authors.Contains(author))
            authors.Add(author);
    }

    public static readonly IFS Default = new();

}
