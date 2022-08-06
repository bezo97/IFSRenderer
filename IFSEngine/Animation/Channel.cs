#nullable enable
using IFSEngine.Animation.ChannelDrivers;
using IFSEngine.Utility;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFSEngine.Animation;

public class Channel
{
    public List<Keyframe> Keyframes { get; init; } = new();
    public AudioChannelDriver? AudioChannelDriver { get; set; } = null;
    //TODO: add other channel drivers: clamp, wrap, repeat

    public Channel() { }
    public Channel(Keyframe keyframe)
    {
        Keyframes.Add(keyframe);
    }

    public double EvaluateAt(double t)
    {
        var keyframes = Keyframes.OrderBy(k => k.t);

        if (t >= keyframes.Last().t)
            return keyframes.Last().Value;
        if (t <= keyframes.First().t)
            return keyframes.First().Value;

        var previousKeyframe = keyframes.Where(c => c.t < t).MaxBy(c => c.t)!;
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
        double eval = MathExtensions.Lerp(previousKeyframe.Value, nextKeyframe.Value, tEasing);

        if(AudioChannelDriver is not null)
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

}
