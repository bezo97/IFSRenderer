using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using IFSEngine.Model;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IFSEngine.Serialization;

public class IteratorConverter : JsonConverter<Iterator>
{
    public override Iterator ReadJson(JsonReader reader, Type objectType, /*[AllowNullAttribute]*/ Iterator existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        var tf = jo["Transform"].ToObject<Transform>(serializer);
        var realParams = jo[nameof(Iterator.RealParams)].ToObject<Dictionary<string, double>>(serializer);
        var vec3Params = jo[nameof(Iterator.Vec3Params)].ToObject<Dictionary<string, Vector3>>(serializer);
        var iterator = jo.ToObject<Iterator>();
        iterator.SetTransform(tf);
        foreach (var k in iterator.RealParams.Keys.Union(tf.RealParams.Keys).ToList())
            iterator.RealParams[k] = realParams.TryGetValue(k, out double val) ? val : tf.RealParams[k];
        foreach (var k in iterator.Vec3Params.Keys.Union(tf.Vec3Params.Keys).ToList())
            iterator.Vec3Params[k] = vec3Params.TryGetValue(k, out Vector3 val) ? val : tf.Vec3Params[k];
        return iterator;
    }

    public override bool CanWrite => false;//use default
    public override void WriteJson(JsonWriter writer, /*[AllowNullAttribute]*/ Iterator value, JsonSerializer serializer) => throw new NotImplementedException();
}
