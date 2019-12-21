using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.TransformFunctions
{
    public class Foci : ITransformFunction
    {
        public string ShaderCode => throw new NotImplementedException();

        public int Id => 3;

        public List<double> GetListOfParams()
        {
            return new List<double>();//0
        }
    }
}
