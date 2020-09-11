using IFSEngine.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFSEngine.Serialization
{
    internal class IteratorConverter : JsonConverter<Iterator>
    {
        public override Iterator ReadJson(JsonReader reader, Type objectType, /*[AllowNullAttribute]*/ Iterator existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            var tf = jo["TransformFunction"].ToObject<TransformFunction>(serializer);
            var vars = jo["TransformVariables"].ToObject<Dictionary<string, double>>(serializer);
            var iterator = jo.ToObject<Iterator>();
            iterator.SetTransformFunction(tf);
            foreach (var k in iterator.TransformVariables.Keys.ToList())
                iterator.TransformVariables[k] = vars[k];
            return iterator;
        }

        public override bool CanWrite => false;//use default
        public override void WriteJson(JsonWriter writer, /*[AllowNullAttribute]*/ Iterator value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
