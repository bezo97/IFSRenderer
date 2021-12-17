using OpenTK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using IFSEngine.Utility;

namespace IFSEngine.Animation;

public class ControlPoint
{
    public ChangeDetector<double> t = new ChangeDetector<double>();
    public ChangeDetector<double> Value = new ChangeDetector<double>();
    public ChangeDetector<double> LeftTangent = new ChangeDetector<double>(); //maybe angle enough
    public ChangeDetector<double> RightTangent = new ChangeDetector<double>();
}
