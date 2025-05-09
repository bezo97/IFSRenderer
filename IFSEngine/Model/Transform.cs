﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IFSEngine.Model;

public partial class Transform
{
    public string Name { get; private set; }
    public string Version { get; private set; }
    public string Description { get; private set; }
    public IReadOnlyList<string> Tags { get; private set; }
    public IReadOnlyList<string> IncludeUses { get; private set; }
    public Uri ReferenceUrl { get; private set; }
    public string SourceCode { get; private set; }
    public IReadOnlyDictionary<string, double> RealParams { get; private set; }//name, default value
    public IReadOnlyDictionary<string, Vector3> Vec3Params { get; private set; }//name, default value
    public string FilePath { get; private set; }


    //These cannot be parameter names:
    private static readonly List<string> _reservedFields = ["Name", "Version", "Description", "Tags", "Reference", "Use"];
    private const string DefaultDescription = "Description not provided by the plugin developer";

    [GeneratedRegex("^(\\s*)@.+:.+$")] //@Param1: 0.0, 0 0 0, min 1
    private static partial Regex fieldMatcher();

    public static async Task<Transform> FromFile(string path)
    {
        string sourceString = await File.ReadAllTextAsync(path);
        Transform tf = FromString(sourceString);
        tf.FilePath = path;
        return tf;
    }

    public static Transform FromString(string s)
    {
        var lines = s.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries)
                     .Select(l => l.Trim());

        var fieldDefinitionLines = lines.Where(l => fieldMatcher().IsMatch(l));
        var fieldKVPs = fieldDefinitionLines.Select(l => (l.Split(":")[0].TrimStart('@').Trim(), l.Split(":", 2)[1].Trim()));
        var includeUses = fieldKVPs.Where(f => f.Item1 == "Use").Select(f => f.Item2).ToList();
        Dictionary<string, string> fields = fieldKVPs.DistinctBy(kvp => kvp.Item1).ToDictionary(kvp => kvp.Item1, kvp => kvp.Item2);

        //check required fields
        if (!fields.ContainsKey("Name") || !fields.ContainsKey("Version"))
            throw new SerializationException($"The plugin must define the @Name and @Version fields.");

        Dictionary<string, string> paramFields = fields
            .Where(p => !_reservedFields.Contains(p.Key))
            .ToDictionary(p => p.Key, p => p.Value);
        Dictionary<string, double> realParams = [];
        Dictionary<string, Vector3> vec3Params = [];
        foreach ((string paramName, string paramDef) in paramFields)
        {
            var paramProps = paramDef.Split(',');
            string defaultValueString = paramProps[0].Trim();
            if (double.TryParse(defaultValueString, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double realDefaultValue))
            {//parse real
                realParams[paramName] = realDefaultValue;
            }
            else if (defaultValueString.Count(c => c == ' ') == 2)
            {//parse vec3
                List<float> vec3Components = defaultValueString
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => float.Parse(l, System.Globalization.CultureInfo.InvariantCulture))
                    .ToList();
                vec3Params[paramName] = new Vector3(vec3Components[0], vec3Components[1], vec3Components[2]);
            }

            //TODO: Handle parts after comma, such as min, max, increment, param description, ..

        }

        string sourceCode = string.Join(Environment.NewLine, lines.Where(l => !fieldDefinitionLines.Contains(l)));
        //replace real params
        foreach ((string pname, double pvalue) in realParams.OrderByDescending(n => n.Key.Length))
        {
            sourceCode = sourceCode.Replace("@" + pname, $"(real_params[iter.real_params_index + {realParams.Keys.ToList().IndexOf(pname)}])");
        }
        //replace vec3 params
        foreach ((string pname, Vector3 pvalue) in vec3Params.OrderByDescending(n => n.Key.Length))
        {
            sourceCode = sourceCode.Replace("@" + pname, $"(vec3_params[iter.vec3_params_index + {vec3Params.Keys.ToList().IndexOf(pname)}].xyz)");
        }

        return new Transform
        {
            Name = fields["Name"],
            Version = fields["Version"],
            Description = fields.TryGetValue("Description", out string descriptionString) ? descriptionString : DefaultDescription,
            Tags = fields.TryGetValue("Tags", out string tagsString) ?
                tagsString
                .Split(',')
                .Select(t => t
                    .Trim()
                    .ToLower(System.Globalization.CultureInfo.InvariantCulture))
                .ToList() : [],
            IncludeUses = includeUses,
            ReferenceUrl = fields.TryGetValue("Reference", out string uriString) ? new Uri(uriString) : null,
            SourceCode = sourceCode,
            RealParams = realParams,
            Vec3Params = vec3Params,
            FilePath = null
        };
    }

    public override bool Equals(object obj)
    {
        if (obj is not Transform)
            return false;
        Transform tf2 = (Transform)obj;
        return Name == tf2.Name && Version == tf2.Version;
    }

    public override int GetHashCode() => (Name + Version).GetHashCode();

    public override string ToString() => $"{Name} ({Version})";
}
