using PvWhisper.Config;
using Whisper.net.Ggml;
using PvWhisper.Logging;

namespace PvWhisper.Transcription;

public sealed class ModelEnsurer
{
    private readonly ILogger _logger;

    public ModelEnsurer(ILogger logger)
    {
        _logger = logger;
    }

    private static string GetModelFileName(ModelKind modelKind)
    {
        return modelKind switch
        {
            ModelKind.Tiny   => "ggml-tiny.bin",
            ModelKind.Small  => "ggml-small.bin",
            ModelKind.Medium => "ggml-medium.bin",
            ModelKind.Large  => "ggml-large.bin",
            _                => "ggml-base.bin"
        };
    }

    public async Task<string> EnsureModelAsync(ModelKind modelKind, string modelDir, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(modelDir))
            throw new ArgumentException("Model directory must be provided", nameof(modelDir));

        var fileName = GetModelFileName(modelKind);
        if (!Directory.Exists(modelDir))
            throw new DirectoryNotFoundException($"Model directory does not exist: {modelDir}");
        var fullPath = Path.Combine(modelDir, fileName);

        if (File.Exists(fullPath))
        {
            _logger.Info($"Using existing model: {fullPath}");
            return fullPath;
        }

        _logger.Warn($"Model '{fullPath}' not found. Downloading Whisper {modelKind} model...");

        var ggmlType = modelKind switch
        {
            ModelKind.Tiny   => GgmlType.Tiny,
            ModelKind.Small  => GgmlType.Small,
            ModelKind.Medium => GgmlType.Medium,
            ModelKind.Large  => GgmlType.LargeV3Turbo,
            _                => GgmlType.Base
        };

        await using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(ggmlType, cancellationToken: token);
        await using var fileStream = File.Open(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await modelStream.CopyToAsync(fileStream, token);

        _logger.Info("Model downloaded.");

        return fullPath;
    }
}