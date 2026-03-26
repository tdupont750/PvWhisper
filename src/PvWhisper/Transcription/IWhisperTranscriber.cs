using PvWhisper.Audio;

namespace PvWhisper.Transcription;

public interface IWhisperTranscriber
{
    Task<string> TranscribeAsync(AudioBuffer audio, CancellationToken token);
}
