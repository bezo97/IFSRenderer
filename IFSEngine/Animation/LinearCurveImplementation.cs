using System.Collections.Generic;
using System.Linq;

using IFSEngine.Utility;

namespace IFSEngine.Animation;

public class LinearCurveImplementation : ICurveImplementation
{
    public double Evaluate(double t, List<Keyframe> keyframes)
    {

        if (t >= keyframes.Last().t)
            return keyframes.Last().Value;
        if (t <= keyframes.First().t)
            return keyframes.First().Value;

        var previousKeyframe = keyframes.Where(c => c.t < t).MaxBy(c => c.t);
        var nextKeyframe = keyframes.Where(c => c.t > t).MinBy(c => c.t);

        var transformedT = (t - previousKeyframe.t) / (nextKeyframe.t - previousKeyframe.t);
        return MathExtensions.Lerp(previousKeyframe.Value, nextKeyframe.Value, transformedT);

    }
}
