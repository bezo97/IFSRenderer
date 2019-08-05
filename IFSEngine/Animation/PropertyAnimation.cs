using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Animation
{
    class PropertyAnimation
    {
        public AnimationCurve AnimationCurve;
        public Action<float> ApplyValue;

        public PropertyAnimation(Action<float> ApplyValue)
        {
            this.ApplyValue = ApplyValue;
        }
        public void Animate(float t)
        {
            ApplyValue(AnimationCurve.Evaluate(t));
        }
    }
}
