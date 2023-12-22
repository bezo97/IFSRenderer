using System.Collections.Generic;

namespace IFSEngine.Animation;

internal interface ICurveImplementation
{
    double Evaluate(double t, List<Keyframe> keyframes);
}
