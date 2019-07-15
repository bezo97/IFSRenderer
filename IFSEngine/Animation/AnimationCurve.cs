using OpenTK;
using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Animation
{
    public class AnimationCurve
    {
        private List<ControlPoint> controlPoints;
        private ICurveImplementation curveImplementation;
        public void AddControlPoint(ControlPoint newPoint)
        {
            controlPoints.Add(newPoint);
        }
        public void Evaluate(float t)
        {
            curveImplementation.Evaluate(t, controlPoints);
        }

    }
}
