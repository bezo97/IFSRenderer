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
        private static JsonSerializerSettings ignoreTfVersionsSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>
            {
                new TransformFunctionConverter(true),
                new IteratorConverter(),
                new IfsConverter()
            },
            ObjectCreationHandling = ObjectCreationHandling.Replace//replace default palette
            //TypeNameHandling = TypeNameHandling.Auto
        };

        public static IFS LoadJson(string path, bool ignoreTransformVersions)
        {
            JsonSerializerSettings settings = defaultSettings;
            if (ignoreTransformVersions) 
                settings = ignoreTfVersionsSettings;
            return JsonConvert.DeserializeObject<IFS>(File.ReadAllText(path, Encoding.UTF8), settings);
        }

        public static void SaveJson(IFS ifs, string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(ifs, defaultSettings), Encoding.UTF8);
        }

        public static void Save(this IFS ifs, string path) => SaveJson(ifs, path);

    }
}
