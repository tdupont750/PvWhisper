namespace PvWhisper.Output.Publishers;

public interface IOutputPublisher
{
    Task PublishAsync(string text, CancellationToken token);
}
