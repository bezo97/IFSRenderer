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
        private static IteratorConverter iteratorConverter = new IteratorConverter();
        private static IfsConverter ifsConverter = new IfsConverter();

        public static IFS LoadJson(string path, IEnumerable<TransformFunction> transforms, bool ignoreTransformVersions)
        {
            var fileContent = File.ReadAllText(path, Encoding.UTF8);
            var settings = GetJsonSerializerSettings(transforms, ignoreTransformVersions);
            return JsonConvert.DeserializeObject<IFS>(fileContent, settings);
        }

        public static void SaveJson(IFS ifs, string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(ifs, GetJsonSerializerSettings(null, false)), Encoding.UTF8);
        }

        internal static JsonSerializerSettings GetJsonSerializerSettings(IEnumerable<TransformFunction> transforms, bool ignoreVersion)
        {
            return new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>
                {
                    new TransformFunctionConverter(transforms, ignoreVersion),
                    iteratorConverter,
                    ifsConverter
                },
                ObjectCreationHandling = ObjectCreationHandling.Replace//replace default palette
                //TypeNameHandling = TypeNameHandling.Auto
            };
        }


    }
}
