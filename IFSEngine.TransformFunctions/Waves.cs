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

        public double W { get; set; } = 0.999;
        public double Sx { get; set; } = 0.02;
        public double Sy { get; set; } = 0.01;
        public double Sz { get; set; } = -0.04;
        public double Fx { get; set; } = 20.0;
        public double Fy { get; set; } = 10.0;
        public double Fz { get; set; } = 10.0;


        public static Waves RandomWaves
        {
            get
            {
                return new Waves
                {
                    W = 0.999,
                    Sx = RandHelper.NextDouble() * 0.1,
                    Sy = RandHelper.NextDouble() * 0.1,
                    Sz = RandHelper.NextDouble() * 0.1,
                    Fx = RandHelper.NextDouble() * 20.0,
                    Fy = RandHelper.NextDouble() * 20.0,
                    Fz = RandHelper.NextDouble() * 20.0,
                };
            }
        }
    }
}
