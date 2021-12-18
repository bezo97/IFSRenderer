using IFSEngine.Utility;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IFSEngine.Animation;

public class AnimationManager
{
    public delegate void AnimationCreatedHandler(PropertyAnimation animationCurve);
    public int AnimationCount => _animations.Count;
    public int CurrentAnimationId => _currentAnimationId;
    public event AnimationCreatedHandler OnAnimationCreated;
    public float AnimationLength = 10f;
    private readonly List<PropertyAnimation> _animations = new();
    private PropertyAnimation _currentAnimation;
    private double _animationSliderTime = 0;
    private int _currentAnimationId;
    public int AddNewAnimation(string animatedVariableName, Action<float> applyAction, double currentValue)
    {
        _animations.Add(new PropertyAnimation(applyAction, animatedVariableName));
        _currentAnimationId = _animations.Count - 1;
        _currentAnimation = _animations[_currentAnimationId];
        OnAnimationCreated?.Invoke(_currentAnimation);

        CreateControlPoint(currentValue);
        return _animations.Count - 1;
    }

    public void AddNewControlPoint(in int animationId, in double value)
    {
        _currentAnimationId = animationId;
        _currentAnimation = _animations[animationId];
        CreateControlPoint(value);
    }

    private void CreateControlPoint(in double value)
    {
        CreateControlPoint(_animationSliderTime, value);
    }
    private void CreateControlPoint(in double timeInSeconds, in double value)
    {
        ControlPoint cp = _currentAnimation.AnimationCurve.GetPointAt(timeInSeconds);
        if (cp == null)
        {
            var newCP = new ControlPoint { t = new ChangeDetector<double>(timeInSeconds), Value = new ChangeDetector<double>(value) };
            _currentAnimation.AnimationCurve.AddControlPoint(newCP);

        }
        else
        {
            cp.Value.Update(value);
        }
    }

    public void EvaluateAt(in double normalizedTime)
    {
        _animationSliderTime = normalizedTime * AnimationLength;
        for (int i = 0; i < _animations.Count; i++)
        {
            _animations[i].Animate(normalizedTime * AnimationLength);
        }
    }
}
