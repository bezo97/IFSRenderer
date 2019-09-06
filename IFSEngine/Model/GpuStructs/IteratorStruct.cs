using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Model.GpuStructs
{
    internal struct IteratorStruct
    {
        internal float w;
        internal float cs;
        internal float ci;//color index, 0 - 1
        internal float op;
        internal int tfId;
        internal int tfParamsStart;
        internal int padding2;
        internal int padding3;
    }
}
