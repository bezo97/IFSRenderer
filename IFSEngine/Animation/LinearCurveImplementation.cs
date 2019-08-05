using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFSEngine.Helper;

namespace IFSEngine.Animation
{
    class LinearCurveImplementation : ICurveImplementation
    {
        public float Evaluate(float t, List<ControlPoint> controlPoints)
        {

            if (t >= controlPoints.Last().t)
                return controlPoints.Last().Value;
            if (t <= controlPoints.First().t)
                return controlPoints.First().Value;

            ControlPoint leftControlPoint = new ControlPoint() , rightControlPoint = new ControlPoint();
            for (int i = 0; i < controlPoints.Count-1; i++)
            {
                if (t >= controlPoints[i].t && t <= controlPoints[i+1].t)
                {
                    leftControlPoint = controlPoints[i];
                    rightControlPoint = controlPoints[i+1];
                }
            }

            var transformedT = (t - leftControlPoint.t) / (rightControlPoint.t - leftControlPoint.t);
            return MathExtensions.Lerp(leftControlPoint.Value, rightControlPoint.Value, transformedT);

        }
    }
}
