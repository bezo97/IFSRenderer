using IFSEngine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFSEngine.Animation;

public class Dopesheet
{
    public List<PropertyAnimation> Channels { get; init; } = new();
    public TimeSpan Length { get; init; } = TimeSpan.FromSeconds(10);
    public int Fps { get; set; } = 30;

    public void EvaluateAt(IFS targetIfs, TimeOnly t)
    {
        foreach (var channel in Channels)
        {
            channel.EvaluateAt(targetIfs, t.ToTimeSpan() / TimeSpan.FromSeconds(1));
        }
    }

}
