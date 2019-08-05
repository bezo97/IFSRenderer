using OpenTK;
using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Animation
{
    public class AnimationCurve
    {
        private List<ControlPoint> controlPoints = new List<ControlPoint>();
        private ICurveImplementation curveImplementation= new LinearCurveImplementation();

        public void AddControlPoint(ControlPoint newPoint)
        {
            controlPoints.Add(newPoint);
            controlPoints.Sort((x,y)=>x.t<y.t?-1:1);
        }
        public float Evaluate(float t)
        {
            return curveImplementation.Evaluate(t, controlPoints);
        }

    }
}
