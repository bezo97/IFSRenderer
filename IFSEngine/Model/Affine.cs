using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Model
{
    public struct Affine
    {
        public float ox;
        public float oy;
        public float oz;

        public float xx;
        public float xy;
        public float xz;

        public float yx;
        public float yy;
        public float yz;

        public float zx;
        public float zy;
        public float zz;

        public Affine(float ox, float oy, float oz, float xx, float xy, float xz, float yx, float yy, float yz, float zx, float zy, float zz)
        {
            this.ox = ox;
            this.oy = oy;
            this.oz = oz;

            this.xx = xx;
            this.xy = xy;
            this.xz = xz;

            this.yx = yx;
            this.yy = yy;
            this.yz = yz;

            this.zx = zx;
            this.zy = zy;
            this.zz = zz;
        }
    }
}
