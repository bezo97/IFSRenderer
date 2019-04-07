using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Model
{
    public struct Iterator
    {
        public Affine aff;
        public int tfID;
        public float w;
        public float cs;
        public float ci;//color index, 0 - 1
        public float op;

        public Iterator(Affine aff, int tfID, float w, float cs, float ci, float op)
        {
            this.aff = aff;
            this.tfID = tfID;
            this.w = w;
            this.cs = cs;
            this.ci = ci;
            this.op = op;
        }
    }

}
