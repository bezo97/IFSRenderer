using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Utility;

public static class MathExtensions
{
    public static float Lerp(float a, float b, float t) => t * b + (1.0f - t) * a;
    public static double Lerp(double a, double b, double t) => t * b + (1.0 - t) * a;
    public static T Lerp<T>(T a, T b, float t) => t * (b as dynamic) + (1.0f - t) * (a as dynamic);
    public static double Remap(this double from, double fromMin, double fromMax, double toMin, double toMax)
    {
        var fromAbs = from - fromMin;
        var fromMaxAbs = fromMax - fromMin;

        var normal = fromAbs / fromMaxAbs;

        var toMaxAbs = toMax - toMin;
        var toAbs = toMaxAbs * normal;

        var to = toAbs + toMin;
        return to;
    }
    public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
    {
        if (val.CompareTo(min) < 0) return min;
        else if (val.CompareTo(max) > 0) return max;
        else return val;
    }
}
