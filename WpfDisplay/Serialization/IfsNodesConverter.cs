using IFSEngine.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using WpfDisplay.Helper;

namespace WpfDisplay.Serialization;

/// <summary>
/// Serializes node positions inside the IFS json while keeping compatibility with the library.
/// </summary>
public class IfsNodeConverter : JsonConverter<IfsNodes>
{
    public override IfsNodes ReadJson(JsonReader reader, Type objectType, IfsNodes existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        IFS ifs = jo.ToObject<IFS>(serializer);
        var ifsnodes = new IfsNodes(ifs);
        if (jo.ContainsKey("Nodes"))
            ifsnodes.Positions = jo["Nodes"].ToObject<List<BindablePoint>>(serializer);
        return ifsnodes;
    }

    public override void WriteJson(JsonWriter writer, IfsNodes value, JsonSerializer serializer)
    {
        JObject jo = JObject.FromObject(value.IFS, serializer);
        jo["Nodes"] = JToken.FromObject(value.Positions, serializer);
        jo.WriteTo(writer, serializer.Converters.ToArray());
    }
}
