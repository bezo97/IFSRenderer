using System;
using System.Linq;

using IFSEngine.Model;
using IFSEngine.Serialization;

using Newtonsoft.Json;

namespace IFSEngine.Utility;

public static class ModelExtensions
{
    /// <summary>
    /// Extension method on <see cref="IFS"/> to <see cref="SaveJsonFile(IFS, string)"/>
    /// </summary>
    public static void Save(this IFS ifs, string path) => IfsSerializer.SaveJsonFile(ifs, path);

    /// <summary>
    /// Lazy, slow, but maintainable deep clone method that uses serialization.
    /// </summary>
    /// <param name="ifs">The IFS object to be cloned.</param>
    /// <returns>A new deep cloned instance.</returns>
    public static IFS DeepClone(this IFS ifs)
    {
        var transforms = ifs.Iterators.Select(i => i.Transform);
        var settings = IfsSerializer.GetJsonSerializerSettings(transforms, ifs.PostFxs, false);
        string serializedContent = JsonConvert.SerializeObject(ifs, settings);
        return JsonConvert.DeserializeObject<IFS>(serializedContent, settings);
    }

    public static void EvaluateIfs(this IFS ifs, TimeOnly t) => ifs.Dopesheet.EvaluateAt(ifs, t);

}
