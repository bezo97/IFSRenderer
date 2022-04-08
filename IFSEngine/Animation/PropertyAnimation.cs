using System;
using System.Collections.Generic;
using System.Linq;

namespace IFSEngine.Animation;

public class PropertyAnimation//double
{
    public string[] PropertyPath { get; set; }
    public Channel Channel { get; set; }

    public void EvaluateAt(object target, double t)
    {
        var value = Channel.EvaluateAt(t);
        SetPropertyValue(target, PropertyPath, value);
    }

    private static double GetPropertyValue(object root, string[] propertyPath)
    {
        if (root == null) throw new ArgumentException("Value cannot be null.", nameof(root));
        if (propertyPath == null) throw new ArgumentException("Value cannot be null.", nameof(propertyPath));
        while (propertyPath.Length > 1)
        {
            root = root.GetType().GetProperty(propertyPath[0]).GetValue(root);
            propertyPath = propertyPath.Skip(1).ToArray();
        }
        return (double)root;
    }

    private static void SetPropertyValue(object root, string[] propertyPath, double newValue)
    {
        if (root == null) throw new ArgumentException("Value cannot be null.", nameof(root));
        if (propertyPath == null) throw new ArgumentException("Value cannot be null.", nameof(propertyPath));
        while (propertyPath.Length > 2)
        {
            root = root.GetType().GetProperty(propertyPath[0]).GetValue(root);
            propertyPath = propertyPath.Skip(1).ToArray();
        }
        root.GetType().GetProperty(propertyPath[0]).SetValue(root, newValue);
    }

}
