using Cavern;
using Cavern.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDisplay.Helper;

internal static class CavernHelper
{
    //must be pow of 2
    public static readonly int defaultSamplingResolution = 512;

    /// <summary>
    /// <a href="https://github.com/VoidXH/Cavern">Cavern repo</a>
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="cache"></param>
    /// <param name="minFreq"></param>
    /// <param name="maxFreq"></param>
    /// <param name="t"></param>
    /// <returns>Audio sample, a value between 0-1</returns>
    public static float CavernSampler(Clip clip, FFTCache cache, int channelId, double minFreq, double maxFreq, double t)
    {
        t = Math.Clamp(t, 0, clip.Length);
        int samplePosition = (int)(t * clip.SampleRate);
        float[] samples = new float[defaultSamplingResolution];
        clip.GetData(samples, channelId, samplePosition);
        var spectrum = Measurements.FFT1D(samples, cache);

        int startBin = (int)(minFreq / clip.SampleRate * samples.Length);
        int endBin = (int)(maxFreq / clip.SampleRate * samples.Length);
        endBin = Math.Clamp(endBin, startBin+1, clip.SampleRate);

        return spectrum[startBin..endBin].Max();
    }

    public static float[] CavernSampler(Clip clip, FFTCache cache, int channelId, double t)
    {
        t = Math.Clamp(t, 0, clip.Length);
        int samplePosition = (int)(t * clip.SampleRate);
        float[] samples = new float[defaultSamplingResolution];
        clip.GetData(samples, channelId, samplePosition);
        var spectrum = Measurements.FFT1D(samples, cache);

        return spectrum;
    }
}
