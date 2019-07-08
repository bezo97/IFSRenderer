using System;
using System.Collections.Generic;
using System.Text;
using IFSEngine.Model.Camera;

namespace IFSEngine.Model
{
    public class IFS
    {
        public float Brightness { get; set; } = 1.0f;
        public float Gamma { get; set; } = 4.0f;

        public IFS(bool random=true)
        {
            RandomizeParams();
        }

        public List<Iterator> Iterators { get; set; } = new List<Iterator>();
        public Iterator FinalIterator { get; set; } = new Iterator(
            new Affine(0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
            0,//linear
            0.0f,
            0.0f,
            1.0f,
            1.0f
        );
        public CameraBase Camera { get; set; } = new YawPitchCamera();

        public IFS ResetCamera()
        {
            //HACK: az uj kameraig: resetnel a resolutiont megjegyezzuk
            int w = Camera.Width;
            int h = Camera.Height;

            Camera = new YawPitchCamera();

            Camera.Width = w;
            Camera.Height = h;

            return this;
        }

        public IFS RandomizeParams()
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

            return this;
        }

    }
}
