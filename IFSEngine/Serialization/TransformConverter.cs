using System;
using System.Collections.Generic;
using System.Linq;

using IFSEngine.Model;

using Newtonsoft.Json;

namespace IFSEngine.Serialization;

/// <summary>
/// Only serializes the Name and Version of the Transform.
/// </summary>
public class TransformConverter : JsonConverter<TransformPlugin>
{
    private readonly bool _ignoreVersion;
    private readonly IEnumerable<TransformPlugin> _loadedTransforms;

    public TransformConverter(IEnumerable<TransformPlugin> transforms, bool ignoreVersion)
    {
        _loadedTransforms = transforms;
        _ignoreVersion = ignoreVersion;
    }

    public override TransformPlugin ReadJson(JsonReader reader, Type objectType, /*[AllowNullAttribute]*/ TransformPlugin existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        (string Name, string Version) = serializer.Deserialize<ValueTuple<string, string>>(reader);
        var tf = _loadedTransforms.FirstOrDefault(i => i.Name == Name && i.Version == Version);//TODO: override and use Equals
        if (tf is null && _ignoreVersion)
            tf = _loadedTransforms.FirstOrDefault(i => i.Name == Name);
        if (tf is null)
            throw new UnknownPluginException(Name, Version);
        return tf;
    }

    public override void WriteJson(JsonWriter writer, /*[AllowNullAttribute]*/ TransformPlugin value, JsonSerializer serializer) => serializer.Serialize(writer, (value.Name, value.Version));
}
