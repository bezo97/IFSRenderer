#nullable enable
using IFSEngine.Animation.ChannelDrivers;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFSEngine.Animation;

public class Channel
{
    public SortedDictionary<double, Keyframe> Keyframes { get; init; } = new();
    public AudioChannelDriver? AudioChannelDriver { get; set; } = null;

    public Channel() { }
    public Channel(Keyframe keyframe)
    {
        Keyframes[keyframe.t] = keyframe;
    }

    public void AddKeyframe(Keyframe keyframe)
    {
        Keyframes[keyframe.t] = keyframe;
    }

    public double EvaluateAt(double t)
    {
        //var interpolationMode = _keyframes.Where(c => c.t < t).MaxBy(c=>c.t).InterpolationMode;
        //TODO: Interpolation Mode

        double eval = new LinearCurveImplementation().Evaluate(t, Keyframes.Values.ToList());

        if(AudioChannelDriver is not null)
        {
            eval = AudioChannelDriver.Apply(eval, t);
        }

        return eval;
    }

}
