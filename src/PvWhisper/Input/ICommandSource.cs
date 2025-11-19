namespace PvWhisper.Input;

public interface ICommandSource
{
    IAsyncEnumerable<char> ReadCommandsAsync(CancellationToken token);
}