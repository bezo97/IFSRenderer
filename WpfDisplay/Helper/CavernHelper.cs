using Cavern;
using Cavern.Utilities;
using IFSEngine.Animation.ChannelDrivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDisplay.Helper;

internal static class CavernHelper
{
    /// <summary>
    /// <a href="https://github.com/VoidXH/Cavern">Cavern repo</a>
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="cache"></param>
    /// <param name="minFreq"></param>
    /// <param name="maxFreq"></param>
    /// <param name="t"></param>
    /// <returns>Audio sample, a value between 0-1</returns>
    public static float CavernSampler(Clip clip, FFTCache cache, double minFreq, double maxFreq, double t)
    {
        //TODO: channelProrotopye
        //var channelsMatrix = Cavern.Remapping.ChannelPrototype.GetStandardMatrix(clip.Channels);
        //channelsMatrix[0].ToString();//-> front left

        //TODO: clip.Length; (seconds)
        int position = (int)(t * clip.SampleRate);
        float[] samples = new float[512];//2 hatványa!
        clip.GetData(samples, 0/*left channel*/, position);

        var spectrum = Measurements.FFT1D(samples, cache);
        //0 - clip.samplingRate (=maxfreq)
        int startBin = (int)(minFreq / clip.SampleRate * samples.Length);
        int endBin = (int)(maxFreq / clip.SampleRate * samples.Length);

        return samples[startBin..endBin].Max();//TODO: use spectrum
    }
}
