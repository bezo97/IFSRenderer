using System.Numerics;

namespace IFSEngine.Animation;

public class ControlPoint
{
    public float t;
    public float Value;
    public Vector2 LeftTangent; //maybe angle enough
    public Vector2 RightTangent;
}
