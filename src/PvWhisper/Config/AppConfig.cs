using PvWhisper.Text;

namespace PvWhisper.Config;

// Acts as the single DTO for both defaults deserialization and the final configuration
public sealed class AppConfig
{
    public bool HasHttpSource => HttpPort > 0;

    public int? DeviceIndex { get; set; }
    public string? DeviceName { get; set; }
    public int HttpPort { get; set; } = 5000;

    public string Language { get; set; } = "en";

    public ModelKind ModelType { get; set; } = ModelKind.Base;

    // Directory where model files are stored/loaded
    public string? ModelDir { get; set; }

    // Number of threads to use by Whisper for processing
    public int WhisperThreads { get; set; } = 8;

    // Number of audio frames per read from the microphone
    public int FrameLength { get; set; } = 512;

    // Ordered text transforms applied to transcribed text
    public List<TextTransformConfig>? TextTransforms { get; set; }

    public IReadOnlyCollection<OutputTarget> Outputs { get; set; } =
        new[] { OutputTarget.Console };

    // Max time to keep capturing audio before auto-stopping (in seconds)
    public int CaptureTimeoutSeconds { get; set; } = 60;
}
