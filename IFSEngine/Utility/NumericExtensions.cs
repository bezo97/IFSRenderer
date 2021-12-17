using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Utility;

internal static class NumericExtensions
{
    public static float ToRadians(float val)
    {
        return ((float)Math.PI / 180.0f) * val;
    }
}
