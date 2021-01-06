using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IFSEngine.Model
{
    public class TransformFunction
    {
        public string Name { get; private set; }
        public string Version { get; private set; }
        public string SourceCode { get; private set; }
        public IReadOnlyDictionary<string, double> Variables { get; private set; }//name, default //type?
        public string FilePath { get; private set; }

        private const string regexVarDef = @"^(\s*)@[\w]+: .+";//@Name: name1

        public static TransformFunction FromFile(string path)
        {
            string sourceString = File.ReadAllText(path);
            var tf = FromString(sourceString);
            tf.FilePath = path;
            return tf;
        }

        public static TransformFunction FromString(string s)
        {
            var lines = s.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(l=>l.Trim());

            string readParam(string pName) => lines
                .First(l => l.StartsWith($"@{pName}: "))
                .Split(new[] { $"@{pName}: " }, StringSplitOptions.RemoveEmptyEntries)[0];

            var variables = lines.Where(l => Regex.IsMatch(l, regexVarDef) && !l.Contains("@Name: ") && !l.Contains("@Version: "))//TODO: vec3              
                .ToDictionary(l => l.Split(' ')[0].TrimStart('@').TrimEnd(':'), l => double.Parse(l.Split(' ')[1], System.Globalization.CultureInfo.InvariantCulture));

            var sourceCode = string.Join(Environment.NewLine, lines.Where(l => !Regex.IsMatch(l, regexVarDef)));
            for (int iVar = 0; iVar < variables.Count; iVar++)
            {
                sourceCode = sourceCode.Replace("@" + variables.Keys.ElementAt(iVar), $"(tfParams[p_cnt + {iVar}])");
            }

            return new TransformFunction
            {
                Name = readParam("Name"),
                Version = readParam("Version"),
                SourceCode = sourceCode,
                Variables = variables,
                FilePath = null
            };
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TransformFunction))//TODO: is not
                return false;
            var tf2 = (TransformFunction)obj;
            return (Name == tf2.Name && Version == tf2.Version);
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
