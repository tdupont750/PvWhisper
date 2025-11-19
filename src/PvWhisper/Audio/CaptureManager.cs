using Pv;
using PvWhisper.Logging;

namespace PvWhisper.Audio;

public sealed class CaptureManager
{
    private PvRecorder? _recorder;
    private readonly int _deviceIndex;
    private readonly int _frameLength;
    private readonly Lock _lock = new();

    private List<short>? _buffer;
    private Task? _captureTask;
    private CancellationTokenSource? _captureCts;

    public bool IsCapturing { get; private set; }

    public CaptureManager(int deviceIndex, int frameLength)
    {
        _deviceIndex = deviceIndex;
        _frameLength = frameLength;
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
            _recorder = PvRecorder.Create(frameLength: _frameLength, deviceIndex: _deviceIndex);
            _recorder.Start();
            _captureTask = Task.Run(() => CaptureLoop(_captureCts.Token), _captureCts.Token);
        }

        return Task.CompletedTask;
    }

    public async Task StopCaptureAndDiscardAsync()
    {
        List<short>? discardBuffer;
        Task? captureTaskToWait;
        CancellationTokenSource? cts;

        lock (_lock)
        {
            if (!IsCapturing)
                return;

            IsCapturing = false;
            discardBuffer = _buffer;
            _buffer = null;

            cts = _captureCts;
            _captureCts = null;

            captureTaskToWait = _captureTask;
            _captureTask = null;
        }

        if (cts != null)
            cts.Cancel();

        if (captureTaskToWait != null)
        {
            try
            {
                await captureTaskToWait;
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }

        StopAndDisposeRecorderSafe();

        discardBuffer?.Clear();
    }

    public async Task<short[]?> StopCaptureAndGetSamplesAsync()
    {
        List<short>? bufferSnapshot;
        Task? captureTaskToWait;
        CancellationTokenSource? cts;

        lock (_lock)
        {
            if (!IsCapturing)
            {
                bufferSnapshot = _buffer;
                _buffer = null;
                return bufferSnapshot?.ToArray();
            }

            IsCapturing = false;
            bufferSnapshot = _buffer;
            _buffer = null;

            cts = _captureCts;
            _captureCts = null;

            captureTaskToWait = _captureTask;
            _captureTask = null;
        }

        if (cts != null)
            cts.Cancel();

        if (captureTaskToWait != null)
        {
            try
            {
                await captureTaskToWait;
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }

        StopAndDisposeRecorderSafe();

        return bufferSnapshot?.ToArray();
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

                // TODO check for overflow?
                /*if (recorder.NumOverflowedFrames > 0)
                {
                    Logger.Warn($"Recorder overflowed frames: {recorder.NumOverflowedFrames}");
                }*/
            }
        }
        catch (OperationCanceledException)
        {
            // normal
        }
        catch (Exception ex)
        {
            Logger.Error($"Capture loop error: {ex.Message}");
        }
        finally
        {
            StopAndDisposeRecorderSafe();
        }
    }

    private void StopAndDisposeRecorderSafe()
    {
        PvRecorder? recorderToDispose = null;
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