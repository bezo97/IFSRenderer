using IFSEngine.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IFSEngine.Model
{
    public class Iterator
    {
        public TransformFunction TransformFunction { get; private set; }
        public Dictionary<string, double> TransformVariables { get; private set; }

        public double BaseWeight { get; set; } = 1.0;//not normalized
        public double ColorSpeed { get; set; } = 0.0;
        public double ColorIndex { get; set; } = 0.0;//0 - 1
        public double InputWeight { get; set; } = 1.0;
        public double Opacity { get; set; } = 1.0;
        public ShadingMode ShadingMode { get; set; } = ShadingMode.Default;

        /// <remarks>Custom serialization logic implemented in <see cref="IFS.JsonHelperXaos"/></remarks>
        [JsonIgnore]
        public Dictionary<Iterator, double> WeightTo { get; set; } = new Dictionary<Iterator, double>();

        public Iterator() { }
        public Iterator(TransformFunction tf)
        {
            SetTransformFunction(tf);
        }

        public void SetTransformFunction(TransformFunction tf)
        {
            TransformFunction = tf;
            TransformVariables = TransformFunction.Variables.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static Iterator RandomIterator(List<TransformFunction> transforms)
        {
            var tf = transforms[RandHelper.Next(transforms.Count)];
            var iterator = new Iterator(tf)
            {
                ColorIndex = RandHelper.NextDouble(),
                ColorSpeed = RandHelper.NextDouble(),
                InputWeight = 1.0,
                Opacity = (RandHelper.Next(3) == 0) ? 0 : RandHelper.NextDouble(),
                BaseWeight = RandHelper.NextDouble()
            };
            foreach (var var in iterator.TransformVariables.Keys.ToList())
                iterator.TransformVariables[var] = RandHelper.NextDouble() * 2.2 - 1.1;
            return iterator;
        }

    }
}
