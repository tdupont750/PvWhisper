using System.Text;
using PvWhisper.Audio;
using PvWhisper.Text;
using Whisper.net;

namespace PvWhisper.Transcription;

public sealed class WhisperTranscriber : IWhisperTranscriber
{
    private const int SampleRate = 16000;
    private readonly WhisperProcessor _processor;
    private readonly IWavConverter _wavConverter;
    private readonly ITextTransformer _textTransformer;

    public WhisperTranscriber(WhisperProcessor processor, ITextTransformer textTransformer, IWavConverter? wavHelper = null)
    {
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _textTransformer = textTransformer ?? throw new ArgumentNullException(nameof(textTransformer));
        _wavConverter = wavHelper ?? new WavConverter();
    }

    public async Task<string> TranscribeAsync(short[] samples, CancellationToken token)
    {
        if (samples == null || samples.Length == 0)
            return string.Empty;

        using var wavStream = _wavConverter.CreateWavFromPcm16(samples, SampleRate);
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

        var raw = sb.ToString().Trim();
        // Apply text transformation inside the transcriber to decouple callers
        return _textTransformer.Transform(raw);
    }
}