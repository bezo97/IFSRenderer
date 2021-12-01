﻿#pragma warning disable CS0649
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace IFSEngine.Rendering.GpuStructs
{
    internal struct SettingsStruct
    {
        internal CameraStruct camera_params;

        internal float fog_effect;
        internal int itnum;//number of iterators
        internal int palettecnt;//how many colors in the palette
        internal int padding1;

        internal int warmup;//initial skipping / fuse
        internal float entropy;
        internal int max_filter_radius;
        internal int padding0;

        internal int filter_method;
        internal float filter_param0;
        internal float filter_param1;
        internal float filter_param2;
    }
}
