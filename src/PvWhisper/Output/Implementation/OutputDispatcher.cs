using PvWhisper.Output;
using PvWhisper.Output.Publishers;

namespace PvWhisper.Output.Implementation;

/// <summary>
/// Dispatches output text to one or more configured publishers.
/// </summary>
public sealed class OutputDispatcher : IOutputDispatcher
{
    private readonly IReadOnlyCollection<IOutputPublisher> _publishers;

    public OutputDispatcher(IReadOnlyCollection<IOutputPublisher> publishers)
    {
        if (publishers == null || publishers.Count == 0)
            throw new ArgumentException("At least one publisher must be provided.", nameof(publishers));
        _publishers = publishers;
    }

    public async Task DispatchAsync(string text, CancellationToken token)
    {
        foreach (var publisher in _publishers)
        {
            await publisher.PublishAsync(text, token);
        }
    }
}
