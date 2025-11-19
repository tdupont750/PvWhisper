using System.Threading.Channels;
using Pv;
using PvWhisper.Audio;
using PvWhisper.Config;
using PvWhisper.Input;
using PvWhisper.Logging;
using PvWhisper.Output;
using PvWhisper.Text;
using PvWhisper.Transcription;

namespace PvWhisper;

/// <summary>
/// Encapsulates the main application loop and command processing for PvWhisper.
/// Program.Main is responsible for constructing dependencies and invoking this app.
/// </summary>
public sealed class PvWhisperApp
{
    private readonly AppConfig _config;
    private readonly CaptureManager _captureManager;
    private readonly WhisperTranscriber _transcriber;
    private readonly TextTransformer _textTransformer;
    private readonly OutputDispatcher _outputDispatcher;
    private readonly ILogger _logger;

    public PvWhisperApp(
        AppConfig config,
        CaptureManager captureManager,
        WhisperTranscriber transcriber,
        TextTransformer textTransformer,
        OutputDispatcher outputDispatcher,
        ILogger logger)
    {
        _config = config;
        _captureManager = captureManager;
        _transcriber = transcriber;
        _textTransformer = textTransformer;
        _outputDispatcher = outputDispatcher;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationTokenSource appCts)
    {
        var (channel, consoleProducer, pipeProducer) = CreateChannelAndStartProducers(_config, appCts.Token, _logger);

        await ProcessCommandsAsync(
            channel.Reader,
            _captureManager,
            _transcriber,
            _textTransformer,
            _outputDispatcher,
            _config,
            appCts,
            _logger);

        // Signal producers that no more writes are accepted
        channel.Writer.TryComplete();

        // Ensure console producer finishes (non-blocking source)
        await consoleProducer;

        // Pipe producer may be reading a FIFO and not respond to cancellation promptly on some systems.
        // Do a bounded await; if it doesn't finish quickly, continue shutdown.
        if (pipeProducer != null)
        {
            _logger.Info("Waiting briefly for pipe input to stop...");
            var completed = await Task.WhenAny(pipeProducer, Task.Delay(TimeSpan.FromSeconds(1.5)));
            if (completed == pipeProducer)
            {
                // Observe any exceptions
                await pipeProducer;
            }
            else
            {
                _logger.Warn("Pipe input did not stop promptly; continuing shutdown.");
            }
        }

        return 0;
    }

    private static (Channel<char> channel, Task consoleProducer, Task? pipeProducer) CreateChannelAndStartProducers(
        AppConfig config,
        CancellationToken token,
        ILogger logger)
    {
        var channel = Channel.CreateUnbounded<char>();

        // Console input
        var consoleSource = new ConsoleCommandSource();
        var consoleProducer = Task.Run(
            () => ProduceCommandsAsync(consoleSource, channel.Writer, logger, token),
            token);

        // Optional pipe input (simultaneous)
        Task? pipeProducer = null;
        if (config.PipePath != null)
        {
            var pipeSource = new PipeCommandSource(config.PipePath);
            pipeProducer = Task.Run(
                () => ProduceCommandsAsync(pipeSource, channel.Writer, logger, token),
                token);

            logger.Info($"Pipe input enabled: {config.PipePath}");
        }

        return (channel, consoleProducer, pipeProducer);
    }

    private static async Task ProduceCommandsAsync(
        ICommandSource source,
        ChannelWriter<char> writer,
        ILogger logger,
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
            logger.Warn($"Command source error: {ex.Message}");
        }
    }

    private static async Task ProcessCommandsAsync(
        ChannelReader<char> reader,
        CaptureManager captureManager,
        WhisperTranscriber transcriber,
        TextTransformer textTransformer,
        OutputDispatcher outputDispatcher,
        AppConfig config,
        CancellationTokenSource appCts,
        ILogger logger)
    {
        // Dedicated capture timeout manager
        using var timeoutManager = new CaptureTimeoutManager(
            config.CaptureTimeoutSeconds,
            appCts.Token,
            () => captureManager.IsCapturing,
            ct => HandleStopAndTranscribeAsync(
                captureManager, transcriber, textTransformer, outputDispatcher, logger, ct));

        while (await reader.WaitToReadAsync(appCts.Token))
        {
            while (reader.TryRead(out var raw))
            {
                var cmd = char.ToLowerInvariant(raw);

                if (cmd is not ('c' or 'z' or 'x' or 'q' or 'v'))
                    continue;

                if (cmd == 'q')
                {
                    timeoutManager.Cancel();
                    await captureManager.StopCaptureAndDiscardAsync();
                    appCts.Cancel();
                    return;
                }

                if (cmd == 'v')
                {
                    if (captureManager.IsCapturing)
                    {
                        timeoutManager.Cancel();
                        await HandleStopAndTranscribeAsync(
                            captureManager, transcriber, textTransformer, outputDispatcher, logger, appCts.Token);
                    }
                    else
                    {
                        await HandleStartCaptureAsync(captureManager, logger);
                        _ = timeoutManager.ArmAsync();
                    }
                    continue;
                }

                switch (cmd)
                {
                    case 'c':
                        await HandleStartCaptureAsync(captureManager, logger);
                        _ = timeoutManager.ArmAsync();
                        break;

                    case 'z':
                        timeoutManager.Cancel();
                        await captureManager.StopCaptureAndDiscardAsync();
                        logger.Info("Capture stopped; audio discarded.");
                        break;

                    case 'x':
                        timeoutManager.Cancel();
                        await HandleStopAndTranscribeAsync(
                            captureManager, transcriber, textTransformer, outputDispatcher, logger, appCts.Token);
                        break;
                }
            }
        }
    }

    private static async Task HandleStartCaptureAsync(CaptureManager captureManager, ILogger logger)
    {
        if (captureManager.IsCapturing)
        {
            logger.Info("Already capturing; ignoring 'c'/'v start'.");
            return;
        }

        logger.Info("Starting capture...");
        await captureManager.StartCaptureAsync();
    }

    private static async Task HandleStopAndTranscribeAsync(
        CaptureManager captureManager,
        WhisperTranscriber transcriber,
        TextTransformer textTransformer,
        OutputDispatcher outputDispatcher,
        ILogger logger,
        CancellationToken token)
    {
        logger.Info("Stopping capture and transcribing...");
        var samples = await captureManager.StopCaptureAndGetSamplesAsync();

        if (samples == null || samples.Length == 0)
        {
            logger.Info("No audio captured to transcribe.");
            return;
        }

        try
        {
            var text = await transcriber.TranscribeAsync(samples, token);
            var transformed = textTransformer.Transform(text);

            await outputDispatcher.DispatchAsync(transformed, token);
        }
        catch (OperationCanceledException)
        {
            logger.Info("Transcription cancelled.");
        }
        catch (Exception ex)
        {
            logger.Error($"Transcription failed: {ex.Message}");
        }
    }
}
