using PvWhisper.Output.Publishers;

namespace PvWhisper.Output.Publishers.Implementation;

public sealed class ConsoleOutputPublisher : IOutputPublisher
{
    public Task PublishAsync(string text, CancellationToken token)
    {
        // Write transcript directly to stdout (not via Logger)
        Console.WriteLine();
        Console.WriteLine(text);
        Console.WriteLine();
        return Task.CompletedTask;
    }
}
