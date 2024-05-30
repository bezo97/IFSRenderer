using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using IFSEngine.Model;

using Newtonsoft.Json;

namespace IFSEngine.Serialization;

public static class IfsSerializer
{
    private static readonly IteratorConverter _iteratorConverter = new();
    private static readonly IfsConverter _ifsConverter = new();

    public static IFS LoadJsonFile(string path, IEnumerable<Transform> transforms, bool ignoreTransformVersions)
    {
        string fileContent = File.ReadAllText(path, Encoding.UTF8);
        return DeserializeJsonString(fileContent, transforms, ignoreTransformVersions);
    }

    public static void SaveJsonFile(IFS ifs, string path)
    {
        string fileContent = SerializeJsonString(ifs);
        File.WriteAllText(path, fileContent, Encoding.UTF8);
    }

    public static async Task<IFS> LoadJsonFileAsync(string path, IEnumerable<Transform> transforms, bool ignoreTransformVersions)
    {
        string fileContent = await File.ReadAllTextAsync(path, Encoding.UTF8);
        return DeserializeJsonString(fileContent, transforms, ignoreTransformVersions);
    }

    public static async Task SaveJsonFileAsync(IFS ifs, string path)
    {
        string fileContent = SerializeJsonString(ifs);
        await File.WriteAllTextAsync(path, fileContent, Encoding.UTF8);
    }

    public static IFS DeserializeJsonString(string ifsState, IEnumerable<Transform> transforms, bool ignoreTransformVersions)
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

    internal static JsonSerializerSettings GetJsonSerializerSettings(IEnumerable<Transform> transforms, bool ignoreVersion)
    {
        return new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>
                {
                    new TransformConverter(transforms, ignoreVersion),
                    _iteratorConverter,
                    _ifsConverter
                },
            ObjectCreationHandling = ObjectCreationHandling.Replace//replace default palette
                                                                   //TypeNameHandling = TypeNameHandling.Auto
        };
    }


}
