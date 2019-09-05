using IFSEngine.Helper;
using IFSEngine.TransformFunctions;
using IFSEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Model
{
    public class Iterator
    {
        public List<ITransformFunction> Transforms = new List<ITransformFunction>();
        public double w;
        public double cs;
        public double ci;//color index, 0 - 1
        public double op;

        public static Iterator RandomIterator {
            get
            {
                //TODO: remove switch, randomize transforms
                ITransformFunction r1 = null;
                switch (RandHelper.Next(3))
                {
                    case 0:
                        r1 = new Affine();
                        break;
                    case 1:
                        r1 = new Spherical();
                        break;
                    case 2:
                        r1 = Waves.RandomWaves;
                        break;
                    default:
                        break;
                };
                return new Iterator
                {
                    Transforms = new List<ITransformFunction> { Affine.RandomAffine, r1 },
                    ci = RandHelper.NextDouble(),
                    cs = (RandHelper.NextDouble() * 2.0f - 1.0f) * 0.1f,
                    op = RandHelper.NextDouble(),
                    w = RandHelper.NextDouble()
                };
            }
        }

    }

}
