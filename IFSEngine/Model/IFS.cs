using System;
using System.Collections.Generic;
using System.IO;
using IFSEngine.Model.Camera;
using Newtonsoft.Json;

namespace IFSEngine.Model
{
    public class IFS
    {
        //TODO: IFS: Palette + Iterators + FinalIterator + Views (+ Animations?)

        public UFPalette Palette { get; set; } = UFPalette.Default;
        public List<IFSView> Views { get; set; } = new List<IFSView>();
        public List<Iterator> Iterators { get; set; } = new List<Iterator>();
        public Iterator FinalIterator { get; set; } = new Iterator(
            new Affine(0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
            0,//linear
            0.0f,
            0.0f,
            1.0f,
            1.0f
        );

        public IFS(bool random=true)
        {
            RandomizeParams();
            Views.Add(new IFSView());
        }

        public void RandomizeParams()
        {
            Random rand = new Random();
            Iterators.Clear();
            float SummWeights = 0.0f;
            var itnum = rand.Next(5) + 2;

            for (var ii = 0; ii < itnum; ii++)
            {
                var nit = new Iterator();
                nit.aff.ox = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.oy = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.oz = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.xx = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.xy = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.xz = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.yx = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.yy = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.yz = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.zx = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.zy = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.zz = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.w = (float)rand.NextDouble();
                nit.cs = (float)(rand.NextDouble() * 2.0f - 1.0f) * 0.1f;
                nit.ci = (float)rand.NextDouble();
                nit.op = (float)rand.NextDouble();
                nit.tfID = rand.Next() % 3;//transform id
                Iterators.Add(nit);

                SummWeights += nit.w;
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
