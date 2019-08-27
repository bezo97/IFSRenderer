using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.TransformFunctions
{
    public class Waves : ITransformFunction
    {
        public string ShaderCode => throw new NotImplementedException();

        public int Id => 2;

        public List<double> GetListOfParams()
        {
            return new List<double>();//
        }
    }
}
