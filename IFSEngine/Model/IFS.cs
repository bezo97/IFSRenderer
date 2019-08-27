using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IFSEngine.Helper;
using IFSEngine.Model.Camera;
using IFSEngine.TransformFunctions;
using IFSEngine.Util;
using Newtonsoft.Json;

namespace IFSEngine.Model
{
    public class IFS
    {
        //TODO: IFS: Palette + Iterators + Views (+ Animations?)

        public UFPalette Palette { get; set; } = UFPalette.Default;
        public List<IFSView> Views { get; set; } = new List<IFSView>();
        public List<Iterator> Iterators { get; set; } = new List<Iterator>();

        public IFS(bool random=true)
        {
            RandomizeParams();
            Views.Add(new IFSView());
        }

        public void RandomizeParams()
        {
            Iterators.Clear();
            double SummWeights = 0.0f;
            var itnum = RandHelper.Next(5) + 2;

            for (var ii = 0; ii < itnum; ii++)
            {
                Iterators.Add(Iterator.RandomIterator);

                SummWeights += Iterators.Last().w;
            }

            //normalize weights
            for (var s = 0; s < Iterators.Count; s++)
            {
                var tmpit = Iterators[s];
                tmpit.w /= SummWeights;
                Iterators[s] = tmpit;
            }

        }

        public static IFS LoadJson(string path)
        {
            return JsonConvert.DeserializeObject<IFS>(File.ReadAllText(path));
        }

        public void SaveJson(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this));
        }

    }
}
