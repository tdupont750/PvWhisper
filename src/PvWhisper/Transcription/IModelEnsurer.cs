using PvWhisper.Config;

namespace PvWhisper.Transcription;

public interface IModelEnsurer
{
    Task<string> EnsureModelAsync(ModelKind modelKind, string modelDir, CancellationToken token);
}
