using OpenTK;
using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Animation
{
    public struct ControlPoint
    {
        public float t;
        public Vector2 Point;
        public Vector2 LeftTangent;
        public Vector2 RightTangent;
    }
}
