#nullable enable
using IFSEngine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IFSEngine.Animation;

public class Dopesheet
{
    public static double T = 0.0;
    public Dictionary<string, Channel> Channels { get; init; } = new();
    public TimeSpan Length { get; set; } = TimeSpan.FromSeconds(10);//should be private set!
    public int Fps { get; set; } = 30;

    public void AddOrUpdateChannel(string path, double value)
    {
        var keyframe = new Keyframe
        {
            InterpolationMode = "Linear",
            t = Dopesheet.T,
            Value = value
        };

        if (Channels.TryGetValue(path, out var channel))
            channel.AddKeyframe(keyframe);
        else
            Channels[path] = new Channel(keyframe);

    }

    //public void AddOrUpdateChannel(AnimatedValue av, double value)
    //{
    //    if (av.Channel is null)
    //        av.Channel = new Channel();
    //    av.Channel.AddKeyframe(new Keyframe
    //    {

    //    });
    //}

    //public void AddOrUpdateChannel<T>(System.Linq.Expressions.Expression<Func<T>> property, double value)
    //{
    //    System.Diagnostics.Debug.WriteLine(property.Body.ToString());
    //    //if(Channels.Exists(a=>a.PropertyPath == property.Body.ToString()))
    //    //{

    //    //}
    //}

    public void EvaluateAt(IFS targetIfs, TimeOnly t)
    {

        foreach (var (path, channel) in Channels)
        {
            var val = channel.EvaluateAt(t.ToTimeSpan() / TimeSpan.FromSeconds(1));

            //[45][5].Brightness.X
            //[45][5]
            //[2].Brightness
            //[1].Orientation.X
            var m = Regex.Match(path, @"^(?:\[(\d+)\])*(?:\.?(\w+))+");
            string? iteratorIndex = m.Groups[1].Captures.FirstOrDefault()?.Value;
            string? xaosToIndex = m.Groups[1].Captures.Skip(1).FirstOrDefault()?.Value;
            string? propName = m.Groups[2].Captures.FirstOrDefault()?.Value;
            string? propField = m.Groups[2].Captures.Skip(1).FirstOrDefault()?.Value;

            if (xaosToIndex is not null)
            {//xaos weight animation
                int fromIndex = int.Parse(iteratorIndex!);
                int toIndex = int.Parse(xaosToIndex);
                var listHack = targetIfs.Iterators.ToList();
                listHack[fromIndex].WeightTo[listHack[toIndex]] = 0;
            }
            else if (iteratorIndex is not null)
            {//specific iterator property or parameter
                int indexer = int.Parse(iteratorIndex);
                Iterator it = targetIfs.Iterators.ToList()[indexer];

                if (typeof(Iterator).GetProperty(propName!) is var iteratorprop && iteratorprop is not null)
                {//iterator property
                    if (propField is not null)
                    {
                        object propVal = iteratorprop.GetValue(it, null)!;
                        iteratorprop.GetType().GetField(propField)!.SetValue(propVal, val);
                    }
                    else
                        iteratorprop.SetValue(it, val, null);
                }
                else
                {//transform parameter
                    if (propField is not null)
                        typeof(Vector3).GetField(propField)!.SetValue(it.Vec3Params[propName], val);
                    else
                        it.RealParams[propName] = val;
                }
            }
            else
            {//simple property
                if (typeof(IFS).GetProperty(path) is var ifsprop && ifsprop is not null)
                {
                    if (propField is not null)
                    {
                        object propVal = ifsprop.GetValue(targetIfs, null)!;
                        ifsprop.GetType().GetField(propField)!.SetValue(propVal, val);
                    }
                    else
                        ifsprop.SetValue(targetIfs, val, null);
                }
                else if (typeof(Camera).GetProperty(path) is var cameraprop && cameraprop is not null)
                {
                    if (propField is not null)
                    {
                        object propVal = cameraprop.GetValue(targetIfs.Camera, null)!;
                        cameraprop.GetType().GetField(propField)!.SetValue(propVal, val);
                    }
                    else
                        cameraprop.SetValue(targetIfs.Camera, val, null);
                }
            }

            //if(Regex.IsMatch(path, "^[^.[]*$"))
            //{//simple prop name
            //    if (typeof(IFS).GetProperty(path) is var ifsprop && ifsprop is not null)
            //        ifsprop.SetValue(targetIfs, val, null);
            //    else if (typeof(Camera).GetProperty(path) is var cameraprop && cameraprop is not null)
            //        cameraprop.SetValue(targetIfs.Camera, val, null);
            //}
            //else if (Regex.IsMatch(path, "^[^.[]*$"))
            //{//simple prop name
            //    if (typeof(IFS).GetProperty(path) is var ifsprop && ifsprop is not null)
            //        ifsprop.SetValue(targetIfs, val, null);
            //    else if (typeof(Camera).GetProperty(path) is var cameraprop && cameraprop is not null)
            //        cameraprop.SetValue(targetIfs.Camera, val, null);
            //}

            ////parse path
            //string[] pathParts = path.Split('.');
            //if (pathParts.Length == 1)
            //{
            //    //props
            //    if (typeof(IFS).GetProperty(path) is var ifsprop && ifsprop is not null)
            //        ifsprop.SetValue(targetIfs, val, null);
            //    else if (typeof(Camera).GetProperty(path) is var cameraprop && cameraprop is not null)
            //        cameraprop.SetValue(targetIfs.Camera, val, null);
            //}
            //else if(pathParts[0].StartsWith('['))
            //{
            //    //iterator indexer
            //    //[0].BaseWeight
            //    //[0].
            //    var indexer = int.Parse(pathParts[0].Trim('[', ']'));
            //    var propName = pathParts[1];
            //    if (typeof(Iterator).GetProperty(propName) is var iteratorprop && iteratorprop is not null)
            //        iteratorprop.SetValue(targetIfs.Iterators.ToList()[indexer], val, null);//
            //}

        }
    }

    public void SetLength(TimeSpan length)
    {
        Length = length;
        //
    }

}
