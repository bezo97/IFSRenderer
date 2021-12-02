using System;
using System.Collections.Generic;

namespace IFSEngine.Animation;

public class AnimationManager
{
    private readonly List<PropertyAnimation> _animations;

    public void AddNewAnimation(Action<float> applyAction)
    {
        _animations.Add(new PropertyAnimation(applyAction));

        _animations[0].AnimationCurve.AddControlPoint(new ControlPoint { t = 0f, Value = 0f });
        _animations[0].AnimationCurve.AddControlPoint(new ControlPoint { t = 10f, Value = 100f });
    }

    public void PlayAnimation()
    {

    }
}
