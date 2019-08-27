using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Model.GpuStructs
{
    internal struct IteratorStruct
    {
        internal int TfFirst;
        internal int TfNum;
        internal float w;
        internal float cs;
        internal float ci;//color index, 0 - 1
        internal float op;
    }
}
