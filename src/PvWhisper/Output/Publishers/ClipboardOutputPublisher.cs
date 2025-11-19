using PvWhisper.Logging;
using TextCopy;

namespace PvWhisper.Output.Publishers;

public sealed class ClipboardOutputPublisher : IOutputPublisher
{
    private readonly ILogger _logger;

    public ClipboardOutputPublisher(ILogger logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(string text, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.Warn("Empty transcription; skipping clipboard write.");
            return Task.CompletedTask;
        }

        try
        {
            ClipboardService.SetText(text);
            _logger.Info("Transcription copied to clipboard.");
        }
        catch (Exception ex)
        {
            _logger.Warn($"Failed to copy to clipboard: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
