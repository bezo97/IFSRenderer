using System;
using System.Collections.Generic;
using System.Text;
using IFSEngine.Helper;
using OpenTK;

namespace IFSEngine.Model.Camera
{
    public abstract class CameraBase
    {
        internal CameraBaseParameters Params = new CameraBaseParameters();

        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1080;

        public float FOV
        {
            get => fov;
            set
            {
                fov = value;
                projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(NumericExtensions.ToRadians(FOV), (float)Width / (float)Height, 0.2f, 100.0f);
            }
        }

        private float fov = 30;
        public bool EnableDepthFog { get; set; } = true;

        //public float FocalLength { get => Params.focallength; set => Params.focallength = value; }
        //public float FocusDistance { get => Params.focusdistance; set => Params.focusdistance = value; }
        //public float DepthOfField { get => Params.dof_effect; set => Params.dof_effect = value; }

        // Default CameraBase values
        private const float Speed = 2.5f;
        private const float Sensitivity = 0.1f;

        // Camera Attributes
        protected Vector3 position
        {
            get => Params.position.Xyz;
            set => Params.position = new Vector4(value, 1.0f);
        }
        protected Vector3 front
        {
            get => Params.forward.Xyz;
            set => Params.forward = new Vector4(value, 1.0f);
        }
        protected Vector3 up;
        protected Vector3 right;
        protected Matrix4 projectionMatrix;
        protected readonly Vector3 worldUp;

        // Camera options
        private readonly float movementSpeed;
        private readonly float mouseSensitivity;

        public CameraBase() : this(new Vector3(0.0f, 0.0f, 2.0f), new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 0.0f, -1.0f))
        {
        }

        // Constructor with vectors
        public CameraBase(Vector3 position, Vector3 up, Vector3 front)
        {
            this.front = front;
            movementSpeed = Speed;
            mouseSensitivity = Sensitivity;
            this.position = position;
            worldUp = up;
            FOV = 30f;
        }

        // Returns the view matrix calculated using Euler Angles and the LookAt Matrix
        protected abstract void SetViewProjMatrix();

        // Processes input received from any keyboard-like input system. Accepts input parameter in the form of CameraBase defined ENUM (to abstract it from windowing systems)
        public abstract void Translate(Vector3 translateVector);

        // Processes input received from a mouse input system. Expects the offset value in both the x and y direction.
        public abstract void ProcessMouseMovement(float xoffset, float yoffset);

    }
}
