using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace IFSEngine.Model.GpuStructs
{
    internal struct SettingsStruct
    {
        //current view:
        internal CameraStruct CameraBase;
        internal Vector4 focuspoint;
        internal float fog_effect;
        internal float depth_of_field;
        internal float focusdistance;
        internal float focusarea;
        //current frame:
        internal int itnum;//length of iterators - 1 (last one is finalit)
        internal int pass_iters;//iterations per pass
        internal int dispatchCnt;//number of dispatches since accumulation reset
        internal int palettecnt;//how many colors in the palette

        internal int resetPointsState;//0/1
        internal int warmup;//initial skipping / fuse
        internal float entropy;
        internal int padding1;
    }
}
