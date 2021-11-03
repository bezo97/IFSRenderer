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
            var tf = jo["Transform"].ToObject<Transform>(serializer);
            var vars = jo["Variables"].ToObject<Dictionary<string, double>>(serializer);
            var iterator = jo.ToObject<Iterator>();
            iterator.SetTransform(tf);
            foreach (var k in iterator.Variables.Keys.ToList())
                iterator.Variables[k] = vars[k];
            return iterator;
        }

        public override bool CanWrite => false;//use default
        public override void WriteJson(JsonWriter writer, /*[AllowNullAttribute]*/ Iterator value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
