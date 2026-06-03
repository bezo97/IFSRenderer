using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using IFSEngine.Model;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IFSEngine.Serialization;

public class EffectLayerConverter : JsonConverter<EffectLayer>
{
    private readonly bool _ignoreVersion;
    private readonly IEnumerable<EffectPlugin> _loadedPostFxs;

    public EffectLayerConverter(IEnumerable<EffectPlugin> postfxs, bool ignoreVersion)
    {
        _loadedPostFxs = postfxs;
        _ignoreVersion = ignoreVersion;
    }

    public override EffectLayer ReadJson(JsonReader reader, Type objectType, EffectLayer existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        //(string Name, string Version) = jo.ToObject<ValueTuple<string, string>>(serializer);
        var Name = jo[nameof(EffectPlugin.Name)]?.ToObject<string>(serializer);
        var Version = jo[nameof(EffectPlugin.Version)]?.ToObject<string>(serializer);
        var realParams = jo[nameof(EffectLayer.RealParams)]?.ToObject<Dictionary<string, double>>(serializer) ?? [];
        var vec3Params = jo[nameof(EffectLayer.Vec3Params)]?.ToObject<Dictionary<string, Vector3>>(serializer) ?? [];
        bool enabled = jo.TryGetValue(nameof(EffectLayer.Enabled), out var enabledToken) && enabledToken?.ToObject<bool>(serializer) == true;

        var fx = _loadedPostFxs.FirstOrDefault(i => i.Name == Name && i.Version == Version);
        if (fx is null && _ignoreVersion)
            fx = _loadedPostFxs.FirstOrDefault(i => i.Name == Name);
        if (fx is null)
            throw new UnknownPluginException(Name, Version);

        var instance = jo.ToObject<EffectLayer>();
        instance.SetEffect(fx);
        //merge serialized params with plugin defaults
        foreach (var k in instance.RealParams.Keys.Union(fx.RealParams.Keys).ToList())
            instance.RealParams[k] = realParams.TryGetValue(k, out double val) ? val : fx.RealParams.GetValueOrDefault(k, 0);
        foreach (var k in instance.Vec3Params.Keys.Union(fx.Vec3Params.Keys).ToList())
            instance.Vec3Params[k] = vec3Params.TryGetValue(k, out Vector3 val) ? val : fx.Vec3Params.GetValueOrDefault(k, Vector3.Zero);
        return instance;
    }

    public override void WriteJson(JsonWriter writer, EffectLayer value, JsonSerializer serializer)
    {
        JObject jo = new()
        {
            [nameof(EffectPlugin.Name)] = JToken.FromObject(value.Effect.Name),
            [nameof(EffectPlugin.Version)] = JToken.FromObject(value.Effect.Version),
            [nameof(EffectLayer.Enabled)] = JToken.FromObject(value.Enabled),
            [nameof(EffectLayer.RealParams)] = JToken.FromObject(value.RealParams),
            [nameof(EffectLayer.Vec3Params)] = JToken.FromObject(value.Vec3Params)
        };
        jo.WriteTo(writer);
    }
}
