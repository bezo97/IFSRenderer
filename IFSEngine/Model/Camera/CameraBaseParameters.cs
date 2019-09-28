using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace IFSEngine.Model
{
    internal struct CameraBaseParameters
    {
        internal Matrix4x4 viewProjMatrix;
        internal Vector4 position;
        internal Vector4 forward;
    }
}
