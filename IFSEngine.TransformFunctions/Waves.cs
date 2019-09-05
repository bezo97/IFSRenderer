using IFSEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.TransformFunctions
{
    public class Waves : ITransformFunction
    {
        public string ShaderCode => throw new NotImplementedException();

        public int Id => 2;

        double waves_w = 0.999;
        double sx = 0.02;
        double sy = 0.01;
        double sz = -0.04;
        double fx = 20.0;
        double fy = 10.0;
        double fz = 10.0;

        public List<double> GetListOfParams()
        {
            return new List<double>
            {
                waves_w,sx,sy,sz,fx,fy,fz
            };
        }

        public static Waves RandomWaves
        {
            get
            {
                return new Waves
                {
                    waves_w = 0.999,
                    sx = RandHelper.NextDouble() * 0.1,
                    sy = RandHelper.NextDouble() * 0.1,
                    sz = RandHelper.NextDouble() * 0.1,
                    fx = RandHelper.NextDouble() * 20.0,
                    fy = RandHelper.NextDouble() * 20.0,
                    fz = RandHelper.NextDouble() * 20.0,
                };
            }
        }
    }
}
