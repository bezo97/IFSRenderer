using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace IFSEngine.TransformFunctions
{
    /// <summary>
    /// Reflection black magic to enable custom Tranform Function variables.
    /// </summary>
    public static class TransformFunctionExtensions
    {
        //tf -> var name -> var prop
        private static readonly Dictionary<int, SortedDictionary<string, PropertyInfo>> tfs = new Dictionary<int, SortedDictionary<string, PropertyInfo>>();

        public static IEnumerable<string> GetFunctionVariables(this ITransformFunction tf)
        {
            if (tfs.TryGetValue(tf.Id, out var o))
                return o.Select(p => p.Key);
            tfs[tf.Id] = new SortedDictionary<string, PropertyInfo>(
                tf.GetType().GetProperties()
                    //.Where(prop => Attribute.IsDefined(prop, typeof(FunctionVariable)))
                    .Where(prop => prop.PropertyType == typeof(double))
                    .ToDictionary(pi => pi.Name, pi => pi));
            return tfs[tf.Id].Select(p=>p.Key);
        }

        public static void SetVar<T>(this ITransformFunction tf, string varName, T value)
        {
            //tf.GetType().GetProperty(varName).SetValue(tf, value);
            tfs[tf.Id][varName].SetValue(tf, value);
        }

        public static T GetVar<T>(this ITransformFunction tf, string varName)
        {
            //tf.GetType().GetProperty(varName).GetValue(tf);
            return (T) tfs[tf.Id][varName].GetValue(tf);
        }

    }
}
