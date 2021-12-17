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
    private List<ControlPoint> controlPoints = new List<ControlPoint>();
    private ICurveImplementation curveImplementation = new LinearCurveImplementation();

    public void AddControlPoint(ControlPoint newPoint)
    {
        controlPoints.Add(newPoint);
        controlPoints.Sort((x, y) => x.t < y.t ? -1 : 1);
        OnControlPointCreated?.Invoke(newPoint, GetDuration());

    }
    public double Evaluate(double t)
    {
        return curveImplementation.Evaluate(t, controlPoints);
    }

    public ControlPoint GetPointAt(double t)
    {
        return controlPoints.FirstOrDefault(cp => Math.Abs(t - cp.t) < 0.01);

    }
    public ControlPoint GetLastControlPoint() => controlPoints[controlPoints.Count - 1];

    public double GetDuration() =>
        10; //controlPoints.Count == 0 ? 10 : (double) controlPoints[controlPoints.Count - 1].t;

}
