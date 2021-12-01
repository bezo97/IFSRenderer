using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFSEngine.Animation;

public class AnimationManager
{
    private List<PropertyAnimation> animations;

    public void AddNewAnimation(Action<float> applyAction)
    {
        animations.Add(new PropertyAnimation(applyAction));

        animations[0].AnimationCurve.AddControlPoint(new ControlPoint { t = 0f, Value = 0f });
        animations[0].AnimationCurve.AddControlPoint(new ControlPoint { t = 10f, Value = 100f });
    }

    public void PlayAnimation()
    {

    }
}
