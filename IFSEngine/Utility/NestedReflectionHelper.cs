using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFSEngine.Utility;

/// <summary>
/// Reflection utilities to access nested properties.
/// Based on <a href="https://stackoverflow.com/questions/48082626/set-property-in-nested-object-using-reflection">this thread</a>.
/// </summary>
internal static class NestedReflectionHelper
{

    public static void SetPropertyValueByPath(object obj, string path, object newValue)
    {
        var pathSegments = path.Split('.');

        if (pathSegments.Length == 1)
        {
            SetPropertyValue(obj, pathSegments[0], newValue);
        }
        else
        {
            ////  If more than one remaining segment, recurse
            var child = GetPropertyValue(obj, pathSegments[0]);

            SetPropertyValueByPath(child, String.Join(".", pathSegments.Skip(1)), newValue);
        }
    }

    private static object GetPropertyValue(object obj, string name)
    {
        if (CrackPropertyName(name, out var namePart, out var indexPart))
        {

            var property = obj.GetType().GetProperty(namePart);
            var list = property.GetValue(obj);
            var value = list.GetType().GetProperty("Item").GetValue(list, new object[] { int.Parse(indexPart.ToString()) });
            return value;
        }
        else
        {
            return obj.GetType().GetProperty(namePart).GetValue(obj);
        }

    }

    private static void SetPropertyValue(object obj, string name, object newValue)
    {
        var property = obj.GetType().GetProperty(name);
        property.SetValue(obj, newValue);
    }

    private static bool CrackPropertyName(string name, out string namePart, out object indexPart)
    {
        if (name.Contains('['))
        {
            namePart = name[..name.IndexOf('[', StringComparison.Ordinal)];

            var leftBrace = name.IndexOf('[', StringComparison.Ordinal);
            var rightBrace = name.IndexOf('[', StringComparison.Ordinal);

            indexPart = name.Substring(leftBrace + 1, rightBrace - leftBrace - 1);
            return true;
        }
        else
        {
            namePart = name;
            indexPart = null;
            return false;
        }
    }

}
