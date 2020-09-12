using IFSEngine.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IFSEngine.Serialization
{
    public static class IfsSerializer
    {
        private static JsonSerializerSettings defaultSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>
            {
                new TransformFunctionConverter(false),
                new IteratorConverter(),
                new IfsConverter()
            },
            ObjectCreationHandling = ObjectCreationHandling.Replace//replace default palette
            //TypeNameHandling = TypeNameHandling.Auto
        };
        //TODO: option for TransformFunctionConverter(true)

        public static IFS LoadJson(string path)
        {
            var ifs = JsonConvert.DeserializeObject<IFS>(File.ReadAllText(path, Encoding.UTF8), defaultSettings);
            return ifs;
        }

        public static void SaveJson(IFS ifs, string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(ifs, defaultSettings), Encoding.UTF8);
        }

        public static void Save(this IFS ifs, string path) => SaveJson(ifs, path);

    }
}
