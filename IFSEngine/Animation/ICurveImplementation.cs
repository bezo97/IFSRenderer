using OpenTK;
using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Animation
{
    interface ICurveImplementation
    {
        void Evaluate(float t, List<ControlPoint> controlPoints);
    }
}
