namespace PvWhisper.Audio;

public interface IDeviceResolver
{
    /// <summary>
    /// Resolves the audio device index to use for capture.
    /// Stateful: logs a warning if the resolved index changes between calls.
    /// </summary>
    int Resolve();

    /// <summary>
    /// Logs all available audio input devices and the currently selected one.
    /// </summary>
    void LogAvailable();
}
