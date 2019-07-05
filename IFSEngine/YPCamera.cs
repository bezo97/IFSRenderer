using IFSEngine.Helper;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IFSEngine.Model;

namespace IFSEngine
{
    // Defines several possible options for camera movement. Used as abstraction to stay away from window-system specific input methods
    public enum CameraMovement
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
            get=>fov;
            set
            {
                fov = value;
                projectionMatrix =
                    /*CreateProjMatrix(); //*/Matrix4.CreatePerspectiveFieldOfView(NumericExtensions.ToRadians(FOV),(float)Width/(float)Height,0.2f,100.0f);
            }
        }

        private Matrix4 CreateProjMatrix()
        {
            float sy =(float) (1.0 / Math.Tan((double) (FOV / 2.0f)));
            float asp = (float)Width / (float)Height;
            float fp = 0.2f;
            float bp = 250.0f;
            return  new Matrix4(
                sy/asp,0,0,0,
                0,sy,0,0,
                0,0,-(fp+bp)/(bp-fp),-1,
                0,0,-2.0f*fp*bp/(bp-fp),0);
        }
        private float fov = 30;
        public bool EnableDepthFog { get; set; } = true;
        internal YPCameraParameters Params = new YPCameraParameters();

        //public float FocalLength { get => Params.focallength; set => Params.focallength = value; }
        //public float FocusDistance { get => Params.focusdistance; set => Params.focusdistance = value; }
        //public float DepthOfField { get => Params.dof_effect; set => Params.dof_effect = value; }

        // Default camera values
        private const float Yaw = -90.0f;
        private const float Pitch = 0.0f;
        private const float Speed = 2.5f;
        private const float Sensitivity = 0.1f;
        private const float Zoom = 45.0f;

        // Camera Attributes
        private Vector3 position
        {
            get=> Params.position.Xyz;
            set=>Params.position=new Vector4(value,1.0f);
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

        public YPCamera(float yaw = Yaw, float pitch = Pitch) :this(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f),new Vector3(0.0f,0.0f,-1.0f), yaw, pitch)
        {
        }

        // Constructor with vectors
        public YPCamera(Vector3 position , Vector3 up,Vector3 front , float yaw = Yaw, float pitch = Pitch)
        {
            this.front =front;
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
            Params.viewProjMatrix =
                /*CreateViewMatrix() **/ Matrix4.LookAt(position, position + front, Vector3.UnitY)* projectionMatrix;
        }

        private Matrix4 CreateViewMatrix()
        {
            Matrix4 trans = new Matrix4(
                1,0,0,0,
                0,1,0,0,
                0,0,1,0,
                -position.X,-position.Y,-position.Z,1
                );

            Vector3 w = (position - (position + front)).Normalized();
            Vector3 u = Vector3.Cross(worldUp,w).Normalized();
            Vector3 v = Vector3.Cross(w,u);
            Matrix4 vTrans = new Matrix4(
                    u.X,u.Y,u.Z,0.0f,
                    v.X,v.Y,v.Z,0.0f,
                    w.X,w.Y,w.Z,0.0f,
                    0,0,0,1
                );
            vTrans.Invert();
            return trans * vTrans;
        }

        // Processes input received from any keyboard-like input system. Accepts input parameter in the form of camera defined ENUM (to abstract it from windowing systems)
        public void Translate(Vector3 translateVector)
        {
            position += front * translateVector.X;
            position += right * translateVector.Y;
            position += up * translateVector.Z;
            UpdateCameraVectors();
        }

        public void Translate(CameraMovement direction,float speed=0.5f)
        {
            if (direction == CameraMovement.FORWARD)
                position += front * speed;
            if (direction == CameraMovement.BACKWARD)
                position -= front * speed;
            if (direction == CameraMovement.LEFT)
                position -= right * speed;
            if (direction == CameraMovement.RIGHT)
                position += right * speed;
            SetViewProjMatrix();
        }

        // Processes input received from a mouse input system. Expects the offset value in both the x and y direction.
        public void ProcessMouseMovement(float xoffset, float yoffset, bool constrainPitch = true)
        {
           // xoffset *= mouseSensitivity;
           // yoffset *= mouseSensitivity;

            yaw += xoffset;
            pitch += yoffset;

            // Make sure that when pitch is out of bounds, screen doesn't get flipped
            if (constrainPitch)
            {
                if (pitch > 89.0f)
                    pitch = 89.0f;
                if (pitch < -89.0f)
                    pitch = -89.0f;
            }

            // Update Front, Right and Up Vectors using the updated Euler angles
            UpdateCameraVectors();
        }



        
    // Calculates the front vector from the Camera's (updated) Euler Angles
    private void UpdateCameraVectors()
        {            
            // Calculate the new Front vector
            Vector3 front = new Vector3();
            front.X =(float) (Math.Cos(NumericExtensions.ToRadians(yaw)) * Math.Cos(NumericExtensions.ToRadians(pitch)));
            front.Y =(float)(Math.Sin(NumericExtensions.ToRadians(pitch)));
            front.Z =(float) (Math.Sin(NumericExtensions.ToRadians(yaw)) * Math.Cos(NumericExtensions.ToRadians(pitch)));
            this.front = front.Normalized();
            // Also re-calculate the Right and Up vector
            right = Vector3.Cross(this.front, worldUp).Normalized();// Normalize the vectors, because their length gets closer to 0 the more you look up or down which results in slower movement.
            up = Vector3.Cross(right, this.front).Normalized();
            SetViewProjMatrix();
        }

    }
}
