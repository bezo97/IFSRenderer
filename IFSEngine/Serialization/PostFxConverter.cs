using System;
using System.Collections.Generic;
using System.Linq;

using IFSEngine.Model;

using Newtonsoft.Json;

namespace IFSEngine.Serialization;

/// <summary>
/// Only serializes the Name, Version and params of the PostFx.
/// </summary>
public class PostFxConverter : JsonConverter<PostFx>
{
    private readonly bool _ignoreVersion;
    private readonly IEnumerable<PostFx> _loadedPostFxs;

    public PostFxConverter(IEnumerable<PostFx> postfxs, bool ignoreVersion)
    {
        _loadedPostFxs = postfxs;
        _ignoreVersion = ignoreVersion;
    }

    public override PostFx ReadJson(JsonReader reader, Type objectType, /*[AllowNullAttribute]*/ PostFx existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        (string Name, string Version) = serializer.Deserialize<ValueTuple<string, string>>(reader);
        var tf = _loadedPostFxs.FirstOrDefault(i => i.Name == Name && i.Version == Version);
        if (tf is null && _ignoreVersion)
            tf = _loadedPostFxs.FirstOrDefault(i => i.Name == Name);
        if (tf is null)
            throw new UnknownPluginException(Name, Version);
        return tf;
    }

    public override void WriteJson(JsonWriter writer, /*[AllowNullAttribute]*/ PostFx value, JsonSerializer serializer) => serializer.Serialize(writer, (value.Name, value.Version));
}
