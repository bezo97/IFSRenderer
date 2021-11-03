using IFSEngine.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IFSEngine.Model
{
    public class Iterator
    {
        public Transform Transform { get; private set; }
        public Dictionary<string, double> Variables { get; private set; } = new Dictionary<string, double>();

        public double BaseWeight { get; set; } = 1.0;//not normalized
        public double ColorSpeed { get; set; } = 0.0;
        public double ColorIndex { get; set; } = 0.0;//0 - 1
        public double StartWeight { get; set; } = 1.0;
        public double Opacity { get; set; } = 1.0;
        public ShadingMode ShadingMode { get; set; } = ShadingMode.Default;

        /// <remarks>Custom serialization logic implemented in <see cref="IFS.JsonHelperXaos"/></remarks>
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
            //remove old variables, keep existing variables, add new variables with default value
            Variables = tf.Variables.ToDictionary(kvp => kvp.Key,
                kvp => Variables.TryGetValue(kvp.Key, out double val) ? val : kvp.Value);
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
            //TODO: variable descriptor that tells min,max,increment,..
            foreach (var var in iterator.Variables.Keys.ToList())
                iterator.Variables[var] = RandHelper.NextDouble() * 2.2 - 1.1;
            return iterator;
        }

    }
}
