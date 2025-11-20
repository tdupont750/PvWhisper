using PvWhisper.Text;

namespace PvWhisper.Config;

// Acts as the single DTO for both defaults deserialization and the final configuration
public sealed class AppConfig
{
    public bool HasPipeSource => !string.IsNullOrWhiteSpace(PipePath);
    
    public int? DeviceIndex { get; set; }
    public string? PipePath { get; set; }

    public string Language { get; set; } = "en";

    public ModelKind ModelType { get; set; } = ModelKind.Base;

    // Directory where model files are stored/loaded
    public string? ModelDir { get; set; }

    // Ordered text transforms applied to transcribed text
    public List<TextTransformConfig>? TextTransforms { get; set; }

    public IReadOnlyCollection<OutputTarget> Outputs { get; set; } =
        new[] { OutputTarget.Console };

    // Max time to keep capturing audio before auto-stopping (in seconds)
    public int CaptureTimeoutSeconds { get; set; } = 60;
}