using PvWhisper.Config;
using PvWhisper.Output.Publishers;

namespace PvWhisper.Output;

/// <summary>
/// Dispatches output text to one or more configured publishers.
/// Publishers are instantiated when the dispatcher is constructed.
/// </summary>
public sealed class OutputDispatcher : IOutputDispatcher
{
    private readonly IReadOnlyCollection<IOutputPublisher> _publishers;
    private readonly PvWhisper.Logging.ILogger _logger;

    public OutputDispatcher(IReadOnlyCollection<OutputTarget> targets, PvWhisper.Logging.ILogger logger)
    {
        if (targets == null || targets.Count == 0)
        {
            throw new ArgumentException("At least one output target must be specified.", nameof(targets));
        }
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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

    private IOutputPublisher CreatePublisher(OutputTarget target) => target switch
    {
        OutputTarget.Console => new ConsoleOutputPublisher(),
        OutputTarget.Clipboard => new ClipboardOutputPublisher(_logger),
        OutputTarget.Ydotool => new YdotoolOutputPublisher(_logger),
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Unknown output target")
    };
}