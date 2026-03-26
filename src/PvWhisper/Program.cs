using System.Text;
using PvWhisper.Audio;
using PvWhisper.Audio.Implementation;
using PvWhisper.Config;
using PvWhisper.Config.Implementation;
using PvWhisper.Input.Implementation;
using PvWhisper.Logging.Implementation;
using PvWhisper.Output.Implementation;
using PvWhisper.Output.Publishers;
using PvWhisper.Output.Publishers.Implementation;
using PvWhisper.Text.Implementation;
using PvWhisper.Transcription;
using PvWhisper.Transcription.Implementation;
using Whisper.net;

namespace PvWhisper;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var logger = new Logger();
        IConfigService configService = new ConfigService(logger);

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

            IModelEnsurer modelEnsurer = new ModelEnsurer(logger);
            var modelPath = await modelEnsurer.EnsureModelAsync(config.ModelType, config.ModelDir, appCts.Token);

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

            IDeviceResolver deviceResolver = new DeviceResolver(config, logger);
            deviceResolver.LogAvailable();

            var publishers = BuildPublishers(config, logger);
            var captureManager = new CaptureManager(deviceResolver, config.FrameLength, logger);
            var outputDispatcher = new OutputDispatcher(publishers);
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

    private static IReadOnlyCollection<IOutputPublisher> BuildPublishers(
        AppConfig config,
        PvWhisper.Logging.ILogger logger)
    {
        return config.Outputs
            .Distinct()
            .Select<OutputTarget, IOutputPublisher>(t => t switch
            {
                OutputTarget.Console   => new ConsoleOutputPublisher(),
                OutputTarget.Clipboard => new ClipboardOutputPublisher(logger),
                OutputTarget.Ydotool   => new YdotoolOutputPublisher(logger),
                _ => throw new ArgumentOutOfRangeException(nameof(t), t, "Unknown output target")
            })
            .ToList();
    }

    private static void PrintStartupInfo(AppConfig appConfig, PvWhisper.Logging.ILogger logger)
    {
        logger.Debug("PvWhisper – A cross platform Speech to Text program");

        logger.Debug("Commands:");
        logger.Debug("  v = toggle capture (start / stop + transcribe)");
        logger.Debug("  c = start capture");
        logger.Debug("  z = stop capture and discard audio");
        logger.Debug("  x = stop capture and transcribe");
        logger.Debug("  q / Esc = quit");

        if (!appConfig.HasPipeSource) return;

        logger.Debug("In another terminal, you can send commands:");
        logger.Debug("  echo -n 'v' > '$PIPE_PATH'   # toggle capture");
        logger.Debug("  echo -n 'q' > '$PIPE_PATH'   # quit");
    }
}
