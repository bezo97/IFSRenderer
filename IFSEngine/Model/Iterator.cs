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
        public ITransformFunction Transform;// = new Affine();
        public double baseWeight;//baseweight
        public double cs;
        public double ci;//color index, 0 - 1
        public double op;
        public Dictionary<Iterator, double> WeightTo = new Dictionary<Iterator, double>();

        public static Iterator RandomIterator {
            get
            {
                //TODO: remove switch, randomize transforms
                ITransformFunction r1 = null;
                switch (RandHelper.Next(3))
                {
                    case 0:
                        r1 = Affine.RandomAffine;
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
                    Transform = r1,
                    //Transforms = new List<ITransformFunction> { Affine.RandomAffine, r1 },
                    ci = RandHelper.NextDouble(),
                    cs = 1.0f - RandHelper.NextDouble() * 2.0f,
                    op = RandHelper.NextDouble(),
                    baseWeight = RandHelper.NextDouble()
                };
            }
        }

    }

}
