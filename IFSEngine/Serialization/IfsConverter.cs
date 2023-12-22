using System;
using System.Collections.Generic;
using System.Linq;

using IFSEngine.Model;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IFSEngine.Serialization;

public class IfsConverter : JsonConverter<IFS>
{
    public override IFS ReadJson(JsonReader reader, Type objectType, /*[AllowNullAttribute]*/ IFS existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        double[][] xaos = jo["xaos"].ToObject<double[][]>(serializer);
        jo.Remove("xaos");
        //iterators
        List<Iterator> iterators = [];
        List<Exception> exceptions = [];
        foreach (var iteratorToken in jo["Iterators"].Children())
        {
            try
            {
                iterators.Add(iteratorToken.ToObject<Iterator>(serializer));
            }
            catch (UnknownTransformException ex) { exceptions.Add(ex); }
        }
        if (exceptions.Count > 0)
            throw new AggregateException("Failed to deserialize iterators", exceptions);
        jo.Remove("Iterators");

        serializer.Converters.Remove(this);
        var ifs = jo.ToObject<IFS>(serializer);
        serializer.Converters.Add(this);
        //set authors
        List<Author> authorsList = jo["Authors"].ToObject<List<Author>>();
        foreach (var author in authorsList)
            ifs.AddAuthor(author);
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
        //the model has the iterators in a set, they must be ordered to serialize
        List<Iterator> list = value.Iterators.ToList();
        double[][] xaos = new double[list.Count][];
        for (int i = 0; i < list.Count; i++)
        {
            xaos[i] = new double[list.Count];
            for (int j = 0; j < list.Count; j++)
            {
                list[i].WeightTo.TryGetValue(list[j], out double weight);//0 if not found
                xaos[i][j] = weight;
            }
        }
        jo["xaos"] = JToken.FromObject(xaos, serializer);
        jo.WriteTo(writer, serializer.Converters.ToArray());
    }
}
