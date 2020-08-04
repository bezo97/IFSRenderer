using IFSEngine.Helper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using IFSEngine.Model.GpuStructs;

namespace IFSEngine.Model
{
    //http://www.opengl-tutorial.org/intermediate-tutorials/tutorial-17-quaternions/
    //order: x pitch, y yaw, z roll

    public class Camera
    {
        public Quaternion Orientation { get; set; } = Quaternion.CreateFromYawPitchRoll(/*(float)-Math.PI / 2.0f, (float)-Math.PI / 2.0f, 0.00001f*/0,0,0);//no rotation
        public Vector3 Position { get; set; } = new Vector3(0.0f, 0.0f, -2.0f);
        public float TranslationSensitivity { get; set; } = 0.005f;
        public float RotationSensitivity { get; set; } = 0.01f;

        
        public Vector3 rightDirection = new Vector3(1.0f, 0.0f, 0.0f);
        public Vector3 upDirection = new Vector3(0.0f, 1.0f, 0.0f);
        public Vector3 forwardDirection = new Vector3(0.0f, 0.0f, 1.0f);

        /// <summary>
        /// Vertical field of view in degrees. Ranges from 0 to 180 exclusive.
        /// </summary>
        public float FieldOfView { get; set; } = 60;

        /// <summary>
        /// Moves camera position by a translate vector given in camera space.
        /// The vector is multiplied by <see cref="TranslationSensitivity"/>
        /// </summary>
        /// <param name="translateVector"></param>
        public void TranslateWithSensitivity(Vector3 translateVector)
        {
            translateVector *= TranslationSensitivity;
            Position += rightDirection * translateVector.X
                     + upDirection * translateVector.Y
                     + forwardDirection * translateVector.Z;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rotVector">Yaw, pitch and roll delta values relative to <see cref="CameraBase.RotationSensitivity"/></param>
        public void RotateWithSensitivity(Vector3 rotVector)
        {
            float rotSpeed = RotationSensitivity * FieldOfView / 180.0f;
            RotateBy(rotSpeed * rotVector);
        }

        /// <summary>
        /// Rotates the camera direction by the specified angles.
        /// </summary>
        /// <param name="rotVector">Euler angle (Yaw, Pitch, Roll) deltas in radians.</param>
        public void RotateBy(Vector3 rotVector)
        {
            Quaternion rotq = Quaternion.CreateFromYawPitchRoll(rotVector.X,rotVector.Y,rotVector.Z);
            Orientation = rotq * Orientation;//order matters
            Orientation = Quaternion.Normalize(Orientation);
            RefreshCameraValues();
        }

        /// <summary>
        /// Converts a quaternion into Euler angles (Yaw, Pitch, Roll).
        /// https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles
        /// </summary>
        private Vector3 ToEulerAngles(Quaternion q)
        {
            // pitch (x-axis rotation)
            double sinr_cosp = +2.0 * (q.W * q.X + q.Y * q.Z);
            double cosr_cosp = +1.0 - 2.0 * (q.X * q.X + q.Y * q.Y);
            double pitch = Math.Atan2(sinr_cosp, cosr_cosp);

            // yaw (y-axis rotation)
            double yaw;
            double sinp = +2.0 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sinp) >= 1)
                yaw = (3.141592 / 2.0) * (sinp / Math.Abs(sinp)); // use 90 degrees if out of range
            else
                yaw = Math.Asin(sinp);

            // roll (z-axis rotation)
            double siny_cosp = +2.0 * (q.W * q.Z + q.X * q.Y);
            double cosy_cosp = +1.0 - 2.0 * (q.Y * q.Y + q.Z * q.Z);
            double roll = Math.Atan2(siny_cosp, cosy_cosp);

            return new Vector3((float)yaw, (float)pitch, (float)roll);
        }

        protected void RefreshCameraValues()
        {
            Vector3 rot = ToEulerAngles(Orientation);
            float yaw = rot.X;
            float pitch = rot.Y;
            float roll = rot.Z;

            float cosYaw = (float)Math.Cos(yaw);
            float sinYaw = (float)Math.Sin(yaw);
            float cosPitch = (float)Math.Cos(pitch);
            float sinPitch = (float)Math.Sin(pitch);
            float cosRoll = (float)Math.Cos(roll);
            float sinRoll = (float)Math.Sin(roll);

            forwardDirection = new Vector3(
                cosPitch * cosYaw,
                sinPitch * cosYaw,
                -sinYaw
            );
            upDirection = new Vector3(
                -sinPitch * cosRoll + cosPitch * sinYaw * sinRoll,
                cosPitch * cosRoll + sinPitch * sinYaw * sinRoll,
                cosYaw * sinRoll
            );
            rightDirection = new Vector3(
                sinPitch * sinRoll + cosPitch * sinYaw * cosRoll,
                -cosPitch * sinRoll + sinPitch * sinYaw * cosRoll,
                cosYaw * cosRoll
            );

        }
        private Matrix4x4 GetViewProjectionMatrix()
        {
            var viewMatrix = Matrix4x4.CreateLookAt(Position, Position + forwardDirection, upDirection);
            var projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(NumericExtensions.ToRadians(1 + FieldOfView % 180), 1.0f, 0.2f, 100.0f);
            return viewMatrix * projectionMatrix;
        }

        internal CameraStruct GetCameraParameters()
        {
            return new CameraStruct
            {
                position = new Vector4(Position, 1.0f),
                forward = new Vector4(forwardDirection, 1.0f),
                viewProjMatrix = GetViewProjectionMatrix()
            };
        }


    }
}
