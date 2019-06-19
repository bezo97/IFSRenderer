using GlmNet;
using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine
{
    // Defines several possible options for camera movement. Used as abstraction to stay away from window-system specific input methods
    enum CameraMovement
    {
        FORWARD,
        BACKWARD,
        LEFT,
        RIGHT
    };

    class YPCamera
    {
        // Default camera values
        const float YAW = -90.0f;
        const float PITCH = 0.0f;
        const float SPEED = 2.5f;
        const float SENSITIVITY = 0.1f;
        const float ZOOM = 45.0f;

        // Camera Attributes
        vec3 Position;
        vec3 Front;
        vec3 Up;
        vec3 Right;
        vec3 WorldUp;
        // Euler Angles
        float Yaw;
        float Pitch;
        // Camera options
        float MovementSpeed;
        float MouseSensitivity;
        float Zoom;

        public YPCamera(float yaw = YAW, float pitch = PITCH):this(new vec3(0.0f, 0.0f, 0.0f), new vec3(0.0f, 1.0f, 0.0f), yaw, pitch)
        {
        }

        // Constructor with vectors
        public YPCamera(vec3 position , vec3 up , float yaw = YAW, float pitch = PITCH)
        {
            Front = new vec3(0.0f, 0.0f, -1.0f);
            MovementSpeed = SPEED;
            MouseSensitivity = SENSITIVITY;
            Zoom = ZOOM;
            Position = position;
            WorldUp = up;
            Yaw = yaw;
            Pitch = pitch;
            updateCameraVectors();
        }

        // Constructor with scalar values
        public YPCamera(float posX, float posY, float posZ, float upX, float upY, float upZ, float yaw, float pitch)
        {
            Position = new vec3(posX, posY, posZ);
            WorldUp = new vec3(upX, upY, upZ);
            Yaw = yaw;
            Pitch = pitch;
            updateCameraVectors();
        }

        // Returns the view matrix calculated using Euler Angles and the LookAt Matrix
        mat4 GetViewMatrix()
        {
            return lookAt(Position, Position + Front, Up);
        }

        // Processes input received from any keyboard-like input system. Accepts input parameter in the form of camera defined ENUM (to abstract it from windowing systems)
        void ProcessKeyboard(CameraMovement direction, float deltaTime)
        {
            float velocity = MovementSpeed * deltaTime;
            if (direction == CameraMovement.FORWARD)
                Position += Front * velocity;
            if (direction == CameraMovement.BACKWARD)
                Position -= Front * velocity;
            if (direction == CameraMovement.LEFT)
                Position -= Right * velocity;
            if (direction == CameraMovement.RIGHT)
                Position += Right * velocity;
        }

        // Processes input received from a mouse input system. Expects the offset value in both the x and y direction.
        void ProcessMouseMovement(float xoffset, float yoffset, bool constrainPitch = true)
        {
            xoffset *= MouseSensitivity;
            yoffset *= MouseSensitivity;

            Yaw += xoffset;
            Pitch += yoffset;

            // Make sure that when pitch is out of bounds, screen doesn't get flipped
            if (constrainPitch)
            {
                if (Pitch > 89.0f)
                    Pitch = 89.0f;
                if (Pitch < -89.0f)
                    Pitch = -89.0f;
            }

            // Update Front, Right and Up Vectors using the updated Euler angles
            updateCameraVectors();
        }

        // Processes input received from a mouse scroll-wheel event. Only requires input on the vertical wheel-axis
        void ProcessMouseScroll(float yoffset)
        {
            if (Zoom >= 1.0f && Zoom <= 45.0f)
                Zoom -= yoffset;
            if (Zoom <= 1.0f)
                Zoom = 1.0f;
            if (Zoom >= 45.0f)
                Zoom = 45.0f;
        }

        
    // Calculates the front vector from the Camera's (updated) Euler Angles
    private void updateCameraVectors()
        {
            
            // Calculate the new Front vector
            vec3 front;
            front.x = Math.Cos(radians(Yaw)) * Math.Cos(radians(Pitch));
            front.y = Math.Sin(radians(Pitch));
            front.z = Math.Sin(radians(Yaw)) * Math.Cos(radians(Pitch));
            Front = normalize(front);
            // Also re-calculate the Right and Up vector
            Right = normalize(cross(Front, WorldUp));  // Normalize the vectors, because their length gets closer to 0 the more you look up or down which results in slower movement.
            Up = normalize(cross(Right, Front));
        }
    }
}
