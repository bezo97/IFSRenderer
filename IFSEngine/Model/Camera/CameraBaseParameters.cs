using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

namespace IFSEngine.Model
{
    internal struct CameraBaseParameters
    {
        internal Matrix4 viewProjMatrix;
        internal Vector4 position;
        internal Vector4 forward;
    }
}
