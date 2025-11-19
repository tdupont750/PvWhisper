using System;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly object _lock = new();

    private int _timeoutSeconds;
    private CancellationTokenSource? _cts;

    public CaptureTimeoutManager(
        int timeoutSeconds,
        CancellationToken appToken,
        Func<bool> isCapturing,
        Func<CancellationToken, Task> onTimeoutAsync)
    {
        _timeoutSeconds = timeoutSeconds;
        _appToken = appToken;
        _isCapturing = isCapturing;
        _onTimeoutAsync = onTimeoutAsync;
    }

    /// <summary>
    /// Updates timeout duration in seconds. Non-positive values disable the feature.
    /// </summary>
    public void UpdateTimeoutSeconds(int timeoutSeconds)
    {
        _timeoutSeconds = timeoutSeconds;
    }

    /// <summary>
    /// Arms the timeout countdown. Cancels any previously armed countdown.
    /// No-op if timeoutSeconds is non-positive.
    /// </summary>
    public Task ArmAsync()
    {
        CancelInternal();

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
                Logger.Warn($"Auto-stopping capture after {_timeoutSeconds} seconds...");
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
        CancelInternal();
    }

    private void CancelInternal()
    {
        CancellationTokenSource? toCancel = null;
        lock (_lock)
        {
            toCancel = _cts;
            _cts = null;
        }

        if (toCancel != null)
        {
            try { toCancel.Cancel(); } catch { /* ignore */ }
            try { toCancel.Dispose(); } catch { /* ignore */ }
        }
    }

    public void Dispose()
    {
        CancelInternal();
    }
}
