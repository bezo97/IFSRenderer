using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using IFSEngine.Utility;

namespace IFSEngine.Model
{
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
        
        protected HashSet<Iterator> iterators = new();
        protected List<Author> authors = new();

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

        public Iterator DuplicateIterator(Iterator a)
        {
            //clone first
            Iterator d = new(a.Transform)
            {
                BaseWeight = a.BaseWeight,
                ColorIndex = a.ColorIndex,
                ColorSpeed = a.ColorSpeed,
                Opacity = a.Opacity,
                ShadingMode = a.ShadingMode,
                StartWeight = a.StartWeight,
                
            };
            //copy parameter values
            foreach (var tv in a.RealParams)
                d.RealParams[tv.Key] = tv.Value;
            foreach (var tv in a.Vec3Params)
                d.Vec3Params[tv.Key] = tv.Value;
            //add to the ifs
            AddIterator(d, false);
            //copy connection weights
            foreach(Iterator it in Iterators)
            {//'from' weights
                if (it.WeightTo.TryGetValue(a, out double w))
                    it.WeightTo[d] = w;
            }
            foreach (var w in a.WeightTo)
            {//'to' weights
                if(w.Key == a)//if self-connected, do the same on the duplicate
                    d.WeightTo[d] = w.Value;
                else
                    d.WeightTo[w.Key] = w.Value;
            }
            //connect the original with the duplicate
            //d.WeightTo[a] = 1.0;
            //a.WeightTo[d] = 1.0;
            a.WeightTo[d] = 0.0;
            //split base weight
            a.BaseWeight /= 2;
            d.BaseWeight = a.BaseWeight;
            return d;
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
                if(newtf != null)
                    iterator.SetTransform(newtf);
                //leave old transform if a newer version is not found
            }
        }

        public void AddAuthor(Author author)
        {
            if(!authors.Contains(author))
                authors.Add(author);
        }

    }
}
