namespace PvWhisper.Logging;

public interface ILogger
{
    bool DebugEnabled { get; set; }
    void Debug(string message);
    void Info(string message);
    void Warn(string message);
    void Error(string message);
    void Error(Exception ex);
    void ToggleAlert(string alertText, bool isShow);
}