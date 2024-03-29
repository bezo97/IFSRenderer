﻿#nullable enable
using System;
using System.Collections.Generic;

namespace IFSEngine.Utility;

/// <summary>
/// Reflection utilities to access nested properties and fields.
/// Based on <a href="https://stackoverflow.com/questions/48082626/set-property-in-nested-object-using-reflection">this thread</a>, heavily modified and simplified.
/// </summary>
internal static class NestedReflectionHelper
{
    /// <param name="obj">The root object that the path applies to.</param>
    /// <param name="memberPath">A path to a property or field under the root object. Such as "[0].MyDict.[asd]".</param>
    /// <param name="newValue">The new value to be set for the specified member.</param>
    public static void SetMemberValueByPath(object obj, string memberPath, object newValue)
    {
        var pathSegments = new Queue<string>(memberPath.Split('.'));
        _ = SetRecursive(obj, pathSegments, newValue);
    }

    private static object SetRecursive(object obj, Queue<string> pathSegments, object newValue)
    {
        if (pathSegments.TryDequeue(out var segment))
        {
            ParseMemberPathSegment(segment, out var memberName, out var index);

            var propInfo = obj.GetType().GetProperty(memberName);
            if (propInfo is not null)
            {//member is a property
                if (pathSegments.Count > 0)
                {
                    var propValue = propInfo.GetValue(obj, index)!;
                    propValue = SetRecursive(propValue, pathSegments, newValue);
                    if (propValue.GetType().IsValueType)
                        propInfo.SetValue(obj, propValue, index);
                }
                else
                    propInfo.SetValue(obj, newValue, index);
            }
            else
            {//member is a field
                var fieldInfo = obj.GetType().GetField(memberName)!;
                if (pathSegments.Count > 0)
                {
                    var fieldValue = fieldInfo.GetValue(obj)!;
                    fieldValue = SetRecursive(fieldValue, pathSegments, newValue);
                    if (fieldValue.GetType().IsValueType)
                        fieldInfo.SetValue(obj, fieldValue);
                }
                else
                    fieldInfo.SetValue(obj, Convert.ToSingle(newValue));//hack: cast double to float
            }
        }
        return obj;
    }

    private static void ParseMemberPathSegment(string segment, out string memberName, out object?[]? index)
    {
        if (segment.StartsWith('['))
        {//property indexer segment
            memberName = "Item";
            var indexString = segment.TrimStart('[').TrimEnd(']');
            index = new object?[] { int.TryParse(indexString, out int numIndex) ? numIndex : indexString };
        }
        else
        {//member name segment
            memberName = segment;
            index = null;
        }
    }

}
