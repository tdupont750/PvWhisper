using System.Runtime.CompilerServices;

namespace PvWhisper.Input;

public sealed class ConsoleCommandSource : ICommandSource
{
    public async IAsyncEnumerable<char> ReadCommandsAsync(
        [EnumeratorCancellation] CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                yield return key.KeyChar;
            }
            else
            {
                await Task.Delay(10, token);
            }
        }
    }
}