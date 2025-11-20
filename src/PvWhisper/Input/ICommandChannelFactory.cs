using System.Threading.Channels;

namespace PvWhisper.Input;

/// <summary>
/// Creates the command channel and starts input producers (console and optional named pipe).
/// Extracted from PvWhisperApp to separate concerns.
/// </summary>
public interface ICommandChannelFactory
{
    (Channel<char> channel, Task consoleProducer, Task? pipeProducer) CreateChannelAndStartProducers(
        CancellationToken token);
}