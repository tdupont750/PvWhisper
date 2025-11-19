namespace PvWhisper.Output;

public interface IOutputDispatcher
{
    Task DispatchAsync(string text, CancellationToken token);
}