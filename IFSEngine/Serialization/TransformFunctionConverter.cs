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
    /// Only serializes the Name and Version of the TransformFunction.
    /// </summary>
    internal class TransformFunctionConverter : JsonConverter<TransformFunction>
    {
        private readonly bool ignoreVersion;

        public TransformFunctionConverter(bool ignoreVersion)
        {
            this.ignoreVersion = ignoreVersion;
        }

        public override TransformFunction ReadJson(JsonReader reader, Type objectType, /*[AllowNullAttribute]*/ TransformFunction existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            TransformFunction tf;
            (string Name, string Version) = serializer.Deserialize<ValueTuple<string,string>>(reader);
            tf = TransformFunction.LoadedTransformFunctions.FirstOrDefault(i => i.Name == Name && i.Version == Version);//TODO: override and use Equals
            if (tf == null)
            {
                if (ignoreVersion)
                    throw new NotImplementedException();//TODO: prompt to load the transform's different version?
                else
                    throw new SerializationException($"The TransformFunction {Name} (Version: {Version}) is unknown.");
            }
            return tf;
        }

        public override void WriteJson(JsonWriter writer, /*[AllowNullAttribute]*/ TransformFunction value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, (value.Name, value.Version));
        }
    }
}
