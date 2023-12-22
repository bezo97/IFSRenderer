#nullable enable
using System;
using System.Collections.Generic;

using IFSEngine.Model;
using IFSEngine.Utility;

namespace IFSEngine.Animation;

public class Dopesheet
{
    public Dictionary<string, Channel> Channels { get; init; } = [];
    public TimeSpan Length { get; set; } = TimeSpan.FromSeconds(10);
    public int Fps { get; set; } = 30;

    private double keyframeEpsilon => 1.0 / Fps;

    public void AddOrUpdateChannel(string name, string path, TimeOnly time, double value)
    {
        var t = time.ToTimeSpan().TotalSeconds;
        if (Channels.TryGetValue(path, out var channel))
        {
            if (channel.Keyframes.Find(k => Math.Abs(k.t - t) <= keyframeEpsilon) is var kf and not null)
            {
                kf.Value = value;
            }
            else
            {
                channel.Keyframes.Add(new Keyframe
                {
                    t = t,
                    Value = value
                });
            }
        }
        else
        {
            Channels[path] = new Channel(name, new Keyframe
            {
                t = t,
                Value = value
            });
        }
    }

    public void EvaluateAt(IFS targetIfs, TimeOnly t)
    {

        foreach (var (path, channel) in Channels)
        {
            var val = channel.EvaluateAt(t.ToTimeSpan() / TimeSpan.FromSeconds(1));

            NestedReflectionHelper.SetMemberValueByPath(targetIfs, path, val);

        }
    }

}
