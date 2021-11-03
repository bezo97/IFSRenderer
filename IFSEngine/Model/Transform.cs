using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace IFSEngine.Model
{
    public class Transform
    {
        public string Name { get; private set; }
        public string Version { get; private set; }
        public string Description { get; private set; }
        public IEnumerable<string> Tags { get; private set; }
        public Uri ReferenceUrl { get; private set; }
        public string SourceCode { get; private set; }
        public IReadOnlyDictionary<string, double> Variables { get; private set; }//name, default //type?
        public string FilePath { get; private set; }


        //These cannot be variable names:
        private static readonly List<string> reservedFields = new() { "Name", "Version", "Description", "Tags", "Reference" };
        private const string regexVarDef = @"^(\s*)@.+:.+$";//@Var1: 0.0, 0 0 0, min 1
        private const string defaultDescription = "Description not provided by the plugin developer";

        public static Transform FromFile(string path)
        {
            string sourceString = File.ReadAllText(path);
            Transform tf = FromString(sourceString);
            tf.FilePath = path;
            return tf;
        }

        public static Transform FromString(string s)
        {
            var lines = s.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(l => l.Trim());

            var fieldDefinitionLines = lines.Where(l => Regex.IsMatch(l, regexVarDef));
            Dictionary<string, string> fields = fieldDefinitionLines.ToDictionary(
                l => l.Split(":")[0].TrimStart('@').Trim(),
                l => l.Split(":", 2)[1].Trim());

            Dictionary<string, string> variableFields = fields
                .Where(p => !reservedFields.Contains(p.Key))
                .ToDictionary(p => p.Key, p => p.Value);
            Dictionary<string, double> realVariables = new();
            Dictionary<string, Vector3> vec3Variables = new();
            foreach ((string varName, string varDef) in variableFields)
            {
                var variableProps = varDef.Split(',');
                string defaultValueString = variableProps[0].Trim();
                if (double.TryParse(defaultValueString, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double realDefaultValue))
                {//parse real
                    realVariables[varName] = realDefaultValue;
                }
                else if (defaultValueString.Count(c => c == ' ') == 2)
                {//parse vec3
                    List<float> vec3Components = defaultValueString
                        .Split(' ')
                        .Select(l => float.Parse(l, System.Globalization.CultureInfo.InvariantCulture))
                        .ToList();
                    vec3Variables[varName] = new Vector3(vec3Components[0], vec3Components[1], vec3Components[2]);
                }

                //TODO: Handle parts after comma, such as min, max, increment, variable description, ..

            }

            string sourceCode = string.Join(Environment.NewLine, lines.Except(fieldDefinitionLines));
            //replace real variables
            for (int iVar = 0; iVar < realVariables.Count; iVar++)
            {
                sourceCode = sourceCode.Replace("@" + realVariables.Keys.ElementAt(iVar), $"(tfParams[p_cnt + {iVar}])");
            }
            //TODO: replace vec3 variables
            //for (int iVar = 0; iVar < vec3Variables.Count; iVar++)
            //{
            //    sourceCode = sourceCode.Replace("@" + vec3Variables.Keys.ElementAt(iVar), $"(vars_vec3[p_cnt2 + {iVar}])");
            //}

            return new Transform
            {
                Name = fields["Name"],
                Version = fields["Version"],
                Description = fields.TryGetValue("Description", out string descriptionString) ? descriptionString : defaultDescription,
                Tags = fields.TryGetValue("Tags", out string tagsString) ?
                    tagsString
                    .Split(',')
                    .Select(t => t
                        .Trim()
                        .ToLower(System.Globalization.CultureInfo.InvariantCulture))
                    .ToList() : new List<string>(),
                ReferenceUrl = fields.TryGetValue("Reference", out string uriString) ? new Uri(uriString): null,
                SourceCode = sourceCode,
                Variables = realVariables,
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

        public override int GetHashCode()
        {
            return (Name + Version).GetHashCode();
        }

        public override string ToString()
        {
            return $"{Name} ({Version})";
        }

    }
}
