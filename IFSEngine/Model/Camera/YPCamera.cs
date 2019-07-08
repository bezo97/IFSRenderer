using IFSEngine.Helper;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IFSEngine.Model;

namespace IFSEngine.Model.Camera
{
    // Defines several possible options for CameraBase movement. Used as abstraction to stay away from window-system specific input methods
    public enum CameraMovementDirection
    {
        FORWARD,
        BACKWARD,
        LEFT,
        RIGHT
    };

    public class YPCamera
    {
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
        internal CameraBaseParameters Params = new CameraBaseParameters();

        //public float FocalLength { get => Params.focallength; set => Params.focallength = value; }
        //public float FocusDistance { get => Params.focusdistance; set => Params.focusdistance = value; }
        //public float DepthOfField { get => Params.dof_effect; set => Params.dof_effect = value; }

        // Default CameraBase values
        private const float Yaw = -90.0f;
        private const float Pitch = 0.0f;
        private const float Speed = 2.5f;
        private const float Sensitivity = 0.1f;
        private const float Zoom = 45.0f;

        // Camera Attributes
        private Vector3 position
        {
            get => Params.position.Xyz;
            set => Params.position = new Vector4(value, 1.0f);
        }
        private Vector3 front
        {
            get => Params.forward.Xyz;
            set => Params.forward = new Vector4(value, 1.0f);
        }
        private Vector3 up;
        private Vector3 right;
        private Matrix4 projectionMatrix;
        private readonly Vector3 worldUp;
        // Euler Angles
        private float yaw;

        private float pitch;
        // Camera options
        private readonly float movementSpeed;
        private readonly float mouseSensitivity;

        public YPCamera(float yaw = Yaw, float pitch = Pitch) : this(new Vector3(0.0f, 0.0f, 2.0f), new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 0.0f, -1.0f), yaw, pitch)
        {
        }

        // Constructor with vectors
        public YPCamera(Vector3 position, Vector3 up, Vector3 front, float yaw = Yaw, float pitch = Pitch)
        {
            this.front = front;
            movementSpeed = Speed;
            mouseSensitivity = Sensitivity;
            this.position = position;
            worldUp = up;
            this.yaw = yaw;
            this.pitch = pitch;
            FOV = 30f;
            UpdateCameraVectors();
        }

        // Constructor with scalar values
        public YPCamera(float posX, float posY, float posZ, float upX, float upY, float upZ, float yaw, float pitch)
        {
            position = new Vector3(posX, posY, posZ);
            worldUp = new Vector3(upX, upY, upZ);
            this.yaw = yaw;
            this.pitch = pitch;
            FOV = 90f;

            UpdateCameraVectors();
        }

        // Returns the view matrix calculated using Euler Angles and the LookAt Matrix
        private void SetViewProjMatrix()
        {
            Params.viewProjMatrix = Matrix4.LookAt(position, position + front, Vector3.UnitY) * projectionMatrix;
        }

        // Processes input received from any keyboard-like input system. Accepts input parameter in the form of CameraBase defined ENUM (to abstract it from windowing systems)
        public void Translate(Vector3 translateVector)
        {
            position += front * translateVector.X;
            position += right * translateVector.Y;
            position += up * translateVector.Z;
            UpdateCameraVectors();
        }

        public void Translate(CameraMovementDirection direction, float speed = 0.5f)
        {
            if (direction == CameraMovementDirection.FORWARD)
                position += front * speed;
            else if (direction == CameraMovementDirection.BACKWARD)
                position -= front * speed;
            else if (direction == CameraMovementDirection.LEFT)
                position -= right * speed;
            else if (direction == CameraMovementDirection.RIGHT)
                position += right * speed;
            SetViewProjMatrix();
        }

        // Processes input received from a mouse input system. Expects the offset value in both the x and y direction.
        public void ProcessMouseMovement(float xoffset, float yoffset)
        {
            // xoffset *= mouseSensitivity;
            // yoffset *= mouseSensitivity;

            yaw += xoffset;
            pitch += yoffset;


            if (pitch > 89.0f)
                pitch = 89.0f;
            if (pitch < -89.0f)
                pitch = -89.0f;

            // Update Front, Right and Up Vectors using the updated Euler angles
            UpdateCameraVectors();
        }

        // Calculates the front vector from the Camera's (updated) Euler Angles
        private void UpdateCameraVectors()
        {
            // Calculate the new Front vector
            Vector3 front = new Vector3();
            front.X = (float)(Math.Cos(NumericExtensions.ToRadians(yaw)) * Math.Cos(NumericExtensions.ToRadians(pitch)));
            front.Y = (float)(Math.Sin(NumericExtensions.ToRadians(pitch)));
            front.Z = (float)(Math.Sin(NumericExtensions.ToRadians(yaw)) * Math.Cos(NumericExtensions.ToRadians(pitch)));
            this.front = front.Normalized();
            // Also re-calculate the Right and Up vector
            right = Vector3.Cross(this.front, worldUp).Normalized();// Normalize the vectors, because their length gets closer to 0 the more you look up or down which results in slower movement.
            up = Vector3.Cross(right, this.front).Normalized();
            SetViewProjMatrix();
        }

    }
}
