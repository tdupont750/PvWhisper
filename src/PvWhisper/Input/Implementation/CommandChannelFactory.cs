using System.Threading.Channels;
using PvWhisper.Config;
using PvWhisper.Input;
using PvWhisper.Input.Sources;
using PvWhisper.Input.Sources.Implementation;
using PvWhisper.Logging;

namespace PvWhisper.Input.Implementation;

public sealed class CommandChannelFactory : ICommandChannelFactory
{
    private readonly AppConfig _config;
    private readonly ILogger _logger;

    public CommandChannelFactory(AppConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    public CommandChannelResult CreateChannelAndStartProducers(CancellationToken token)
    {
        var channel = Channel.CreateUnbounded<char>();

        // Console input
        var consoleSource = new ConsoleCommandSource();
        var consoleProducer = Task.Run(
            () => ProduceCommandsAsync(consoleSource, channel.Writer, token),
            token);

        // Optional HTTP input (simultaneous)
        Task? httpProducer = null;
        if (_config.HasHttpSource)
        {
            var httpSource = new HttpCommandSource(_config.HttpPort, _logger);
            httpProducer = Task.Run(
                () => ProduceCommandsAsync(httpSource, channel.Writer, token),
                token);
        }

        return new CommandChannelResult(channel, consoleProducer, httpProducer);
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
