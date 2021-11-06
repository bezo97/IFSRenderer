using IFSEngine.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace IFSEngine.Model
{
    public class Iterator
    {
        public Transform Transform { get; private set; }
        public Dictionary<string, double> RealParams { get; private set; } = new Dictionary<string, double>();
        public Dictionary<string, Vector3> Vec3Params { get; private set; } = new Dictionary<string, Vector3>();

        public double BaseWeight { get; set; } = 1.0;//not normalized
        public double ColorSpeed { get; set; } = 0.0;
        public double ColorIndex { get; set; } = 0.0;//0 - 1
        public double StartWeight { get; set; } = 1.0;
        public double Opacity { get; set; } = 1.0;
        public ShadingMode ShadingMode { get; set; } = ShadingMode.Default;

        /// <remarks>Custom serialization logic implemented in <see cref="Serialization.IfsConverter"/></remarks>
        [JsonIgnore]
        public Dictionary<Iterator, double> WeightTo { get; set; } = new Dictionary<Iterator, double>();

        public Iterator() { }
        public Iterator(Transform tf)
        {
            SetTransform(tf);
        }

        public void SetTransform(Transform tf)
        {
            Transform = tf;
            //remove old parameters, keep existing parameters, add new parameters with default value
            RealParams = tf.RealParams.ToDictionary(kvp => kvp.Key,
                kvp => RealParams.TryGetValue(kvp.Key, out double val) ? val : kvp.Value);
            Vec3Params = tf.Vec3Params.ToDictionary(kvp => kvp.Key,
                kvp => Vec3Params.TryGetValue(kvp.Key, out Vector3 val) ? val : kvp.Value);
        }

        public static Iterator RandomIterator(List<Transform> transforms)
        {
            var tf = transforms[RandHelper.Next(transforms.Count)];
            var iterator = new Iterator(tf)
            {
                ColorIndex = RandHelper.NextDouble(),
                ColorSpeed = RandHelper.NextDouble(),
                StartWeight = 1.0,
                Opacity = (RandHelper.Next(3) == 0) ? 0 : RandHelper.NextDouble(),
                BaseWeight = RandHelper.NextDouble()
            };
            //TODO: parameter descriptor that tells min,max,increment,..
            foreach (var var in iterator.RealParams.Keys.ToList())
                iterator.RealParams[var] = RandHelper.NextDouble() * 2.2 - 1.1;
            foreach (var var in iterator.Vec3Params.Keys.ToList())
                iterator.Vec3Params[var] = new Vector3(
                    (float)(RandHelper.NextDouble() * 2.2 - 1.1),
                    (float)(RandHelper.NextDouble() * 2.2 - 1.1),
                    (float)(RandHelper.NextDouble() * 2.2 - 1.1));
            return iterator;
        }

    }
}
