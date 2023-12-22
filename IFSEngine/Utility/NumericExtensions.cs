using System;

namespace IFSEngine.Utility;

internal static class NumericExtensions
{
    public static float ToRadians(float val) => (float)Math.PI / 180.0f * val;
}
