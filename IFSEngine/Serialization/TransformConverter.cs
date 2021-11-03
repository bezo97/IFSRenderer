using IFSEngine.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace IFSEngine.Serialization
{
    /// <summary>
    /// Only serializes the Name and Version of the Transform.
    /// </summary>
    internal class TransformConverter : JsonConverter<Transform>
    {
        private readonly bool ignoreVersion;
        private readonly IEnumerable<Transform> loadedTransforms;

        public TransformConverter(IEnumerable<Transform> transforms, bool ignoreVersion)
        {
            this.loadedTransforms = transforms;
            this.ignoreVersion = ignoreVersion;
        }

        public override Transform ReadJson(JsonReader reader, Type objectType, /*[AllowNullAttribute]*/ Transform existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            Transform tf;
            (string Name, string Version) = serializer.Deserialize<ValueTuple<string,string>>(reader);
            tf = loadedTransforms.FirstOrDefault(i => i.Name == Name && i.Version == Version);//TODO: override and use Equals
            if (tf == null)
            {
                if (ignoreVersion)
                    return loadedTransforms.FirstOrDefault(i => i.Name == Name);
                else
                    throw new SerializationException($"The Transform '{Name}' (Version: {Version}) is unknown.");
            }
            return tf;
        }

        public override void WriteJson(JsonWriter writer, /*[AllowNullAttribute]*/ Transform value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, (value.Name, value.Version));
        }
    }
}
