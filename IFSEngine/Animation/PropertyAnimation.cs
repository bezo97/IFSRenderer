using System;

namespace IFSEngine.Animation;

public class PropertyAnimation
{
    public readonly string AnimatedVariableName;
    public readonly AnimationCurve AnimationCurve;
    private readonly Action<float> _applyValue;

    public PropertyAnimation(Action<float> ApplyValue, string animatedVariableName)
    {
        this._applyValue = ApplyValue;
        AnimationCurve = new AnimationCurve();
        AnimatedVariableName = animatedVariableName;
    }
    public void Animate(double t)
    {
        _applyValue((float)AnimationCurve.Evaluate(t));
    }
}
