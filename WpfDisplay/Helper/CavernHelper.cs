using Cavern.Utilities;
using System;
using System.Linq;
using WpfDisplay.Models;

namespace WpfDisplay.Helper;

internal static class CavernHelper
{
    //must be pow of 2
    public static readonly int defaultSamplingResolution = 2048;

    /// <summary>
    /// <a href="https://github.com/VoidXH/Cavern">Cavern repo</a>
    /// </summary>
    /// <param name="minFreq"></param>
    /// <param name="maxFreq"></param>
    /// <param name="t"></param>
    /// <returns>Audio sample, a value between 0-1</returns>
    public static float CavernSampler(CavernAudio audio, int channelId, double minFreq, double maxFreq, double t)
    {
        var spectrum = CavernSpectrum(audio, channelId, t);
        int startBin = (int)Math.Clamp(2 * minFreq / audio.SampleRate * spectrum.Length, 0, spectrum.Length-1);
        int endBin = (int)Math.Clamp(2 * maxFreq / audio.SampleRate * spectrum.Length, 0, spectrum.Length);
        endBin = Math.Clamp(endBin, startBin+1, spectrum.Length);
        return spectrum[startBin..endBin].Max();
    }

    public static float[] CavernSpectrum(CavernAudio audio, int channelId, double t)
    {
        t = Math.Clamp(t, 0, audio.Length);
        int samplePosition = (int)(t * audio.SampleRate);
        float[] samples = new float[defaultSamplingResolution];
        audio.Clip.GetData(samples, channelId, samplePosition);
        var spectrum = Measurements.GetSpectrum(Measurements.FFT(samples.Select(r => new Complex(r, 0)).ToArray(), audio.Cache));
        WaveformUtils.Gain(spectrum, 1.0f / spectrum.Length);
        return spectrum;
    }
}
