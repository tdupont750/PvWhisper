namespace PvWhisper.Input.Sources;

public interface ICommandSource
{
    IAsyncEnumerable<char> ReadCommandsAsync(CancellationToken token);
}