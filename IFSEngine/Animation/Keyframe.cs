using IFSEngine.Utility;
using OpenTK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace IFSEngine.Animation;

public class Keyframe
{
    public string InterpolationMode { get; set; }
    public double t { get; set; }
    public double Value { get; set; }
    //public double LeftTangent;
    //public double RightTangent;
}
