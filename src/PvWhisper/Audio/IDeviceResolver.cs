namespace PvWhisper.Audio;

public interface IDeviceResolver
{
    int Resolve();
    void LogAvailable();
}
