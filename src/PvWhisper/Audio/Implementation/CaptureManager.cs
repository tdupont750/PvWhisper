using Pv;
using PvWhisper.Audio;
using PvWhisper.Logging;

namespace PvWhisper.Audio.Implementation;

public sealed class CaptureManager : ICaptureManager
{
    private PvRecorder? _recorder;
    private readonly IDeviceResolver _deviceResolver;
    private readonly int _frameLength;
    private readonly ILogger _logger;
    private readonly Lock _lock = new();

    private List<short>? _buffer;
    private Task? _captureTask;
    private CancellationTokenSource? _captureCts;

    public bool IsCapturing { get; private set; }

    public CaptureManager(IDeviceResolver deviceResolver, int frameLength, ILogger logger)
    {
        _deviceResolver = deviceResolver;
        _frameLength = frameLength;
        _logger = logger;
    }

    public Task StartCaptureAsync()
    {
        lock (_lock)
        {
            if (IsCapturing)
                return Task.CompletedTask;

            _buffer = new List<short>();
            _captureCts = new CancellationTokenSource();
            IsCapturing = true;
            // Lazily create and start recorder only while actively capturing
            _recorder = PvRecorder.Create(frameLength: _frameLength, deviceIndex: _deviceResolver.Resolve());
            _recorder.Start();
            _captureTask = Task.Run(() => CaptureLoop(_captureCts.Token), _captureCts.Token);
        }

        return Task.CompletedTask;
    }

    public async Task StopCaptureAndDiscardAsync()
    {
        var buffer = await ExtractAndStopAsync();
        buffer?.Clear();
    }

    public async Task<AudioBuffer?> StopCaptureAndGetSamplesAsync()
    {
        // If not currently capturing, return any remaining buffered samples
        lock (_lock)
        {
            if (!IsCapturing)
            {
                var remaining = _buffer;
                _buffer = null;
                return remaining == null ? null : new AudioBuffer(remaining.ToArray());
            }
        }

        var buffer = await ExtractAndStopAsync();
        return buffer == null ? null : new AudioBuffer(buffer.ToArray());
    }

    /// <summary>
    /// Atomically marks capture as stopped, cancels the capture task, and disposes the recorder.
    /// Returns the buffer snapshot, or null if not capturing.
    /// </summary>
    private async Task<List<short>?> ExtractAndStopAsync()
    {
        List<short>? buffer;
        Task? captureTask;
        CancellationTokenSource? cts;

        lock (_lock)
        {
            if (!IsCapturing)
                return null;

            IsCapturing = false;
            buffer = _buffer; _buffer = null;
            cts = _captureCts; _captureCts = null;
            captureTask = _captureTask; _captureTask = null;
        }

        if (cts != null)
            await cts.CancelAsync();

        if (captureTask != null)
        {
            try { await captureTask; }
            catch (OperationCanceledException) { }
        }

        StopAndDisposeRecorderSafe();
        return buffer;
    }

    private void CaptureLoop(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var recorder = _recorder;
                if (recorder == null)
                    break;

                var frame = recorder.Read();

                lock (_lock)
                {
                    if (!IsCapturing || _buffer == null)
                        break;

                    _buffer.AddRange(frame);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // normal
        }
        catch (Exception ex)
        {
            _logger.Error($"Capture loop error: {ex.Message}");
        }
        finally
        {
            StopAndDisposeRecorderSafe();
        }
    }

    private void StopAndDisposeRecorderSafe()
    {
        PvRecorder? recorderToDispose;
        lock (_lock)
        {
            recorderToDispose = _recorder;
            _recorder = null;
        }

        if (recorderToDispose == null)
            return;

        try { recorderToDispose.Stop(); } catch { /* ignore */ }
        try { recorderToDispose.Dispose(); } catch { /* ignore */ }
    }
}
