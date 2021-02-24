using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace IFSEngine.Rendering.GpuStructs
{
    internal struct CameraStruct
    {
        internal Matrix4x4 viewProjMatrix;
        internal Vector4 position;
        internal Vector4 forward;
        internal Vector4 focus_point;
        internal float depth_of_field;
        internal float focus_distance;
        internal float focus_area;
        internal float padding0;
    }
}
