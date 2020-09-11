using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using IFSEngine.Util;
using Newtonsoft.Json;

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
        public double DepthOfField { get; set; } = 0.0;
        public double FocusDistance { get; set; } = 2.0;
        public double FocusArea { get; set; } = 0.25;
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

        //TODO: duplicate
        //public XForm DuplicateXForm(XForm a)
        //{
        //    XForm d = new XForm(a);//copy ctr
        //    XForms.Add(d);
        //    for (int fi = 0; fi < XForms.Count; fi++)
        //    {//itt nem lehet foreach mert modositjuk
        //        for (int ci = 0; ci < XForms[fi].GetConns().Count; ci++)
        //        {
        //            if (XForms[fi].GetConns()[ci].ConnTo == a)
        //                XForms[fi].SetConn(new Conn(d, 1));
        //        }
        //    }
        //    /*foreach (Conn c in a.GetConns())
        //    {
        //        if(c.ConnTo==a)
        //        {//ha önmaga, akkor az újat is önmagába kötjük
        //            d.SetConn(new Conn(d, c.WeightTo));
        //        }
        //        else
        //         d.SetConn(c);
        //    }*/
        //    //eredetit és duplikáltat is összekötjük
        //    d.SetConn(new Conn(a, 1.0));
        //    a.SetConn(new Conn(d, 1.0));
        //
        //    return d;
        //}

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

        public static IFS GenerateRandom()
        {
            IFS randomIFS = new IFS
            {
                Brightness = 4,
                Gamma = 4
            };
            //add random iterators
            var itnum = RandHelper.Next(3) + 2;
            for (var ii = 0; ii < itnum; ii++)
            {
                randomIFS.AddIterator(Iterator.RandomIterator(TransformFunction.LoadedTransformFunctions), true);
            }
            //randomize xaos weights
            foreach (var it in randomIFS.iterators)
            {
                foreach (var itTo in randomIFS.iterators)
                {
                    it.WeightTo[itTo] = RandHelper.Next(3) == 0 ? 0.0 : RandHelper.NextDouble();
                }
            }
            return randomIFS;
        }

        /// <summary>
        /// helper property to serilaize xaos weights
        /// hack: relies on hashset's implementation: preserves order
        /// </summary>
        [JsonProperty]
        private List<List<double>> JsonHelperXaos
        {
            get
            {
                List<List<double>> o = new List<List<double>>();
                foreach (Iterator iterator in iterators)
                {
                    o.Add(iterator.WeightTo.Values.ToList());
                }
                return o;
            }
            set
            {
                var tmpl = iterators.ToList();
                for (int iX = 0; iX < value.Count; iX++)
                {
                    tmpl[iX].WeightTo = tmpl.ToDictionary(k => k, v => value[iX][tmpl.IndexOf(v)]);
                }
            }
        }

    }
}
