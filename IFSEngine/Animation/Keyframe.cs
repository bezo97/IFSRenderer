namespace IFSEngine.Animation;

public class Keyframe
{
    public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.CatmullRom;
    public double EasingPower { get; set; } = 1.0;
    public EasingDirection EasingDirection { get; set; } = EasingDirection.InOut;
    public double t { get; set; }
    public double Value { get; set; }
    //public double LeftTangent;
    //public double RightTangent;
}
