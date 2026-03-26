namespace PvWhisper.Audio;

public interface ICaptureTimeoutManager : IDisposable
{
    /// <summary>
    /// Arms the timeout countdown. Cancels any previously armed countdown.
    /// Fires-and-forgets the delay internally; the caller does not await it.
    /// No-op if timeoutSeconds is non-positive.
    /// </summary>
    void RestartTimeout();
    void Cancel();
}
