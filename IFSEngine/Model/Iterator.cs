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
        public ITransformFunction Transform = new Affine();
        public double BaseWeight = 1.0;//use ifs.NormalizeBaseWeights()
        public double ColorSpeed = 0.0;
        public double ColorIndex = 0.0;//0 - 1
        public double Opacity = 1.0;

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
                    ColorIndex = RandHelper.NextDouble(),
                    ColorSpeed = 1.0f - RandHelper.NextDouble() * 2.0f,
                    Opacity = (RandHelper.Next(3) == 0) ? 0 : RandHelper.NextDouble(),
                    BaseWeight = RandHelper.NextDouble()
                };
            }
        }

    }

}
