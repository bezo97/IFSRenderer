using IFSEngine.Utility;
using OpenTK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace IFSEngine.Animation;

public class ControlPoint
{
    public ChangeDetector<double> t = new();
    public ChangeDetector<double> Value = new();
    public ChangeDetector<double> LeftTangent = new(); //maybe angle enough
    public ChangeDetector<double> RightTangent = new();
}
