using IFSEngine.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace IFSEngine.Serialization
{
    internal class IteratorConverter : JsonConverter<Iterator>
    {
        public override Iterator ReadJson(JsonReader reader, Type objectType, /*[AllowNullAttribute]*/ Iterator existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            var tf = jo["Transform"].ToObject<Transform>(serializer);
            var realParams = jo[nameof(Iterator.RealParams)].ToObject<Dictionary<string, double>>(serializer);
            var vec3Params = jo[nameof(Iterator.Vec3Params)].ToObject<Dictionary<string, Vector3>>(serializer);
            var iterator = jo.ToObject<Iterator>();
            iterator.SetTransform(tf);
            foreach (var k in iterator.RealParams.Keys.ToList())
                iterator.RealParams[k] = realParams[k];
            foreach (var k in iterator.Vec3Params.Keys.ToList())
                iterator.Vec3Params[k] = vec3Params[k];
            return iterator;
        }

        public override bool CanWrite => false;//use default
        public override void WriteJson(JsonWriter writer, /*[AllowNullAttribute]*/ Iterator value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
