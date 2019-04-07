using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Model
{
    internal struct CameraParameters
    {
        internal float ox;//pos
        internal float oy;
        internal float oz;

        internal float dx;//dir
        internal float dy;
        internal float dz;

        //dir
        internal float theta;//0-pi
        internal float phi;//0-2pi
        //helpers
        internal float sin_theta;
        internal float cos_theta;
        internal float sin_phi;
        internal float cos_phi;
        internal float sin_phi_hack;
        internal float cos_phi_hack;

        internal float focallength;//for perspective

        internal float focusdistance;//for dof
        internal float dof_effect;
    }
}
