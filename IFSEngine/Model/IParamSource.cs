using System.Collections.Generic;
using System.Numerics;

namespace IFSEngine.Model;

/// <summary>
/// Common interface for objects that hold user-editable plugin parameters (transform/postfx).
/// </summary>
public interface IParamSource
{
    /// <summary>
    /// Unique identifier of the parameter holder (such as an iterator or post-effect instance), used for animation channel paths.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// The first segment of the animation path used to locate this source on the IFS root object.
    /// Iterators return "Node[{Id}]", EffectLayers return "PostFx[{Id}]".
    /// </summary>
    string ParamPathPrefix { get; }

    /// <summary>
    /// Parameter map for real-typed parameters.
    /// For example: "Strength" -> 0.5
    /// </summary>
    Dictionary<string, double> RealParams { get; }

    /// <summary>
    /// Parameter map for vector3-typed parameters.
    /// For example: "Offset" -> Vector3(0, 0, 0)
    /// </summary>
    Dictionary<string, Vector3> Vec3Params { get; }

    /// <summary>
    /// Default values for real-typed parameters. Each real param has a default value.
    /// </summary>
    IReadOnlyDictionary<string, double> RealParamDefaults { get; }

    /// <summary>
    /// Default values for vector3-typed parameters. Each vector3 param has a default value.
    /// </summary>
    IReadOnlyDictionary<string, Vector3> Vec3ParamDefaults { get; }
}
