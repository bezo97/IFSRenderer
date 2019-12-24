using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IFSEngine.Util;
using Newtonsoft.Json;

namespace IFSEngine.Model
{
    public class IFS
    {
        //TODO: IFS: Palette + Iterators + Views (+ Animations?)

        public UFPalette Palette { get; set; } = UFPalette.Default;
        public IFSView ViewSettings { get; set; } = new IFSView();
        public HashSet<Iterator> Iterators { get; } = new HashSet<Iterator>();

        public IFS(bool random = false)
        {
            if(random)
                RandomizeParams();
        }

        public void AddIterator(Iterator it1, bool connect)
        {
            Iterators.Add(it1);
            if (connect)
            {//connect to/from each existing Iterator
                foreach (var it in Iterators)
                {
                    it1.WeightTo[it] = 1.0;
                    it.WeightTo[it1] = 1.0;
                }
            }
            NormalizeBaseWeights();
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
            if (!Iterators.Contains(it1))
                return;
            foreach (var it in Iterators)
            {
                it1.WeightTo.Remove(it);
                it.WeightTo.Remove(it1);
            }
            Iterators.Remove(it1);
            NormalizeBaseWeights();
        }

        private void RandomizeParams()
        {
            Iterators.Clear();
            var itnum = RandHelper.Next(5) + 2;
            for (var ii = 0; ii < itnum; ii++)
            {
                AddIterator(Iterator.RandomIterator, true);
            }
            NormalizeBaseWeights();
            //randomize xaos weights
            foreach (var it in Iterators)
            {
                foreach (var itTo in Iterators)
                {
                    it.WeightTo[itTo] = RandHelper.Next(3) == 0 ? 0.0 : RandHelper.NextDouble();
                }
            }
        }

        public void NormalizeBaseWeights()
        {
            double SummWeights = 0.0f;
            foreach (var it in Iterators)
            {
                SummWeights += it.baseWeight;
            }
            foreach (var it in Iterators)
            {
                it.baseWeight /= SummWeights;
            }
        }

        public static IFS LoadJson(string path)
        {
            var ifs = JsonConvert.DeserializeObject<IFS>(File.ReadAllText(path), new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto //heterogen collection
            });
            ifs.ViewSettings.Camera.UpdateCamera();//hack
            return ifs;
        }

        public void SaveJson(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto, //heterogen collection
            }));
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
                foreach (Iterator iterator in Iterators)
                {
                    o.Add(iterator.WeightTo.Values.ToList());
                }
                return o;
            }
            set
            {
                var tmpl = Iterators.ToList();
                for (int iX = 0; iX < value.Count; iX++)
                {
                    tmpl[iX].WeightTo = tmpl.ToDictionary(k => k, v => value[iX][tmpl.IndexOf(v)]);
                }
            }
        }

    }
}
