using Cavern;
using Cavern.Format;
using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;

using WpfDisplay.Helper;

namespace WpfDisplay.Models;

public interface IAudio
{
    float Length { get; }
    int SampleRate { get; }

    float[] GetLogSpectrum(int channelId, double t, double startFreq, double endFreq);
}

public class CavernAudio(string audioFilePath) : IAudio
{
    public Clip Clip { get; } = AudioReader.ReadClip(audioFilePath);
    public FFTCache Cache { get; } = new(CavernHelper.defaultSamplingResolution);

    public float Length => Clip.Length;
    public int SampleRate => Clip.SampleRate;

    public float[] GetLogSpectrum(int channelId, double t, double startFreq, double endFreq)
    {
        var spectrum = CavernHelper.CavernSpectrum(this, channelId, t);
        return GraphUtils.ConvertToGraph(spectrum, startFreq, endFreq, SampleRate, spectrum.Length);
    }
}
