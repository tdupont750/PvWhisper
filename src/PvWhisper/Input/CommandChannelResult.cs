using System.Threading.Channels;

namespace PvWhisper.Input;

/// <summary>
/// Holds the channel and producer tasks created by <see cref="ICommandChannelFactory"/>.
/// </summary>
public sealed record CommandChannelResult(
    Channel<char> Channel,
    Task ConsoleProducer,
    Task? HttpProducer);
