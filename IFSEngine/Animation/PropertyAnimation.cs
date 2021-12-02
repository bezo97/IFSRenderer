using System;

namespace IFSEngine.Animation;

class PropertyAnimation
{
    public AnimationCurve AnimationCurve = new();
    private readonly Action<float> _applyValue;

    public PropertyAnimation(Action<float> ApplyValue)
    {
        _applyValue = ApplyValue;
    }
    public void Animate(float t)
    {
        _applyValue(AnimationCurve.Evaluate(t));
    }
}
