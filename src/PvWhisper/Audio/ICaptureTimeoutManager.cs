namespace PvWhisper.Audio;

public interface ICaptureTimeoutManager : IDisposable
{
    Task RestartTimeoutAsync();
    void Cancel();
}
