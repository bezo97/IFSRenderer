using IFSEngine.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace IFSEngine.Serialization
{
    public static class IfsSerializer
    {
        private static readonly IteratorConverter iteratorConverter = new();
        private static readonly IfsConverter ifsConverter = new();

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
            try
            {
                IFS result = JsonConvert.DeserializeObject<IFS>(ifsState, settings);
                return result;
            }
            catch (Exception ex)
            {//Custom JsonConverters may throw different Exceptions
                throw new SerializationException("Serialization Exception", ex);
            }
        }

        public static string SerializeJsonString(IFS ifs)
        {
            JsonSerializerSettings settings = GetJsonSerializerSettings(null, false);
            return JsonConvert.SerializeObject(ifs, settings);
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
