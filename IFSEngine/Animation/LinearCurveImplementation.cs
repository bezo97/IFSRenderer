using IFSEngine.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFSEngine.Animation;

public class LinearCurveImplementation : ICurveImplementation
{
    public double Evaluate(double t, List<Keyframe> keyframes)
    {

        if (t >= keyframes.Last().t)
            return keyframes.Last().Value;
        if (t <= keyframes.First().t)
            return keyframes.First().Value;

        //Keyframe leftControlPoint = new(), rightControlPoint = new();
        //for (int i = 0; i < controlPoints.Count - 1; i++)
        //{
        //    if (t >= controlPoints[i].t && t <= controlPoints[i + 1].t)
        //    {
        //        leftControlPoint = controlPoints[i];
        //        rightControlPoint = controlPoints[i + 1];
        //    }
        //}
        var previousKeyframe = keyframes.Where(c => c.t < t).MaxBy(c => c.t);
        var nextKeyframe = keyframes.Where(c => c.t > t).MinBy(c => c.t);

        var transformedT = (t - previousKeyframe.t) / (nextKeyframe.t - previousKeyframe.t);
        return MathExtensions.Lerp(previousKeyframe.Value, nextKeyframe.Value, transformedT);

    }
}
