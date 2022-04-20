#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFSEngine.Animation.ChannelDrivers;

public class AudioChannelDriver
{
    public int AudioChannelIndex { get; set; } = 0;
    public bool UsePitch { get; set; } = true;
    public double MinFrequency { get; set; } = 20;
    public double MaxFrequency { get; set; } = 20000;
    public double EffectMultiplier { get; set; } = 1;
    //TODO: add offset, decay, smooth params

    private Func<AudioChannelDriver, double, float>? _sampler = null;

    public void SetSamplerFunction(Func<AudioChannelDriver, double, float> sampler)
    {
        _sampler = sampler;
    }

    public double Apply(double inputValue, double t)
    {
        if (_sampler is null)
            return inputValue;
        return inputValue + EffectMultiplier * _sampler(this, t);
    }

}
