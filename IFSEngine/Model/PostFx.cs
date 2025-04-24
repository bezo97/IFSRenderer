using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace IFSEngine.Model;

/// <summary>
/// Represents a single post-effect shader in the pipeline.
/// </summary>
public class PostFx
{
    public string Name { get; private set; }
    public string Version { get; private set; }
    public string Description { get; private set; }
    public IReadOnlyList<string> IncludeUses { get; private set; }
    public string SourceCode { get; private set; }
    public IReadOnlyDictionary<string, double> RealParams { get; private set; }//name, default value
    public IReadOnlyDictionary<string, Vector3> Vec3Params { get; private set; }//name, default value
    public string FilePath { get; private set; }

    /// TODO: implement similar to Transform.cs - might need to refactor some parts

}
