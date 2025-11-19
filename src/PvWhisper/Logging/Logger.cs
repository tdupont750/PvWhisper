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
        WriteInfo("DEBUG", message, isError: false, ConsoleColor.DarkGray);
    }

    public void Info(string message)
    {
        WriteInfo("INFO", message, isError: false, ConsoleColor.DarkBlue);
    }

    public void Warn(string message)
    {
        WriteInfo("WARN", message, isError: true, ConsoleColor.DarkYellow);
    }

    public void Error(string message)
    {
        WriteInfo("ERROR", message, isError: true, ConsoleColor.DarkRed);
    }

    public void Error(Exception ex)
    {
        WriteLine(true, ex.ToString(), ConsoleColor.DarkRed);
    }

    private static void WriteInfo(string level, string message, bool isError, ConsoleColor color)
    {
        // Avoid double-tagging if the message already starts with [LEVEL]
        if (!message.StartsWith("[" + level + "]", StringComparison.OrdinalIgnoreCase))
        {
            message = $"[{level}] {message}";
        }
        WriteLine(isError, message, color);
    }

    private static void WriteLine(bool isError, string message, ConsoleColor color)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        
        if (isError)
            Console.Error.WriteLine(message);
        else
            Console.WriteLine(message);
        
        Console.ForegroundColor = originalColor;
    }
}
