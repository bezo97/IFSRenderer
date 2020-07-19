using IFSEngine.Helper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace IFSEngine.Model.Camera
{
    //http://www.opengl-tutorial.org/intermediate-tutorials/tutorial-17-quaternions/
    //order: x pitch, y yaw, z roll

    public class QuatCamera : CameraBase
    {
        public Quaternion Orientation { get; set; }

        public QuatCamera()
        {
            Orientation = Quaternion.CreateFromYawPitchRoll((float)-Math.PI / 2.0f, (float)-Math.PI / 2.0f, 0.00001f);//no rotation
            UpdateCamera();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rotVector">Yaw, pitch and roll delta values relative to <see cref="CameraBase.RotationSensitivity"/></param>
        public override void RotateWithSensitivity(Vector3 rotVector)
        {
            float rotSpeed = RotationSensitivity * FOV / 180.0f;
            RotateBy(rotSpeed * rotVector);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rotVector">Yaw, pitch and roll delta in radians</param>
        public override void RotateBy(Vector3 rotVector)
        {
            Quaternion rotq = Quaternion.CreateFromYawPitchRoll(rotVector.X, rotVector.Y, rotVector.Z);
            Orientation = rotq * Orientation;//order matters
            Orientation = Quaternion.Normalize(Orientation);
        }

        //https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles
        private (double yaw, double pitch, double roll) ToEulerAngles(Quaternion q)
        {
            double yaw, pitch, roll;
            // pitch (x-axis rotation)
            double sinr_cosp = +2.0 * (q.W * q.X + q.Y * q.Z);
            double cosr_cosp = +1.0 - 2.0 * (q.X * q.X + q.Y * q.Y);
            pitch = Math.Atan2(sinr_cosp, cosr_cosp);

            // yaw (y-axis rotation)
            double sinp = +2.0 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sinp) >= 1)
                yaw = (3.141592 / 2.0) * (sinp / Math.Abs(sinp)); // use 90 degrees if out of range
            else
                yaw = Math.Asin(sinp);

            // roll (z-axis rotation)
            double siny_cosp = +2.0 * (q.W * q.Z + q.X * q.Y);
            double cosy_cosp = +1.0 - 2.0 * (q.Y * q.Y + q.Z * q.Z);
            roll = Math.Atan2(siny_cosp, cosy_cosp);

            return (yaw, pitch, roll);
        }

        protected override void RefreshCameraValues()
        {
            (double yaw, double pitch, double roll) = ToEulerAngles(Orientation);

            float a = (float)yaw;
            float c = (float)pitch;
            float b = (float)roll;

            //trig
            float cosa = (float)Math.Cos(a);
            float sina = (float)Math.Sin(a);
            float cosb = (float)Math.Cos(b);
            float sinb = (float)Math.Sin(b);
            float cosc = (float)Math.Cos(c);
            float sinc = (float)Math.Sin(c);

            forward = new Vector3(
                cosc * cosa,
                sinc * cosa,
                -sina
            );
            up = new Vector3(
                -sinc * cosb + cosc * sina * sinb,
                cosc * cosb + sinc * sina * sinb,
                cosa * sinb
            );
            right = new Vector3(
                sinc * sinb + cosc * sina * cosb,
                -cosc * sinb + sinc * sina * cosb,
                cosa * cosb
            );

        }

        protected override void SetViewProjMatrix()
        {
            Params.viewProjMatrix = Matrix4x4.CreateLookAt(position, position + forward, up) * projectionMatrix;
        }
    }
}
