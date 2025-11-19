using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PvWhisper.Input;

public sealed class PipeCommandSource : ICommandSource
{
    private readonly string _path;

    public PipeCommandSource(string path)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
    }

    public IAsyncEnumerable<char> ReadCommandsAsync(CancellationToken token)
    {
        // On Unix, prefer a non-blocking open/read of the FIFO so the loop can honor cancellation
        // even when no writer is connected or no data is available. On non-Unix, fall back to
        // standard FileStream ReadAsync behavior.
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            return UnixReadCommandsAsync(token);

        return WinReadCommandsAsync(token);
    }

    private async IAsyncEnumerable<char> UnixReadCommandsAsync(
        [EnumeratorCancellation] CancellationToken token)
    {
        var fd = Native.open(_path, Native.O_RDONLY | Native.O_NONBLOCK);
        if (fd < 0)
            throw new IOException($"Failed to open pipe '{_path}' (errno={Marshal.GetLastSystemError()}).");

        try
        {
            var buffer = new byte[1];
            while (!token.IsCancellationRequested)
            {
                int n = Native.read(fd, buffer, 1);
                if (n == 1)
                {
                    yield return (char)buffer[0];
                    continue;
                }

                if (n == 0)
                {
                    // No writer (EOF) – brief pause then retry to allow a new writer to connect.
                    await Task.Delay(50, token);
                    continue;
                }

                // n < 0 -> error; check EAGAIN/EWOULDBLOCK and continue, else throw
                int errno = Marshal.GetLastSystemError();
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (errno == Native.EAGAIN || errno == Native.EWOULDBLOCK)
                {
                    await Task.Delay(20, token);
                    continue;
                }

                throw new IOException($"Error reading pipe '{_path}' (errno={errno}).");
            }
        }
        finally
        {
            // Close fd
#pragma warning disable CA1806
            Native.close(fd);
#pragma warning restore CA1806
        }
    }

    private async IAsyncEnumerable<char> WinReadCommandsAsync(
        [EnumeratorCancellation] CancellationToken token)
    {
        await using var fs = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var buffer = new byte[1];
        while (!token.IsCancellationRequested)
        {
            int read;
            try
            {
                read = await fs.ReadAsync(buffer.AsMemory(0, 1), token);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            if (read == 0)
            {
                await Task.Delay(50, token);
                continue;
            }
            yield return (char)buffer[0];
        }
    }

    // ReSharper disable InconsistentNaming
    private static class Native
    {
        public const int O_RDONLY = 0x0000;
        public const int O_NONBLOCK = 0x0800; // Linux/macOS compatible value
        public const int EAGAIN = 11;
        public const int EWOULDBLOCK = 11; // Usually same as EAGAIN

        [DllImport("libc", SetLastError = true, EntryPoint = "open")]
        public static extern int open(string pathname, int flags);

        [DllImport("libc", SetLastError = true, EntryPoint = "read")]
        private static extern IntPtr read_intptr(int fd, IntPtr buf, UIntPtr count);

        public static int read(int fd, byte[] buffer, int count)
        {
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                var ret = read_intptr(fd, handle.AddrOfPinnedObject(), (uint)count);
                var n = (int)ret;
                if (n >= 0) return n;
                return -1;
            }
            finally
            {
                handle.Free();
            }
        }

        [DllImport("libc", SetLastError = true, EntryPoint = "close")]
        public static extern int close(int fd);
    }
    // ReSharper restore InconsistentNaming
}