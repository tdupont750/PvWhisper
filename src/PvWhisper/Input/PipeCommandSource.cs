using System.Runtime.CompilerServices;

namespace PvWhisper.Input;

public sealed class PipeCommandSource : ICommandSource
{
    private readonly string _path;

    public PipeCommandSource(string path)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
    }

    public async IAsyncEnumerable<char> ReadCommandsAsync(
        [EnumeratorCancellation] CancellationToken token)
    {
        using var fs = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var buffer = new byte[1];

        while (!token.IsCancellationRequested)
        {
            int read;
            try
            {
                read = await fs.ReadAsync(buffer, 0, 1, token);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            if (read == 0)
            {
                // Writer gone; wait for a new writer.
                await Task.Delay(50, token);
                continue;
            }

            yield return (char)buffer[0];
        }
    }
}