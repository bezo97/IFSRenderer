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
internal class TransformConverter : JsonConverter<Transform>
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
        if (tf == null)
        {
            if (_ignoreVersion)
                return _loadedTransforms.FirstOrDefault(i => i.Name == Name);
            else
                throw new SerializationException($"The Transform '{Name}' (Version: {Version}) is unknown.");
        }
        return tf;
    }

    public override void WriteJson(JsonWriter writer, /*[AllowNullAttribute]*/ Transform value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, (value.Name, value.Version));
    }
}
