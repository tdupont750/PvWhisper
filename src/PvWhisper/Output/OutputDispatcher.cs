using PvWhisper.Config;

namespace PvWhisper.Output;

/// <summary>
/// Dispatches output text to one or more configured publishers.
/// Publishers are instantiated when the dispatcher is constructed.
/// </summary>
public sealed class OutputDispatcher
{
    private readonly IReadOnlyCollection<IOutputPublisher> _publishers;

    public OutputDispatcher(IReadOnlyCollection<OutputTarget> targets)
    {
        if (targets == null || targets.Count == 0)
        {
            throw new ArgumentException("At least one output target must be specified.", nameof(targets));
        }

        // Instantiate publishers for the provided targets (deduplicate targets)
        _publishers = targets
            .Distinct()
            .Select(CreatePublisher)
            .ToList();
    }

    public async Task DispatchAsync(string text, CancellationToken token)
    {
        foreach (var publisher in _publishers)
        {
            await publisher.PublishAsync(text, token);
        }
    }

    private static IOutputPublisher CreatePublisher(OutputTarget target) => target switch
    {
        OutputTarget.Console => new ConsoleOutputPublisher(),
        OutputTarget.Clipboard => new ClipboardOutputPublisher(),
        OutputTarget.Ydotool => new YdotoolOutputPublisher(),
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Unknown output target")
    };
}