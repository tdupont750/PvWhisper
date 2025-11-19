namespace PvWhisper.Transcription;

public interface IWhisperTranscriber
{
    Task<string> TranscribeAsync(short[] samples, CancellationToken token);
}