using System;
using System.Collections.Generic;
using System.Text;
using IFSEngine.Helper;
using System.Numerics;

namespace IFSEngine.Model.Camera
{
    public abstract class CameraBase
    {
        internal CameraBaseParameters Params = new CameraBaseParameters();
        public event Action OnManipulate;

        //?
        public float AspectRatio { get; set; } = 1.0f;

        public float MovementSpeed { get; set; } = 2.5f;
        public float Sensitivity { get; set; } = 0.2f;
        public float FOV
        {
            get => fov;
            set
            {
                fov = value;
                //TODO: this should be in UpdateCamera (?)
                projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(NumericExtensions.ToRadians(FOV), AspectRatio, 0.2f, 100.0f);
            }
        }
        private float fov = 30;

        // Camera 3D Attributes
        protected Vector3 position
        {
            get => new Vector3(Params.position.X, Params.position.Y, Params.position.Z);
            set => Params.position = new Vector4(value, 1.0f);
        }
        protected Vector3 forward
        {
            get => new Vector3(Params.forward.X, Params.forward.Y, Params.forward.Z);
            set => Params.forward = new Vector4(value, 1.0f);
        }
        protected Vector3 up;
        protected Vector3 right;
        protected Matrix4x4 projectionMatrix;

        public CameraBase() : this(new Vector3(0.0f, 0.0f, -2.0f), new Vector3(0.0f, 0.0f, 1.0f), new Vector3(1.0f, 0.0f, 0.0f), new Vector3(0.0f,1.0f,0.0f), 60.0f)
        {
        }

        // Constructor with vectors
        public CameraBase(Vector3 position, Vector3 forward, Vector3 right, Vector3 up, float FOV)
        {
            this.position = position;
            this.forward = forward;
            this.right = right;
            this.up = up;
            this.FOV = FOV;
        }

        public void Translate(Vector3 translateVector)
        {
            translateVector *= MovementSpeed;
            position += forward * translateVector.X;
            position += right * translateVector.Y;
            position += up * translateVector.Z;
        }

        // Processes input received from a mouse input system. Expects the offset value in both the x and y direction.
        public abstract void ProcessMouseMovement(float xoffset, float yoffset);

        public void UpdateCamera()
        {
            RefreshCameraValues();
            SetViewProjMatrix();
            OnManipulate?.Invoke();
        }

        protected abstract void RefreshCameraValues();

        protected abstract void SetViewProjMatrix();
    }
}
