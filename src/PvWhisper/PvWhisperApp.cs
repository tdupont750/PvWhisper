using System.Threading.Channels;
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
    private readonly IWhisperTranscriber _transcriber;
    private readonly ITextTransformer _textTransformer;
    private readonly IOutputDispatcher _outputDispatcher;
    private readonly ILogger _logger;

    public PvWhisperApp(
        AppConfig config,
        ICaptureManager captureManager,
        IWhisperTranscriber transcriber,
        ITextTransformer textTransformer,
        IOutputDispatcher outputDispatcher,
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
        
        // Ensure pipe producer finishes (non-blocking source)
        if (pipeProducer is { IsCompleted: false })
        {
            // Pipe producer may be deadlocked trying to read an empty pipe,
            // so wait up to 5 seconds for it to finish
            var shutdownTask = Task.Delay(TimeSpan.FromSeconds(2));
            await Task.WhenAny(shutdownTask, pipeProducer);
            if (!pipeProducer.IsCompleted)
            {
                _logger.Warn("Pipe producer did not shut down in time; ignoring and continuing with shutdown.");
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
            HandleStopAndTranscribeAsync,
            _logger);

        while (await reader.WaitToReadAsync(appCts.Token))
        {
            while (reader.TryRead(out var raw))
            {
                TryToggleCaptureStatusIndicator(true);
                
                var cmd = char.ToLowerInvariant(raw);

                if (cmd == 'q')
                {
                    _logger.Info("Stopping...");
                    timeoutManager.Cancel();
                    await _captureManager.StopCaptureAndDiscardAsync();
                    await appCts.CancelAsync();
                    return;
                }

                switch (cmd)
                {
                    case 'v' when _captureManager.IsCapturing:
                        timeoutManager.Cancel();
                        await HandleStopAndTranscribeAsync(appCts.Token);
                        break;
                    case 'v':
                        await HandleStartCaptureAsync();
                        _ = timeoutManager.ArmAsync();
                        break;
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
                    case 'i':
                        _logger.Info("Pipe inintialized.");
                        break;
                    default:
                        _logger.Warn($"Unknown command: '{cmd}'");
                        break;
                }
                
                TryToggleCaptureStatusIndicator(false);
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

    private void TryToggleCaptureStatusIndicator(bool clear)
    {
        if (!_captureManager.IsCapturing) return;
        
        const string captureStatusLine = "--- CAPTURING ---";

        if (clear)
        {
            var cursorPosition = Console.GetCursorPosition();
            Console.SetCursorPosition(0, cursorPosition.Top);
            Console.Write(new string(' ', captureStatusLine.Length));
            Console.SetCursorPosition(0, cursorPosition.Top);
        }
        else
        {
            var prevColor = Console.BackgroundColor;
            Console.BackgroundColor = ConsoleColor.Red;
            Console.Write(captureStatusLine);
            Console.BackgroundColor = prevColor;
        }
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
