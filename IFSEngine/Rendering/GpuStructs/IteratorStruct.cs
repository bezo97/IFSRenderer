#pragma warning disable CS0649
using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Rendering.GpuStructs
{
    internal struct IteratorStruct
    {
        internal float color_speed;
        internal float color_index;//color index, 0 - 1
        internal float opacity;
        internal float reset_prob;
        internal int reset_alias;
        internal int tfId;
        internal int real_params_index;
        internal int vec3_params_index;
        internal int shading_mode;//0: default, 1: delta_p
        internal float tf_mix;
        internal float tf_add;
        internal int padding2;
    }
}
