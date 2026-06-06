using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Newtonsoft.Json;

namespace IFSEngine.Model;

public class Iterator : IParamSource
{
    public int Id { get; init; } = Random.Shared.Next();
    public string Name { get; set; } = null;
    public TransformPlugin Transform { get; private set; }
    public Dictionary<string, double> RealParams { get; private set; } = [];
    public Dictionary<string, Vector3> Vec3Params { get; private set; } = [];

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

    public double this[int iteratorId]
    {
        get => WeightTo.First(i => i.Key.Id == iteratorId).Value;
        set => WeightTo[WeightTo.Keys.First(i => i.Id == iteratorId)] = value;
    }

    // Support animating custom params
    string IParamSource.ParamPathPrefix => $"Node[{Id}]";
    IReadOnlyDictionary<string, double> IParamSource.RealParamDefaults => Transform.RealParams;
    IReadOnlyDictionary<string, Vector3> IParamSource.Vec3ParamDefaults => Transform.Vec3Params;

    public Iterator() { }
    public Iterator(TransformPlugin plugin)
    {
        SetTransform(plugin);
    }

    /// <summary>
    /// Sets the transform plugin for this iterator and updates the parameter dictionaries while keeping existing parameter values.
    /// </summary>
    public void SetTransform(TransformPlugin plugin)
    {
        Transform = plugin;
        //remove old parameters, keep existing parameters, add new parameters with default value
        RealParams = plugin.RealParams.ToDictionary(kvp => kvp.Key,
            kvp => RealParams.TryGetValue(kvp.Key, out double val) ? val : kvp.Value);
        Vec3Params = plugin.Vec3Params.ToDictionary(kvp => kvp.Key,
            kvp => Vec3Params.TryGetValue(kvp.Key, out Vector3 val) ? val : kvp.Value);
    }

}
