namespace PvWhisper.Audio;

public interface ICaptureManager
{
    bool IsCapturing { get; }
    Task StartCaptureAsync();
    Task StopCaptureAndDiscardAsync();
    Task<short[]?> StopCaptureAndGetSamplesAsync();
}