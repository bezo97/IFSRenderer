using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Animation
{
    public class PropertyAnimation
    {
        public readonly string AnimatedVariableName;
        public readonly AnimationCurve AnimationCurve;
        private readonly Action<float> ApplyValue;

        public PropertyAnimation(Action<float> ApplyValue,string animatedVariableName)
        {
            this.ApplyValue = ApplyValue;
            AnimationCurve = new AnimationCurve();
            AnimatedVariableName = animatedVariableName;
        }
        public void Animate(double t)
        {
            ApplyValue((float)AnimationCurve.Evaluate(t));
        }
    }
}
