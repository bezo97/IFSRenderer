using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using IFSEngine.Model;
using IFSEngine.Serialization;

using Newtonsoft.Json;

namespace WpfDisplay.Serialization;

/// <summary>
/// Similar to <see cref="IfsSerializer"/>, but also adds node positions to the json.
/// </summary>
public static class IfsNodesSerializer
{
    private static readonly IteratorConverter _iteratorConverter = new();
    private static readonly IfsConverter _ifsConverter = new();
    private static readonly IfsNodeConverter _conv = new();

    public static IFS LoadJsonFile(string path, IEnumerable<Transform> transforms, IEnumerable<PostFx> postfxs, bool ignorePluginVersions)
    {
        string fileContent = File.ReadAllText(path, Encoding.UTF8);
        return DeserializeJsonString(fileContent, transforms, postfxs, ignorePluginVersions);
    }

    public static void SaveJsonFile(IFS ifs, string path)
    {
        string fileContent = SerializeJsonString(ifs);
        File.WriteAllText(path, fileContent, Encoding.UTF8);
    }

    public static async Task<IFS> LoadJsonFileAsync(string path, IEnumerable<Transform> transforms, IEnumerable<PostFx> postfxs, bool ignorePluginVersions)
    {
        string fileContent = await File.ReadAllTextAsync(path, Encoding.UTF8);
        return DeserializeJsonString(fileContent, transforms, postfxs, ignorePluginVersions);
    }

    public static async Task SaveJsonFileAsync(IFS ifs, string path)
    {
        string fileContent = SerializeJsonString(ifs);
        await File.WriteAllTextAsync(path, fileContent, Encoding.UTF8);
    }

    public static IFS DeserializeJsonString(string ifsState, IEnumerable<Transform> transforms, IEnumerable<PostFx> postfxs, bool ignorePluginVersions)
    {
        JsonSerializerSettings settings = GetJsonSerializerSettings(transforms, postfxs, ignorePluginVersions);
        try
        {
            IFS result = JsonConvert.DeserializeObject<IfsNodes>(ifsState, settings).IFS;
            return result;
        }
        catch (Exception ex)
        {//Custom JsonConverters may throw different Exceptions
            throw new SerializationException("Serialization Exception", ex);
        }
    }

    public static string SerializeJsonString(IFS ifs)
    {
        JsonSerializerSettings settings = GetJsonSerializerSettings(null, null, false);
        return JsonConvert.SerializeObject(new IfsNodes(ifs), settings);
    }

    internal static JsonSerializerSettings GetJsonSerializerSettings(IEnumerable<Transform> transforms, IEnumerable<PostFx> postfxs, bool ignoreVersion)
    {
        return new JsonSerializerSettings
        {
            Converters =
                [
                    new TransformConverter(transforms, ignoreVersion),
                    new PostFxConverter(postfxs, ignoreVersion),
                    _iteratorConverter,
                    _ifsConverter,
                    _conv
                ],
            ObjectCreationHandling = ObjectCreationHandling.Replace//replace default palette
                                                                   //TypeNameHandling = TypeNameHandling.Auto
        };
    }
}
