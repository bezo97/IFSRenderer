using IFSEngine.Rendering.GpuStructs;
using IFSEngine.Utility;
using System.Numerics;

namespace IFSEngine.Model;

//http://www.opengl-tutorial.org/intermediate-tutorials/tutorial-17-quaternions/
public class Camera
{
    public Quaternion Orientation { get; set; } = Quaternion.Identity;//no rotation
    public Vector3 Position { get; set; } = new Vector3(0.0f, 0.0f, -10.0f);
    public Vector3 RightDirection { get; private set; } = new Vector3(1.0f, 0.0f, 0.0f);
    public Vector3 UpDirection { get; private set; } = new Vector3(0.0f, 1.0f, 0.0f);
    public Vector3 ForwardDirection { get; private set; } = new Vector3(0.0f, 0.0f, 1.0f);

    /// <summary>
    /// Vertical field of view in degrees. Ranges from 0 to 180 exclusive.
    /// </summary>
    public double FieldOfView { get; set; } = 60;
    public double Aperture { get; set; } = 0.0;
    public double FocusDistance { get; set; } = 10.0;
    public double DepthOfField { get; set; } = 0.25;

    /// <summary>
    /// Moves camera position by a translate vector given in camera space.
    /// </summary>
    /// <param name="translateVector"></param>
    public void Translate(Vector3 translateVector)
    {
        Position += RightDirection * translateVector.X
                 + UpDirection * translateVector.Y
                 + ForwardDirection * translateVector.Z;
    }

    /// <summary>
    /// Rotates the camera orientation by the specified Euler angles.
    /// </summary>
    /// <param name="rotVector">Euler angle (Yaw, Pitch, Roll) deltas in radians.</param>
    public void Rotate(Vector3 rotVector)
    {
        Quaternion rotq = Quaternion.CreateFromYawPitchRoll(rotVector.X, rotVector.Y, rotVector.Z);
        Orientation *= rotq;
        Orientation = Quaternion.Normalize(Orientation);
        UpdateDirectionVectors();
    }

    private void UpdateDirectionVectors()
    {
        RightDirection = Vector3.Transform(new Vector3(1.0f, 0.0f, 0.0f), Orientation);
        UpDirection = Vector3.Transform(new Vector3(0.0f, 1.0f, 0.0f), Orientation);
        ForwardDirection = Vector3.Transform(new Vector3(0.0f, 0.0f, 1.0f), Orientation);
    }

    private Matrix4x4 GetViewProjectionMatrix()
    {
        //Matrix4x4.CreateLookAt uses different handedness so direction vectors are inverted here to get the correct view matrix.
        var viewMatrix = Matrix4x4.CreateLookAt(Position, Position - ForwardDirection, -UpDirection);
        var projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(NumericExtensions.ToRadians(1 + (float)FieldOfView % 179), 1.0f, 0.2f, 100.0f);
        return viewMatrix * projectionMatrix;
    }

    internal CameraStruct GetCameraParameters()
    {
        UpdateDirectionVectors();
        return new CameraStruct
        {
            position = new Vector4(Position, 1.0f),
            forward = new Vector4(ForwardDirection, 1.0f),
            viewProjMatrix = GetViewProjectionMatrix(),
            aperture = (float)Aperture,
            focus_distance = (float)FocusDistance,
            depth_of_field = (float)DepthOfField,
            focus_point = new Vector4(Position + (float)FocusDistance * ForwardDirection, 0.0f)
        };
    }


}
