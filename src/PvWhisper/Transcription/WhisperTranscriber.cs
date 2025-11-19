using System.Text;
using PvWhisper.Audio;
using Whisper.net;

namespace PvWhisper.Transcription;

public sealed class WhisperTranscriber
{
    private const int SampleRate = 16000;
    private readonly WhisperProcessor _processor;
    private readonly IWavHelper _wavHelper;

    public WhisperTranscriber(WhisperProcessor processor, IWavHelper? wavHelper = null)
    {
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _wavHelper = wavHelper ?? new WavHelper();
    }

    public async Task<string> TranscribeAsync(short[] samples, CancellationToken token)
    {
        if (samples == null || samples.Length == 0)
            return string.Empty;

        using var wavStream = _wavHelper.CreateWavFromPcm16(samples, SampleRate);
        var sb = new StringBuilder();

        await foreach (var segment in _processor.ProcessAsync(wavStream, token))
        {
            var text = segment.Text;
            if (string.IsNullOrWhiteSpace(text))
                continue;

            if (sb.Length > 0)
                sb.Append(' ');

            sb.Append(text.Trim());
        }

        return sb.ToString().Trim();
    }
}