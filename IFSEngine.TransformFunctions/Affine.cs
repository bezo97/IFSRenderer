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

        public double ox;
        public double oy;
        public double oz;
               
        public double xx = 1;
        public double xy;
        public double xz;
               
        public double yx;
        public double yy = 1;
        public double yz;
               
        public double zx;
        public double zy;
        public double zz = 1;

        public List<double> GetListOfParams()
        {
            return new List<double>
            {
                ox,oy,oz,xx,xy,xz,yx,yy,yz,zx,zy,zz
            };
        }

        public static Affine RandomAffine
        {
            get
            {
                return new Affine
                {
                    ox = (RandHelper.NextDouble() * 2 - 1) * 1.5,
                    oy = (RandHelper.NextDouble() * 2 - 1) * 1.5,
                    oz = (RandHelper.NextDouble() * 2 - 1) * 1.5,
                    xx = (RandHelper.NextDouble() * 2 - 1) * 1.5,
                    xy = (RandHelper.NextDouble() * 2 - 1) * 1.5,
                    xz = (RandHelper.NextDouble() * 2 - 1) * 1.5,
                    yx = (RandHelper.NextDouble() * 2 - 1) * 1.5,
                    yy = (RandHelper.NextDouble() * 2 - 1) * 1.5,
                    yz = (RandHelper.NextDouble() * 2 - 1) * 1.5,
                    zx = (RandHelper.NextDouble() * 2 - 1) * 1.5,
                    zy = (RandHelper.NextDouble() * 2 - 1) * 1.5,
                    zz = (RandHelper.NextDouble() * 2 - 1) * 1.5
                };
            }
        }

    }
}
