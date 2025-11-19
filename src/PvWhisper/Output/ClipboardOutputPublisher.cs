using System;
using System.Threading;
using System.Threading.Tasks;
using PvWhisper.Logging;
using TextCopy;

namespace PvWhisper.Output;

public sealed class ClipboardOutputPublisher : IOutputPublisher
{
    public Task PublishAsync(string text, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Logger.Warn("Empty transcription; skipping clipboard write.");
            return Task.CompletedTask;
        }

        try
        {
            ClipboardService.SetText(text);
            Logger.Info("Transcription copied to clipboard.");
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to copy to clipboard: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
