using IFSEngine.Helper;
using IFSEngine.TransformFunctions;
using IFSEngine.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace IFSEngine.Model
{
    public class Iterator
    {
        public ITransformFunction Transform;// = new Affine();
        public double baseWeight;//baseweight
        public double cs;
        public double ci;//color index, 0 - 1
        public double op;

        /// <remarks>Custom serialization logic implemented in <see cref="IFS.JsonHelperXaos"/></remarks>
        [JsonIgnore]
        public Dictionary<Iterator, double> WeightTo = new Dictionary<Iterator, double>();

        public static Iterator RandomIterator
        {
            get
            {
                //TODO: remove switch, randomize transforms
                ITransformFunction r1 = null;
                switch (RandHelper.Next(5))
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
                    case 3:
                        r1 = new Foci();
                        break;
                    case 4:
                        r1 = new Loonie();
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
                    op = (RandHelper.Next(3) == 0) ? 0 : RandHelper.NextDouble(),
                    baseWeight = RandHelper.NextDouble()
                };
            }
        }

    }

}
