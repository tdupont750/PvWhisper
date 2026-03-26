using Pv;
using PvWhisper.Audio;
using PvWhisper.Config;
using PvWhisper.Logging;

namespace PvWhisper.Audio.Implementation;

public sealed class DeviceResolver : IDeviceResolver
{
    private readonly AppConfig _config;
    private readonly ILogger _logger;
    private int? _lastIndex;

    public DeviceResolver(AppConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    public int Resolve()
    {
        var devices = PvRecorder.GetAvailableDevices();
        return ResolveFromDevices(devices);
    }

    public void LogAvailable()
    {
        var devices = PvRecorder.GetAvailableDevices();
        var index = ResolveFromDevices(devices);

        _logger.Debug("Available input devices:");
        for (var i = 0; i < devices.Length; i++)
            _logger.Debug($"  [{i}] {devices[i]}");

        var name = (index >= 0 && index < devices.Length) ? devices[index] : "System default";
        _logger.Debug($"Using device index {index} -> {name}");
    }

    private int ResolveFromDevices(string[] devices)
    {
        var fallback = _config.DeviceIndex ?? -1;

        if (!string.IsNullOrWhiteSpace(_config.DeviceName))
        {
            for (var i = 0; i < devices.Length; i++)
            {
                if (devices[i].Contains(_config.DeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    if (_lastIndex == null)
                        _logger.Info($"Device with name '{_config.DeviceName}' found at index {i}.");
                    else if (i != _lastIndex)
                        _logger.Warn($"Device with name '{_config.DeviceName}' index changed from {_lastIndex} to {i}.");
                    _lastIndex = i;
                    return i;
                }
            }

            if (_lastIndex == null)
                _logger.Warn($"Device with name '{_config.DeviceName}' NOT found, falling back to index {fallback}.");
            else if (fallback != _lastIndex)
                _logger.Warn($"Device with name '{_config.DeviceName}' no longer found; index changed from {_lastIndex} to fallback {fallback}.");
        }

        _lastIndex = fallback;
        return fallback;
    }
}
