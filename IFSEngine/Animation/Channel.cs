using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFSEngine.Animation;

public class Channel
{
    public List<Keyframe> Keyframes { get; init; } = new();//TODO: SortedList

    public void AddKeyframe(Keyframe keyframe)
    {
        Keyframes.Add(keyframe);
        Keyframes.Sort((x, y) => x.t < y.t ? -1 : 1);

    }

    public double EvaluateAt(double t)
    {
        //var interpolationMode = _keyframes.Where(c => c.t < t).MaxBy(c=>c.t).InterpolationMode;
        //TODO: Interpolation Mode

        return new LinearCurveImplementation().Evaluate(t, Keyframes);
    }

}
