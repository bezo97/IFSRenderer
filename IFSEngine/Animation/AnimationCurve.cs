using OpenTK;
using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Animation;

public class AnimationCurve
{
    private readonly List<ControlPoint> _controlPoints = new();
    private readonly ICurveImplementation _curveImplementation = new LinearCurveImplementation();

    public void AddControlPoint(ControlPoint newPoint)
    {
        _controlPoints.Add(newPoint);
        _controlPoints.Sort((x, y) => x.t < y.t ? -1 : 1);
    }
    public float Evaluate(float t)
    {
        return _curveImplementation.Evaluate(t, _controlPoints);
    }

}
