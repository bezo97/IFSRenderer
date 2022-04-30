#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace IFSEngine.Utility;

/// <summary>
/// Reflection utilities to access nested properties.
/// Based on <a href="https://stackoverflow.com/questions/48082626/set-property-in-nested-object-using-reflection">this thread</a>, heavily modified and simplified.
/// </summary>
internal static class NestedReflectionHelper
{
    /// <param name="obj">The root object that the path applies to.</param>
    /// <param name="propertyPath">A path to a property under the root object. Such as "[0].MyDict.[asd]".</param>
    /// <param name="newValue">The new value to be set for the spceified property.</param>
    public static void SetPropertyValueByPath(object obj, string propertyPath, object newValue)
    {
        var pathSegments = new Queue<string>(propertyPath.Split('.'));
        while(pathSegments.TryDequeue(out var segment))
        {
            ParsePropertyPathSegment(segment, out var propName, out var index);
            var propInfo = obj.GetType().GetProperty(propName)!;
            if (pathSegments.Count > 0)
                obj = propInfo.GetValue(obj, index)!;
            else
                propInfo.SetValue(obj, newValue, index);
        }
    }

    private static void ParsePropertyPathSegment(string segment, out string propName, out object?[]? index)
    {
        if (segment.StartsWith('['))
        {//indexer segment
            propName = "Item";
            var indexString = segment.TrimStart('[').TrimEnd(']');
            index = new object?[] { int.TryParse(indexString, out int numIndex) ? numIndex : indexString };
        }
        else
        {//property name segment
            propName = segment;
            index = null;
        }
    }

}
