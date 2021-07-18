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

        public static IFS LoadJsonFile(string path, IEnumerable<TransformFunction> transforms, bool ignoreTransformVersions)
        {
            string fileContent = File.ReadAllText(path, Encoding.UTF8);
            return DeserializeJsonString(fileContent, transforms, ignoreTransformVersions);
        }

        public static void SaveJsonFile(IFS ifs, string path)
        {
            string fileContent = SerializeJsonString(ifs);
            File.WriteAllText(path, fileContent, Encoding.UTF8);
        }

        public static IFS DeserializeJsonString(string ifsState, IEnumerable<TransformFunction> transforms, bool ignoreTransformVersions)
        {
            JsonSerializerSettings settings = GetJsonSerializerSettings(transforms, ignoreTransformVersions);
            return JsonConvert.DeserializeObject<IFS>(ifsState, settings);
        }

        public static string SerializeJsonString(IFS ifs)
        {
            JsonSerializerSettings settings = GetJsonSerializerSettings(null, false);
            return JsonConvert.SerializeObject(ifs, settings);
        }

        /// <summary>
        /// Extension method on <see cref="IFS"/> to <see cref="SaveJsonFile(IFS, string)"/>
        /// </summary>
        public static void Save(this IFS ifs, string path) => SaveJsonFile(ifs, path);

        private static JsonSerializerSettings GetJsonSerializerSettings(IEnumerable<TransformFunction> transforms, bool ignoreVersion)
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
