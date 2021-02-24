using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Utility
{
    internal static class MathExtensions
    {
        public static float Lerp(float a, float b, float t) => t * a + (1.0f - t) * b;
        public static bool IsPow2(int a) => (a & -a) == a;
    }
}
