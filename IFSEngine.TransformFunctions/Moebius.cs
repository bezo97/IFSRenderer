using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using IFSEngine.Util;

namespace IFSEngine.TransformFunctions
{
    public class Moebius : ITransformFunction
    {
        public string ShaderCode => throw new NotImplementedException();

        public int Id => 5;

        public double Alpha { get; set; } = 1.0;
        public double AX { get; set; } = 0.0;
        public double AY { get; set; } = 0.0;
        public double AZ { get; set; } = 0.0;
        public double BX { get; set; } = 0.0;
        public double BY { get; set; } = 0.0;
        public double BZ { get; set; } = 0.0;
        public double Yaw { get; set; } = 0.0;
        public double Pitch { get; set; } = 0.0;
        public double Roll { get; set; } = 0.0;

        //TODO: rework params
        public double M11 => m.M11;
        public double M12 => m.M12;
        public double M13 => m.M13;
        public double M21 => m.M21;
        public double M22 => m.M22;
        public double M23 => m.M23;
        public double M31 => m.M31;
        public double M32 => m.M32;
        public double M33 => m.M33;

        private System.Numerics.Matrix4x4 m => Matrix4x4.CreateFromYawPitchRoll((float)Yaw, (float)Pitch, (float)Roll);

        public static Moebius RandomMoebius
        {
            get
            {
                return new Moebius
                {
                    Alpha = RandHelper.NextDouble()*4-2,
                    AX = RandHelper.NextDouble()*4-2,
                    AY = RandHelper.NextDouble()*4-2,
                    AZ = RandHelper.NextDouble()*4-2,
                    BX = RandHelper.NextDouble()*4-2,
                    BY = RandHelper.NextDouble()*4-2,
                    BZ = RandHelper.NextDouble() * 4 - 2,
                    Yaw = RandHelper.NextDouble() * Math.PI * 2,
                    Pitch = RandHelper.NextDouble() * Math.PI,
                    Roll = RandHelper.NextDouble() * Math.PI * 2
                };
            }
        }


    }
}
