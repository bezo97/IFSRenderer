#nullable enable
using System;

namespace IFSEngine.Animation.ChannelDrivers;

public class AudioChannelDriver
{
    public int AudioChannelId { get; set; } = 0;
    public bool UsePitch { get; set; } = true;
    public double MinFrequency { get; set; } = 0;
    public double MaxFrequency { get; set; } = 20000;
    public double EffectMultiplier { get; set; } = 1;
    //TODO: add offset, decay, smooth params

    private Func<AudioChannelDriver, double, float>? _sampler = null;

    public void SetSamplerFunction(Func<AudioChannelDriver, double, float> sampler) => _sampler = sampler;

    public double Apply(double inputValue, double t) => _sampler is null ? inputValue : inputValue + EffectMultiplier * _sampler(this, t);

}
