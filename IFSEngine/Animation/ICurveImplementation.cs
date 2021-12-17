using OpenTK;
using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Animation;

interface ICurveImplementation
{
    double Evaluate(double t, List<ControlPoint> controlPoints);
}
