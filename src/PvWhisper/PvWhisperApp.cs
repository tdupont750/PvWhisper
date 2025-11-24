using System.Threading.Channels;
using PvWhisper.Audio;
using PvWhisper.Config;
using PvWhisper.Input;
using PvWhisper.Logging;
using PvWhisper.Output;
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
    private readonly IOutputDispatcher _outputDispatcher;
    private readonly ICommandChannelFactory _commandChannelFactory;
    private readonly ILogger _logger;

    public PvWhisperApp(
        AppConfig config,
        ICaptureManager captureManager,
        IWhisperTranscriber transcriber,
        IOutputDispatcher outputDispatcher,
        ICommandChannelFactory commandChannelFactory,
        ILogger logger)
    {
        _config = config;
        _captureManager = captureManager;
        _transcriber = transcriber;
        _outputDispatcher = outputDispatcher;
        _commandChannelFactory = commandChannelFactory;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationTokenSource appCts)
    {   
        // Create channel and start input producers via injected factory
        var (channel, consoleProducer, pipeProducer) = _commandChannelFactory.CreateChannelAndStartProducers(appCts.Token);

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

    private async Task ProcessCommandsAsync(
        ChannelReader<char> reader,
        CancellationTokenSource appCts)
    {
        // Dedicated capture timeout manager
        using var timeoutManager = new CaptureTimeoutManager(
            _config.CaptureTimeoutSeconds,
            appCts.Token,
            async () =>
            {
                if (_captureManager.IsCapturing)
                {
                    TryToggleCaptureStatusIndicator(false);
                    _logger.Warn($"Auto-stopping capture after {_config.CaptureTimeoutSeconds} seconds...");
                    await HandleStopAndTranscribeAsync(appCts.Token, true);
                }
            });

        while (await reader.WaitToReadAsync(appCts.Token))
        {
            while (reader.TryRead(out var raw))
            {
                TryToggleCaptureStatusIndicator(false);
                
                var cmd = char.ToLowerInvariant(raw);

                if (cmd == 'q')
                {
                    _logger.Debug("Stopping...");
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
                        _ = timeoutManager.RestartTimeoutAsync();
                        break;
                    case 'c':
                        await HandleStartCaptureAsync();
                        _ = timeoutManager.RestartTimeoutAsync();
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
                        _logger.Debug($"Reader - Successfully initialized pipe '{_config.PipePath}'");
                        break;
                    default:
                        _logger.Error($"Unknown command: '{cmd}'");
                        break;
                }
                
                TryToggleCaptureStatusIndicator(true);
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

    private void TryToggleCaptureStatusIndicator(bool isShow)
    {
        if (_captureManager.IsCapturing)
            _logger.ToggleAlert("--- CAPTURING ---", isShow);
    }
    
    private async Task HandleStopAndTranscribeAsync(CancellationToken token, bool discardTranscribe = false)
    {   
        _logger.Info("Stopping capture and transcribing...");
        var samples = await _captureManager.StopCaptureAndGetSamplesAsync();

        if (discardTranscribe || samples == null || samples.Length == 0)
        {
            _logger.Info("No audio captured to transcribe.");
            return;
        }

        try
        {
            var text = await _transcriber.TranscribeAsync(samples, token);

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.Debug("No text transcribed.");
                return;
            }
            
            await _outputDispatcher.DispatchAsync(text, token);
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
