using System.Threading;
using System.Threading.Tasks;

namespace PvWhisper.Output;

public interface IOutputPublisher
{
    Task PublishAsync(string text, CancellationToken token);
}
