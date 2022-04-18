using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFSEngine.Animation;

public class Channel
{
    public SortedDictionary<double, Keyframe> Keyframes { get; init; } = new();

    public Channel() { }
    public Channel(Keyframe keyframe)
    {
        Keyframes[keyframe.t] = keyframe;
    }

    public void AddKeyframe(Keyframe keyframe)
    {
        Keyframes[keyframe.t] = keyframe;
    }

    public double EvaluateAt(double t)
    {
        //var interpolationMode = _keyframes.Where(c => c.t < t).MaxBy(c=>c.t).InterpolationMode;
        //TODO: Interpolation Mode

        return new LinearCurveImplementation().Evaluate(t, Keyframes.Values.ToList());
    }

}
