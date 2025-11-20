using System.Text;
using Pv;
using PvWhisper.Audio;
using PvWhisper.Config;
using PvWhisper.Logging;
using PvWhisper.Output;
using PvWhisper.Text;
using PvWhisper.Transcription;
using Whisper.net;

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

            var wavHelper = new WavConverter();
            var transcriber = new WhisperTranscriber(processor, wavHelper);
            var outputDispatcher = new OutputDispatcher(config.Outputs, logger);
            var regexReplacer = new RegexReplacer();
            var textTransformer = new TextTransformer(config.TextTransforms, regexReplacer);

            var deviceIndex = config.DeviceIndex ?? -1;

            LogAvailableDevices(deviceIndex, logger);

            var captureManager = new CaptureManager(deviceIndex, frameLength: 512, logger);

            var app = new PvWhisperApp(
                config,
                captureManager,
                transcriber,
                textTransformer,
                outputDispatcher,
                logger);

            var exitCode = await app.RunAsync(appCts);
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
        logger.Debug("Commands:");
        logger.Debug("  v = toggle capture (start / stop + transcribe)");
        logger.Debug("  c = start capture");
        logger.Debug("  z = stop capture and discard audio");
        logger.Debug("  x = stop capture and transcribe");
        logger.Debug("  q = quit");
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
    
}