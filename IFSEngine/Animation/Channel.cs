#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

using IFSEngine.Animation.ChannelDrivers;

namespace IFSEngine.Animation;

public class Channel
{
    public string Name { get; set; } = "Unnamed channel";
    public List<Keyframe> Keyframes { get; init; } = [];
    public AudioChannelDriver? AudioChannelDriver { get; set; } = null;
    //TODO: add other channel drivers: clamp, wrap, repeat

    public Channel() { }
    public Channel(string name, Keyframe keyframe)
    {
        Name = name;
        Keyframes.Add(keyframe);
    }

    public double EvaluateAt(double t)
    {
        //TODO: avoid sorting every time
        var keyframes = Keyframes.OrderBy(k => k.t).ToList();

        double eval = 0;
        if (t >= keyframes.Last().t)
            eval = keyframes.Last().Value;
        else if (t <= keyframes.First().t)
            eval = keyframes.First().Value;
        else
        {//between keyframes
            var previousKeyframe = keyframes.Where(c => c.t <= t).MaxBy(c => c.t)!;
            var nextKeyframe = keyframes.Where(c => c.t > t).MinBy(c => c.t)!;

            var tNorm = (t - previousKeyframe.t) / (nextKeyframe.t - previousKeyframe.t);

            //easing
            double tEasing = previousKeyframe.EasingDirection switch
            {
                EasingDirection.In => Math.Pow(tNorm, previousKeyframe.EasingPower),
                EasingDirection.Out => 1.0 - Math.Pow(tNorm, previousKeyframe.EasingPower),
                EasingDirection.InOut => InOutPowerEasing(tNorm, previousKeyframe.EasingPower, nextKeyframe.EasingPower),
                _ => throw new NotImplementedException(),
            };

            //interpolation
            if (previousKeyframe.InterpolationMode == InterpolationMode.Linear)
            {
                eval = (1.0 - tEasing) * previousKeyframe.Value + tEasing * nextKeyframe.Value;
            }
            else if (previousKeyframe.InterpolationMode == InterpolationMode.Constant)
            {
                eval = previousKeyframe.Value;
            }
            else if (previousKeyframe.InterpolationMode == InterpolationMode.CatmullRom)
            {
                var p0 = keyframes.ElementAtOrDefault(keyframes.IndexOf(previousKeyframe) - 1) ?? previousKeyframe;
                var p3 = keyframes.ElementAtOrDefault(keyframes.IndexOf(nextKeyframe) + 1) ?? nextKeyframe;
                eval = CatmullRom(p0.Value, previousKeyframe.Value, nextKeyframe.Value, p3.Value, tEasing);
            }
            else throw new NotImplementedException();
        }

        if (AudioChannelDriver is not null)
        {
            eval = AudioChannelDriver.Apply(eval, t);
        }

        return eval;
    }

    private static double InOutPowerEasing(double t, double pow1, double pow2)
    {
        if (t < 0.5)
            return Math.Pow(t * 2.0, pow1) / 2.0;
        return 1.0 - Math.Pow((1.0 - t) * 2.0, pow2) / 2.0;
    }

    private static double CatmullRom(double p0, double p1, double p2, double p3, double t)
    {
        var h1 = 2 * t * t * t - 3 * t * t + 1;
        var h2 = -2 * t * t * t + 3 * t * t;
        var h3 = t * t * t - 2 * t * t + t;
        var h4 = t * t * t - t * t;
        var t1 = (p2 - p0) / 2;
        var t2 = (p3 - p1) / 2;
        return h1 * p1 + h2 * p2 + h3 * t1 + h4 * t2;
    }

}
