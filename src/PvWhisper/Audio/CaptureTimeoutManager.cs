using PvWhisper.Logging;

namespace PvWhisper.Audio;

/// <summary>
/// Encapsulates auto-timeout behavior for audio capture.
/// Arm to start a countdown and invoke a callback when time elapses while still capturing.
/// Cancel to stop the countdown. Safe for repeated arm/cancel cycles.
/// </summary>
public sealed class CaptureTimeoutManager : IDisposable
{
    private readonly Func<bool> _isCapturing;
    private readonly Func<CancellationToken, Task> _onTimeoutAsync;
    private readonly CancellationToken _appToken;
    private readonly Lock _lock = new();

    private readonly int _timeoutSeconds;
    private CancellationTokenSource? _cts;
    private readonly ILogger _logger;

    public CaptureTimeoutManager(
        int timeoutSeconds,
        CancellationToken appToken,
        Func<bool> isCapturing,
        Func<CancellationToken, Task> onTimeoutAsync,
        ILogger? logger = null)
    {
        _timeoutSeconds = timeoutSeconds;
        _appToken = appToken;
        _isCapturing = isCapturing;
        _onTimeoutAsync = onTimeoutAsync;
        _logger = logger;
    }

    /// <summary>
    /// Arms the timeout countdown. Cancels any previously armed countdown.
    /// No-op if timeoutSeconds is non-positive.
    /// </summary>
    public Task RestartTimeoutAsync()
    {
        Cancel();

        if (_timeoutSeconds <= 0)
            return Task.CompletedTask;

        var linked = CancellationTokenSource.CreateLinkedTokenSource(_appToken);
        CancellationTokenSource localCts;
        lock (_lock)
        {
            _cts = linked;
            localCts = _cts;
        }

        return RunDelayAsync(localCts);
    }

    private async Task RunDelayAsync(CancellationTokenSource localCts)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(_timeoutSeconds), localCts.Token);

            if (!localCts.IsCancellationRequested && _isCapturing())
            {
                _logger.Warn($"Auto-stopping capture after {_timeoutSeconds} seconds...");
                await _onTimeoutAsync(_appToken);
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
        finally
        {
            // Clean up only if still current
            lock (_lock)
            {
                if (ReferenceEquals(_cts, localCts))
                {
                    try { _cts?.Cancel(); } catch { /* ignore */ }
                    _cts?.Dispose();
                    _cts = null;
                }
                else
                {
                    try { localCts.Dispose(); } catch { /* ignore */ }
                }
            }
        }
    }

    /// <summary>
    /// Cancels any active timeout countdown.
    /// </summary>
    public void Cancel()
    {
        CancellationTokenSource? toCancel;
        lock (_lock)
        {
            toCancel = _cts;
            _cts = null;
        }

        if (toCancel == null) return;
        try { toCancel.Cancel(); } catch { /* ignore */ }
        try { toCancel.Dispose(); } catch { /* ignore */ }
    }

    public void Dispose()
    {
        Cancel();
    }
}
