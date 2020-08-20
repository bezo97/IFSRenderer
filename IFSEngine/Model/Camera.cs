using IFSEngine.Helper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using IFSEngine.Model.GpuStructs;

namespace IFSEngine.Model
{
    //http://www.opengl-tutorial.org/intermediate-tutorials/tutorial-17-quaternions/
    public class Camera
    {
        public Quaternion Orientation { get; set; } = Quaternion.Identity;//no rotation
        public Vector3 Position { get; set; } = new Vector3(0.0f, 0.0f, -2.0f);
        public float TranslationSensitivity { get; set; } = 0.005f;
        public float RotationSensitivity { get; set; } = 0.01f;


        public Vector3 RightDirection { get; private set; } = new Vector3(1.0f, 0.0f, 0.0f);
        public Vector3 UpDirection { get; private set; } = new Vector3(0.0f, 1.0f, 0.0f);
        public Vector3 ForwardDirection { get; private set; } = new Vector3(0.0f, 0.0f, 1.0f);

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
            Position += RightDirection * translateVector.X
                     + UpDirection * translateVector.Y
                     + ForwardDirection * translateVector.Z;
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
        /// Rotates the camera orientation by the specified Euler angles.
        /// </summary>
        /// <param name="rotVector">Euler angle (Yaw, Pitch, Roll) deltas in radians.</param>
        public void RotateBy(Vector3 rotVector)
        {
            Quaternion rotq = Quaternion.CreateFromYawPitchRoll(rotVector.X,rotVector.Y,rotVector.Z);
            Orientation *= rotq;
            Orientation = Quaternion.Normalize(Orientation);

            RightDirection = Vector3.Transform(new Vector3(1.0f, 0.0f, 0.0f), Orientation);
            UpDirection = Vector3.Transform(new Vector3(0.0f, 1.0f, 0.0f), Orientation);
            ForwardDirection = Vector3.Transform(new Vector3(0.0f, 0.0f, 1.0f), Orientation);
        }

        private Matrix4x4 GetViewProjectionMatrix()
        {
            //Matrix4x4.CreateLookAt uses different handedness so direction vectors are inverted here to get the correct view matrix.
            var viewMatrix = Matrix4x4.CreateLookAt(Position, Position - ForwardDirection, -UpDirection);
            var projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(NumericExtensions.ToRadians(1 + FieldOfView % 179), 1.0f, 0.2f, 100.0f);
            return viewMatrix * projectionMatrix;
        }

        internal CameraStruct GetCameraParameters()
        {
            return new CameraStruct
            {
                position = new Vector4(Position, 1.0f),
                forward = new Vector4(ForwardDirection, 1.0f),
                viewProjMatrix = GetViewProjectionMatrix()
            };
        }


    }
}
