using System.Text;
using System.Threading.Channels;
using Pv;
using PvWhisper.Audio;
using PvWhisper.Config;
using PvWhisper.Input;
using PvWhisper.Output;
using PvWhisper.Text;
using PvWhisper.Transcription;
using Whisper.net;
using PvWhisper.Logging;

namespace PvWhisper;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var logger = new Logger();
        var configService = new ConfigService(logger);

        AppConfig config;
        try
        {
            config = configService.Load();
        }
        catch (Exception ex)
        {
            logger.Error($"Configuration error: {ex.Message}");
            return 1;
        }

        PrintStartupInfo(logger);

        using var appCts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            appCts.Cancel();
        };

        try
        {
            var exitCode = await RunAsync(config, appCts, logger);
            logger.Info("PvWhisper stopped.");
            return exitCode;
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown
        }
        catch (Exception ex)
        {
            logger.Error("Fatal error:");
            logger.Error(ex);
            return 1;
        }

        logger.Info("PvWhisper stopped.");
        return 0;
    }

    private static void PrintStartupInfo(ILogger logger)
    {
        logger.Info("PvWhisper – PvRecorder + Whisper.net");
        logger.Info("Commands:");
        logger.Info("  v = toggle capture (start / stop + transcribe)");
        logger.Info("  c = start capture");
        logger.Info("  z = stop capture and discard audio");
        logger.Info("  x = stop capture and transcribe");
        logger.Info("  q = quit");
        logger.Info("  Ctrl+C = quit");
    }

    private static async Task<int> RunAsync(AppConfig config, CancellationTokenSource appCts, ILogger logger)
    {
        // Require explicit model directory; do not create or default
        if (string.IsNullOrWhiteSpace(config.ModelDir))
        {
            logger.Error("Model directory is required. Set 'modelDir' in AppConfig.json.");
            return 1;
        }

        // Ensure model exists in the provided modelDir based on ModelType
        var modelEnsurer = new ModelEnsurer(logger);
        var modelPath = await modelEnsurer.EnsureModelAsync(config.ModelType, config.ModelDir, appCts.Token);

        using var whisperFactory = WhisperFactory.FromPath(modelPath);
        await using var processor = whisperFactory
            .CreateBuilder()
            .WithLanguage(config.Language)
            .WithThreads(8)
            .Build();

        var wavHelper = new WavHelper();
        var transcriber = new WhisperTranscriber(processor, wavHelper);
        var outputDispatcher = new OutputDispatcher(config.Outputs, logger);
        var regexReplacer = new RegexReplacer();
        var textTransformer = new TextTransformer(config.TextTransforms, regexReplacer);

        var deviceIndex = config.DeviceIndex ?? -1;

        LogAvailableDevices(deviceIndex, logger);

        var captureManager = new CaptureManager(deviceIndex, frameLength: 512, logger);

        var (channel, consoleProducer, pipeProducer) = CreateChannelAndStartProducers(config, appCts.Token, logger);

        await ProcessCommandsAsync(
            channel.Reader,
            captureManager,
            transcriber,
            textTransformer,
            outputDispatcher,
            config,
            appCts,
            logger);

        channel.Writer.TryComplete();
        await consoleProducer;
        if (pipeProducer != null)
            await pipeProducer;

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

    private static void LogAvailableDevices(int deviceIndex, ILogger logger)
    {
        logger.Debug("Available input devices:");
        var devices = PvRecorder.GetAvailableDevices();
        for (var i = 0; i < devices.Length; i++)
        {
            logger.Debug($"  [{i}] {devices[i]}");
        }
        var selectedDeviceName = (deviceIndex >= 0 && deviceIndex < devices.Length)
            ? devices[deviceIndex]
            : "System default";
        logger.Info($"Using device index {deviceIndex} -> {selectedDeviceName}");
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