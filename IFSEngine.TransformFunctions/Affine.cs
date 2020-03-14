using IFSEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.TransformFunctions
{
    public class Affine : ITransformFunction
    {
        public string ShaderCode => throw new NotImplementedException();
        public int Id => 0;

        public double OX { get; set; }
        public double OY { get; set; }
        public double OZ { get; set; }

        public double XX { get; set; } = 1;
        public double XY { get; set; }
        public double XZ { get; set; }

        public double YX { get; set; }
        public double YY { get; set; } = 1;
        public double YZ { get; set; }

        public double ZX { get; set; }
        public double ZY { get; set; }
        public double ZZ { get; set; } = 1;

        public static Affine RandomAffine
        {
            get
            {
                return new Affine
                {
                    OX = (RandHelper.NextDouble() * 2 - 1) * 1.1,
                    OY = (RandHelper.NextDouble() * 2 - 1) * 1.1,
                    OZ = (RandHelper.NextDouble() * 2 - 1) * 1.1,
                    XX = (RandHelper.NextDouble() * 2 - 1) * 1.1,
                    XY = (RandHelper.NextDouble() * 2 - 1) * 1.1,
                    XZ = (RandHelper.NextDouble() * 2 - 1) * 1.1,
                    YX = (RandHelper.NextDouble() * 2 - 1) * 1.1,
                    YY = (RandHelper.NextDouble() * 2 - 1) * 1.1,
                    YZ = (RandHelper.NextDouble() * 2 - 1) * 1.1,
                    ZX = (RandHelper.NextDouble() * 2 - 1) * 1.1,
                    ZY = (RandHelper.NextDouble() * 2 - 1) * 1.1,
                    ZZ = (RandHelper.NextDouble() * 2 - 1) * 1.1
                };  
            }       
        }           

    }
}
