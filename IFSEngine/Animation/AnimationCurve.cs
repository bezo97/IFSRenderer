using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFSEngine.Animation;

public delegate void ControlPointCreatedHandler(ControlPoint controlPoint, double duration);

public class AnimationCurve
{
    public event ControlPointCreatedHandler OnControlPointCreated;
    private readonly List<ControlPoint> _controlPoints = new();
    private readonly ICurveImplementation _curveImplementation = new LinearCurveImplementation();

    public void AddControlPoint(ControlPoint newPoint)
    {
        _controlPoints.Add(newPoint);
        _controlPoints.Sort((x, y) => x.t < y.t ? -1 : 1);
        OnControlPointCreated?.Invoke(newPoint, GetDuration());

    }
    public double Evaluate(double t)
    {
        return _curveImplementation.Evaluate(t, _controlPoints);
    }

    public ControlPoint GetPointAt(double t)
    {
        return _controlPoints.FirstOrDefault(cp => Math.Abs(t - cp.t) < 0.01);

    }
    public ControlPoint GetLastControlPoint() => _controlPoints[^1];

    public static double GetDuration() =>
        10; //controlPoints.Count == 0 ? 10 : (double) controlPoints[controlPoints.Count - 1].t;

}
