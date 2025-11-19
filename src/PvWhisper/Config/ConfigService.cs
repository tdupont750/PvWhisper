using System.Text.Json;
using System.Text.Json.Serialization;
using PvWhisper.Logging;

namespace PvWhisper.Config;

public interface IConfigService
{
    AppConfig Load();
}

public sealed class ConfigService : IConfigService
{
    private readonly ILogger _logger;

    public ConfigService(ILogger logger)
    {
        _logger = logger;
    }

    private static string? ExpandPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        // Support tilde expansion for current user's home directory
        if (path == "~")
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return string.IsNullOrWhiteSpace(home) ? path : home;
        }

        if (path.StartsWith("~/", StringComparison.Ordinal) || path.StartsWith("~\\", StringComparison.Ordinal))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(home))
            {
                var remainder = path.Substring(2);
                return Path.Combine(home, remainder);
            }
        }

        return path;
    }

    public AppConfig Load()
    {
        // Locate AppConfig.json (first in CWD, then in BaseDirectory)
        var candidates = new[]
        {
            Path.Combine(Environment.CurrentDirectory, "AppConfig.json"),
            Path.Combine(AppContext.BaseDirectory, "AppConfig.json")
        };

        string? path = null;
        foreach (var c in candidates)
        {
            if (File.Exists(c)) { path = c; break; }
        }

        if (path == null)
            throw new FileNotFoundException("AppConfig.json not found in application base directory or current working directory.");

        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("AppConfig.json is empty.");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());

        var config = JsonSerializer.Deserialize<AppConfig>(json, options)
                     ?? throw new InvalidOperationException("Failed to deserialize AppConfig.json into AppConfig.");

        // Validate paths without applying defaults
        if (!string.IsNullOrWhiteSpace(config.PipePath) && !File.Exists(config.PipePath))
        {
            _logger.Warn($"Pipe file does not exist: {config.PipePath}.");
        }

        // Validate model directory; if specified and doesn't exist, fail.
        if (!string.IsNullOrWhiteSpace(config.ModelDir))
        {
            try
            {
                var expanded = ExpandPath(config.ModelDir);
                var full = Path.GetFullPath(expanded!);
                if (!Directory.Exists(full))
                {
                    throw new ArgumentException($"Model directory does not exist: {full}");
                }
                config.ModelDir = full;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid modelDir '{config.ModelDir}': {ex.Message}");
            }
        }

        // Text transforms are inline in AppConfig (TextTransforms); no external file validation needed.

        return config;
    }
}