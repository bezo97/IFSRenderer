using IFSEngine.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace IFSEngine.Serialization;

/// <summary>
/// Only serializes the Name and Version of the Transform.
/// </summary>
public class TransformConverter : JsonConverter<Transform>
{
    private readonly bool _ignoreVersion;
    private readonly IEnumerable<Transform> _loadedTransforms;

    public TransformConverter(IEnumerable<Transform> transforms, bool ignoreVersion)
    {
        _loadedTransforms = transforms;
        _ignoreVersion = ignoreVersion;
    }

    public override Transform ReadJson(JsonReader reader, Type objectType, /*[AllowNullAttribute]*/ Transform existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        (string Name, string Version) = serializer.Deserialize<ValueTuple<string, string>>(reader);
        var tf = _loadedTransforms.FirstOrDefault(i => i.Name == Name && i.Version == Version);//TODO: override and use Equals
        if (tf is null && _ignoreVersion)
            tf = _loadedTransforms.FirstOrDefault(i => i.Name == Name);
        if (tf is null)
            throw new UnknownTransformException(Name, Version);
        return tf;
    }

    public override void WriteJson(JsonWriter writer, /*[AllowNullAttribute]*/ Transform value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, (value.Name, value.Version));
    }
}
