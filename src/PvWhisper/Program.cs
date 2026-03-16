using System.Text;
using PvWhisper.Audio;
using PvWhisper.Config;
using PvWhisper.Input;
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

        PrintStartupInfo(config, logger);

        using var appCts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            appCts.Cancel();
        };

        try
        {
            if (string.IsNullOrWhiteSpace(config.ModelDir))
            {
                logger.Error("Model directory is required. Set 'modelDir' in AppConfig.json.");
                return 1;
            }

            var modelPath = await new ModelEnsurer(logger).EnsureModelAsync(config.ModelType, config.ModelDir, appCts.Token);

            using var whisperFactory = WhisperFactory.FromPath(modelPath);
            await using var processor = whisperFactory
                .CreateBuilder()
                .WithLanguage(config.Language)
                .WithThreads(config.WhisperThreads)
                .Build();

            var transcriber = new WhisperTranscriber(
                processor,
                new TextTransformer(config.TextTransforms, new RegexReplacer()),
                new WavConverter());

            var deviceResolver = new DeviceResolver(config, logger);
            deviceResolver.LogAvailable();

            var captureManager = new CaptureManager(deviceResolver.Resolve, frameLength: 512, logger);
            var outputDispatcher = new OutputDispatcher(config.Outputs, logger);
            var commandChannelFactory = new CommandChannelFactory(config, logger);

            var app = new PvWhisperApp(config, captureManager, transcriber, outputDispatcher, commandChannelFactory, logger);

            var exitCode = await app.RunAsync(appCts);
            logger.Debug("PvWhisper stopped.");
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

        logger.Debug("PvWhisper stopped.");
        return 0;
    }

    private static void PrintStartupInfo(AppConfig appConfig, ILogger logger)
    {
        logger.Debug("PvWhisper – A cross platform Speech to Text program");

        logger.Debug("Commands:");
        logger.Debug("  v = toggle capture (start / stop + transcribe)");
        logger.Debug("  c = start capture");
        logger.Debug("  z = stop capture and discard audio");
        logger.Debug("  x = stop capture and transcribe");
        logger.Debug("  q = quit");

        if (!appConfig.HasPipeSource) return;

        logger.Debug("In another terminal, you can send commands:");
        logger.Debug("  echo -n 'v' > '$PIPE_PATH'   # toggle capture");
        logger.Debug("  echo -n 'q' > '$PIPE_PATH'   # quit");
    }
}
