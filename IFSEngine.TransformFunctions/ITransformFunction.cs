using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.TransformFunctions
{
    public interface ITransformFunction
    {
        string ShaderCode { get; }
        int Id { get; }//TODO: replace
        List<double> GetListOfParams();

    }
}
