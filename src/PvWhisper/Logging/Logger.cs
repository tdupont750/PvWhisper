namespace PvWhisper.Logging;

/// <summary>
/// Console logger implementation.
/// </summary>
public sealed class Logger : ILogger
{
    public bool DebugEnabled { get; set; } = true;

    public void Debug(string message)
    {
        if (!DebugEnabled) return;
        WriteInfo("DEBUG", message, isError: false);
    }

    public void Info(string message)
    {
        WriteInfo("INFO", message, isError: false);
    }

    public void Warn(string message)
    {
        WriteInfo("WARN", message, isError: true);
    }

    public void Error(string message)
    {
        WriteInfo("ERROR", message, isError: true);
    }

    public void Error(Exception ex)
    {
        WriteLine(true, ex.ToString());
    }

    private static void WriteInfo(string level, string message, bool isError)
    {
        // Avoid double-tagging if the message already starts with [LEVEL]
        if (!message.StartsWith("[" + level + "]", StringComparison.OrdinalIgnoreCase))
        {
            message = $"[{level}] {message}";
        }
        WriteLine(isError, message);
    }

    private static void WriteLine(bool isError, string message)
    {
        if (isError)
            Console.Error.WriteLine(message);
        else
            Console.WriteLine(message);
    }
}
