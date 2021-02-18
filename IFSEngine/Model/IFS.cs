using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using IFSEngine.Util;

namespace IFSEngine.Model
{
    /// <summary>
    /// Iterated Function System.
    /// The model is similar to flam3, with additional parameters like DepthOfField and FocusDistance.
    /// </summary>
    public class IFS
    {
        public IReadOnlyCollection<Iterator> Iterators => iterators;//TODO: use IReadOnlySet in .NET5
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

        public static IFS GenerateRandom(List<TransformFunction> availableTransforms)
        {
            IFS randomIFS = new IFS
            {
                Brightness = 10,
                Gamma = 2
            };
            //add random iterators
            var affinetf = new List<TransformFunction>() { availableTransforms.First(tf => tf.Name == "Affine") };//TODO: nicer
            var pretfs = new List<Iterator>();
            var tfs = new List<Iterator>();
            var itnum = RandHelper.Next(3) + 2;
            for (var ii = 0; ii < itnum; ii++)
            {
                var pretransform = Iterator.RandomIterator(affinetf);
                foreach (var key in pretransform.TransformVariables.Keys.ToList())//contracting affine transform
                    pretransform.TransformVariables[key] = pretransform.TransformVariables[key]*0.75;
                var transform = Iterator.RandomIterator(availableTransforms);
                randomIFS.AddIterator(pretransform, false);
                randomIFS.AddIterator(transform, false);
                pretransform.WeightTo[transform] = 1.0;
                pretransform.Opacity = 0.0;
                pretransform.ColorSpeed = 0.0;
                pretransform.InputWeight = 1.0;
                transform.InputWeight = 0.0;
                transform.Opacity = 1.0;
                pretfs.Add(pretransform);
                tfs.Add(transform);
            }
            //randomize xaos weights
            foreach (var it in tfs)
            {
                foreach (var itTo in pretfs)
                {
                    it.WeightTo[itTo] = RandHelper.Next(3) == 0 ? 0.0 : RandHelper.NextDouble();
                }
            }
            return randomIFS;
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
