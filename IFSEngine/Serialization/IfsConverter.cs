using IFSEngine.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace IFSEngine.Serialization
{
    internal class IfsConverter : JsonConverter<IFS>
    {
        public override IFS ReadJson(JsonReader reader, Type objectType, /*[AllowNullAttribute]*/ IFS existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            double[][] xaos = jo["xaos"].ToObject<double[][]>(serializer);
            jo.Remove("xaos");
            IEnumerable<Iterator> iterators = jo["Iterators"].ToObject<IEnumerable<Iterator>>(serializer);
            jo.Remove("Iterators");
            serializer.Converters.Remove(this);
            var ifs = jo.ToObject<IFS>(serializer);
            serializer.Converters.Add(this);
            //set iterators
            foreach (var i in iterators)
                ifs.AddIterator(i, false);
            //set xaos weights
            var iters = ifs.Iterators.ToList();
            for (int i = 0; i < xaos.GetLength(0); i++)
            {
                iters[i].WeightTo = iters.ToDictionary(k => k, v => xaos[i][iters.IndexOf(v)]);
            }
            return ifs;
        }

        public override void WriteJson(JsonWriter writer, /*[AllowNullAttribute]*/ IFS value, JsonSerializer serializer)
        {
            serializer.Converters.Remove(this);
            JObject jo = JObject.FromObject(value, serializer);
            serializer.Converters.Add(this);
            foreach (var i in value.Iterators)
                foreach (var j in value.Iterators)
                {//hack to fill 0 weights.. TODO
                    if (!i.WeightTo.TryGetValue(j, out _))
                        i.WeightTo[j] = 0.0;
                }
            jo["xaos"] = JToken.FromObject(value.Iterators.Select(i=>i.WeightTo.Values.ToArray()).ToArray(), serializer);
            jo.WriteTo(writer, serializer.Converters.ToArray());            
        }
    }
}
