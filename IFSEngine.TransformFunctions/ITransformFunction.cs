using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.TransformFunctions
{
    public interface ITransformFunction
    {
        [JsonIgnore]
        string ShaderCode { get; }
        int Id { get; }//TODO: replace

    }
}
