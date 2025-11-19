using System.Text;

namespace PvWhisper.Audio;

public static class WavHelper
{
    public static MemoryStream CreateWavFromPcm16(short[] samples, int sampleRate)
    {
        var ms = new MemoryStream();

        int bytesPerSample = 2;
        short numChannels = 1;
        short bitsPerSample = 16;
        int byteRate = sampleRate * numChannels * bytesPerSample;
        short blockAlign = (short)(numChannels * bytesPerSample);
        int subchunk2Size = samples.Length * bytesPerSample;
        int chunkSize = 4 + (8 + 16) + (8 + subchunk2Size);

        using (var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(chunkSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(numChannels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write(bitsPerSample);

            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(subchunk2Size);

            foreach (var s in samples)
                writer.Write(s);
        }

        ms.Position = 0;
        return ms;
    }
}