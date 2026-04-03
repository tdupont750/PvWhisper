namespace PvWhisper.Audio;

/// <summary>
/// Encapsulates auto-timeout behavior for audio capture.
/// Arm to start a countdown and invoke a callback when time elapses while still capturing.
/// Cancel to stop the countdown. Safe for repeated arm/cancel cycles.
/// </summary>
public sealed class CaptureTimeoutManager : IDisposable
{
    private readonly Func<Task> _onTimeoutAsync;
    private readonly CancellationToken _appToken;
    private readonly Lock _lock = new();

    private readonly int _timeoutSeconds;
    private CancellationTokenSource? _cts;

    public CaptureTimeoutManager(
        int timeoutSeconds,
        CancellationToken appToken,
        Func<Task> onTimeoutAsync)
    {
        _timeoutSeconds = timeoutSeconds;
        _appToken = appToken;
        _onTimeoutAsync = onTimeoutAsync;
    }

    /// <inheritdoc/>
    public void RestartTimeout()
    {
        Cancel();

        if (_timeoutSeconds <= 0)
            return;

        var linked = CancellationTokenSource.CreateLinkedTokenSource(_appToken);
        CancellationTokenSource localCts;
        lock (_lock)
        {
            _cts = linked;
            localCts = _cts;
        }

        _ = RunDelayAsync(localCts);
    }

    private async Task RunDelayAsync(CancellationTokenSource localCts)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(_timeoutSeconds), localCts.Token);

            if (!localCts.IsCancellationRequested)
            {
                await _onTimeoutAsync();
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
