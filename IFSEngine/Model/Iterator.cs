using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace IFSEngine.Model;

public class Iterator
{
    public int Id { get; init; } = Random.Shared.Next();
    public string Name { get; set; } = null;
    public Transform Transform { get; private set; }
    public Dictionary<string, double> RealParams { get; private set; } = new();
    public Dictionary<string, Vector3> Vec3Params { get; private set; } = new();

    public double BaseWeight { get; set; } = 1.0;//not normalized
    public double ColorSpeed { get; set; } = 0.5;
    public double ColorIndex { get; set; } = 0.0;//0 - 1
    public double StartWeight { get; set; } = 1.0;
    public double Opacity { get; set; } = 1.0;
    public double Mix { get; set; } = 1.0;
    public double Add { get; set; } = 0.0;
    public ShadingMode ShadingMode { get; set; } = ShadingMode.Default;

    /// <remarks>Custom serialization logic implemented in <see cref="Serialization.IfsConverter"/></remarks>
    [JsonIgnore]
    public Dictionary<Iterator, double> WeightTo { get; set; } = [];

    //public double this[int iteratorId] 
    //{
    //    get => WeightTo.First(i => i.Key.Id == iteratorId).Value;
    //    set => WeightTo[WeightTo.Keys.First(i => i.Id == iteratorId)] = value;
    //}

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

}
