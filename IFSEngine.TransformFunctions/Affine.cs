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

        public static Affine RandomAffine()
        {
            double ox = (RandHelper.NextDouble() * 2 - 1) * 3.0;
            double oy = (RandHelper.NextDouble() * 2 - 1) * 3.0;
            double oz = (RandHelper.NextDouble() * 2 - 1) * 3.0;
            return new Affine
            {
                OX = ox,
                OY = oy,
                OZ = oz,
                XX = ox + (RandHelper.NextDouble() * 2 - 1) * 1.1,
                XY = oy + (RandHelper.NextDouble() * 2 - 1) * 1.1,
                XZ = oz + (RandHelper.NextDouble() * 2 - 1) * 1.1,
                YX = ox + (RandHelper.NextDouble() * 2 - 1) * 1.1,
                YY = oy + (RandHelper.NextDouble() * 2 - 1) * 1.1,
                YZ = oz + (RandHelper.NextDouble() * 2 - 1) * 1.1,
                ZX = ox + (RandHelper.NextDouble() * 2 - 1) * 1.1,
                ZY = oy + (RandHelper.NextDouble() * 2 - 1) * 1.1,
                ZZ = oz + (RandHelper.NextDouble() * 2 - 1) * 1.1
            };
        }

    }
}
