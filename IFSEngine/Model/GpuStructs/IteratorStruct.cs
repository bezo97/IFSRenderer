using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Model.GpuStructs
{
    internal struct IteratorStruct
    {
        internal float color_speed;
        internal float color_index;//color index, 0 - 1
        internal float opacity;
        internal float reset_prob;
        internal int reset_alias;
        internal int tfId;
        internal int tfParamsStart;
        internal int shading_mode;//0: default, 1: delta_p
    }
}
