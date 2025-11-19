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
    private readonly ICaptureManager _captureManager;
    private readonly WhisperTranscriber _transcriber;
    private readonly TextTransformer _textTransformer;
    private readonly OutputDispatcher _outputDispatcher;
    private readonly ILogger _logger;

    public PvWhisperApp(
        AppConfig config,
        ICaptureManager captureManager,
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
        var (channel, consoleProducer, pipeProducer) = CreateChannelAndStartProducers(appCts.Token);

        await ProcessCommandsAsync(channel.Reader, appCts);

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

    private (Channel<char> channel, Task consoleProducer, Task? pipeProducer) CreateChannelAndStartProducers(
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
        if (_config.PipePath != null)
        {
            var pipeSource = new PipeCommandSource(_config.PipePath);
            pipeProducer = Task.Run(
                () => ProduceCommandsAsync(pipeSource, channel.Writer, token),
                token);

            _logger.Info($"Pipe input enabled: {_config.PipePath}");
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

    private async Task ProcessCommandsAsync(
        ChannelReader<char> reader,
        CancellationTokenSource appCts)
    {
        // Dedicated capture timeout manager
        using var timeoutManager = new CaptureTimeoutManager(
            _config.CaptureTimeoutSeconds,
            appCts.Token,
            () => _captureManager.IsCapturing,
            ct => HandleStopAndTranscribeAsync(ct),
            _logger);

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
                    await _captureManager.StopCaptureAndDiscardAsync();
                    appCts.Cancel();
                    return;
                }

                if (cmd == 'v')
                {
                    if (_captureManager.IsCapturing)
                    {
                        timeoutManager.Cancel();
                        await HandleStopAndTranscribeAsync(appCts.Token);
                    }
                    else
                    {
                        await HandleStartCaptureAsync();
                        _ = timeoutManager.ArmAsync();
                    }
                    continue;
                }

                switch (cmd)
                {
                    case 'c':
                        await HandleStartCaptureAsync();
                        _ = timeoutManager.ArmAsync();
                        break;

                    case 'z':
                        timeoutManager.Cancel();
                        await _captureManager.StopCaptureAndDiscardAsync();
                        _logger.Info("Capture stopped; audio discarded.");
                        break;

                    case 'x':
                        timeoutManager.Cancel();
                        await HandleStopAndTranscribeAsync(appCts.Token);
                        break;
                }
            }
        }
    }

    private async Task HandleStartCaptureAsync()
    {
        if (_captureManager.IsCapturing)
        {
            _logger.Info("Already capturing; ignoring 'c'/'v start'.");
            return;
        }

        _logger.Info("Starting capture...");
        await _captureManager.StartCaptureAsync();
    }

    private async Task HandleStopAndTranscribeAsync(CancellationToken token)
    {
        _logger.Info("Stopping capture and transcribing...");
        var samples = await _captureManager.StopCaptureAndGetSamplesAsync();

        if (samples == null || samples.Length == 0)
        {
            _logger.Info("No audio captured to transcribe.");
            return;
        }

        try
        {
            var text = await _transcriber.TranscribeAsync(samples, token);
            var transformed = _textTransformer.Transform(text);

            await _outputDispatcher.DispatchAsync(transformed, token);
        }
        catch (OperationCanceledException)
        {
            _logger.Info("Transcription cancelled.");
        }
        catch (Exception ex)
        {
            _logger.Error($"Transcription failed: {ex.Message}");
        }
    }
}
