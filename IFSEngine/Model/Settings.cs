﻿using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Model
{
    internal struct Settings
    {
        internal CameraBaseParameters CameraBase;
        internal int itnum;//length of iterators - 1 (last one is finalit)
        internal int pass_iters;//iterations per pass
        internal int framestep;
        internal float fog_effect;
    }
}