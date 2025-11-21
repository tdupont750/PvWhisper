using System.Threading.Channels;
using PvWhisper.Config;
using PvWhisper.Input.Sources;
using PvWhisper.Logging;

namespace PvWhisper.Input;

public sealed class CommandChannelFactory : ICommandChannelFactory
{
    private readonly AppConfig _config;
    private readonly ILogger _logger;

    public CommandChannelFactory(AppConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    public (Channel<char> channel, Task consoleProducer, Task? pipeProducer) CreateChannelAndStartProducers(
        CancellationToken token)
    {
        var channel = Channel.CreateUnbounded<char>();

        // Console input
        var consoleSource = new ConsoleCommandSource();
        var consoleProducer = Task.Run(
            () => ProduceCommandsAsync(consoleSource, channel.Writer, token),
            token);

        // Optional pipe input (simultaneous)
        Task? pipeProducer = null;
        if (_config.HasPipeSource)
        {
            var pipeSource = new PipeCommandSource(_config.PipePath!);
            pipeProducer = Task.Run(
                () => ProduceCommandsAsync(pipeSource, channel.Writer, token),
                token);
        }

        return (channel, consoleProducer, pipeProducer);
    }

    private async Task ProduceCommandsAsync(
        ICommandSource source,
        ChannelWriter<char> writer,
        CancellationToken token)
    {
        try
        {
            await foreach (var cmd in source.ReadCommandsAsync(token))
            {
                if (!writer.TryWrite(cmd))
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // normal
        }
        catch (Exception ex)
        {
            _logger.Warn($"Command source error: {ex.Message}");
        }
    }
}
