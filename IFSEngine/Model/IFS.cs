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
        public IReadOnlySet<Iterator> Iterators => iterators;
        public double Brightness { get; set; } = 1.0;
        public double Gamma { get; set; } = 1.0;
        public double GammaThreshold { get; set; } = 0.0;
        public double Vibrancy { get; set; } = 1.0;
        public double FogEffect { get; set; } = 0.0;
        public Camera Camera { get; set; } = new Camera();
        public Color BackgroundColor { get; set; } = Color.Black;
        public Size ImageResolution { get; set; } = new Size(1920, 1080);
        public FlamePalette Palette { get; set; } = FlamePalette.Default;
        
        protected HashSet<Iterator> iterators = new HashSet<Iterator>();

        /// <param name="connect">Whether to connect the new <see cref="Iterator"/> to existing ones.</param>
        public void AddIterator(Iterator newIterator, bool connect)
        {
            iterators.Add(newIterator);
            if (connect)
            {
                foreach (var it in iterators)
                {
                    newIterator.WeightTo[it] = 1.0;
                    it.WeightTo[newIterator] = 1.0;
                }
            }
        }

        public Iterator DuplicateIterator(Iterator a)
        {
            //clone first
            Iterator d = new Iterator(a.TransformFunction)
            {
                BaseWeight = a.BaseWeight,
                ColorIndex = a.ColorIndex,
                ColorSpeed = a.ColorSpeed,
                Opacity = a.Opacity,
                ShadingMode = a.ShadingMode,
                
            };
            //copy variable values
            foreach (var tv in a.TransformVariables)
                d.TransformVariables[tv.Key] = tv.Value;
            //connect weights
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
            //connect he original with the duplicate
            //d.WeightTo[a] = 1.0;
            //a.WeightTo[d] = 1.0;
            a.WeightTo.Remove(d);

            AddIterator(d, false);
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

        public void ReloadTransforms(IEnumerable<TransformFunction> transforms)
        {
            foreach (var iterator in iterators.ToList())
            {
                //ignore transform version checking here so they are updated.
                var newtf = transforms.FirstOrDefault(tf => tf.Name == iterator.TransformFunction.Name);
                if(newtf != null)
                    iterator.SetTransformFunction(newtf);
                //leave old transform if a newer version is not found
            }
        }

    }
}
