using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace IFSEngine.Model;

/// <summary>
/// Represents an instance of an effect plugin with its parameter values and enabled state.
/// Similar to how Iterator holds a Transform with custom parameters.
/// </summary>
public class EffectLayer : IParamSource
{
    public int Id { get; init; } = Random.Shared.Next();
    public EffectPlugin Effect { get; private set; }
    public Dictionary<string, double> RealParams { get; private set; } = [];
    public Dictionary<string, Vector3> Vec3Params { get; private set; } = [];
    public bool Enabled { get; set; } = true;

    public EffectLayer() { }

    public EffectLayer(EffectPlugin plugin)
    {
        SetEffect(plugin);
    }

    /// <summary>
    /// Sets the effect plugin for this layer and updates the parameter dictionaries while keeping existing parameter values.
    /// </summary>
    public void SetEffect(EffectPlugin plugin)
    {
        Effect = plugin;
        //remove old parameters, keep existing parameters, add new parameters with default value
        RealParams = plugin.RealParams.ToDictionary(kvp => kvp.Key,
            kvp => RealParams.TryGetValue(kvp.Key, out double val) ? val : kvp.Value);
        Vec3Params = plugin.Vec3Params.ToDictionary(kvp => kvp.Key,
            kvp => Vec3Params.TryGetValue(kvp.Key, out Vector3 val) ? val : kvp.Value);
    }

    // Support animating custom params
    string IParamSource.ParamPathPrefix => $"PostFx[{Id}]";
    IReadOnlyDictionary<string, double> IParamSource.RealParamDefaults => Effect.RealParams;
    IReadOnlyDictionary<string, Vector3> IParamSource.Vec3ParamDefaults => Effect.Vec3Params;
}
