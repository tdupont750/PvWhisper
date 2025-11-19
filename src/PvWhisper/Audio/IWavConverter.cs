namespace PvWhisper.Audio;

public interface IWavConverter
{
    MemoryStream CreateWavFromPcm16(short[] samples, int sampleRate);
}