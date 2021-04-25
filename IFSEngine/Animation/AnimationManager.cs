using System;
using System.Collections.Generic;
using IFSEngine.Utility;

namespace IFSEngine.Animation
{
    public class AnimationManager
    {
        public delegate void AnimationCreatedHandler(PropertyAnimation animationCurve);
        public int AnimationCount => animations.Count;
        public int CurrentAnimationId => currentAnimationId;
        public event AnimationCreatedHandler OnAnimationCreated;

        private List<PropertyAnimation> animations = new List<PropertyAnimation>();
        private PropertyAnimation currentAnimation;
        private double animationSliderTime = 0;
        private int currentAnimationId;
        public int AddNewAnimation(string animatedVariableName, Action<float> applyAction, double currentValue)
        {
            animations.Add(new PropertyAnimation(applyAction, animatedVariableName));
            currentAnimationId = animations.Count - 1;
            currentAnimation = animations[currentAnimationId];
            OnAnimationCreated?.Invoke(currentAnimation);

            CreateControlPoint(currentValue);
            return animations.Count - 1;
        }

        public void AddNewControlPoint(in int animationId, in double value)
        {
            currentAnimationId = animationId;
            currentAnimation = animations[animationId];
            CreateControlPoint(value);
        }

        private void CreateControlPoint(in double value)
        {
            CreateControlPoint(animationSliderTime, value);
        }
        private void CreateControlPoint(in double timeInSeconds, in double value)
        {
            ControlPoint cp = currentAnimation.AnimationCurve.GetPointAt(timeInSeconds);
            if (cp == null)
            {
                var newCP = new ControlPoint { t = new ChangeDetector<double>(timeInSeconds), Value = new ChangeDetector<double>(value) };
                currentAnimation.AnimationCurve.AddControlPoint(newCP);

            }
            else
            {
                cp.Value.Update(value);
            }
        }

        public void PlayAnimation()
        {

        }

        public void EvaluateAt(in double timeInSeconds)
        {
            animationSliderTime = timeInSeconds;
            for (int i = 0; i < animations.Count; i++)
            {
                animations[i].Animate(timeInSeconds);
            }
        }
    }
}
