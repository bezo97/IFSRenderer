using System;
using System.Collections.Generic;

namespace IFSEngine.TransformFunctions
{
    public class Spherical : ITransformFunction
    {
        public string ShaderCode => throw new NotImplementedException();
        public int Id => 1;

        public List<double> GetListOfParams()
        {
            return new List<double>();//0
        }
    }
}
