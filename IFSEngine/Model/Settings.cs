using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace IFSEngine.Model
{
    internal struct Settings
    {
        //current view:
        internal CameraBaseParameters CameraBase;
        internal Vector4 focuspoint;
        internal float fog_effect;
        internal float dof;
        internal float focusdistance;
        internal float focusarea;
        //current frame:
        internal int itnum;//length of iterators - 1 (last one is finalit)
        internal int pass_iters;//iterations per pass
        internal int framestep;
        internal int palettecnt;//how many colors in the palette
    }
}
