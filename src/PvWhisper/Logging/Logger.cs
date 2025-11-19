using System;

namespace PvWhisper.Logging;

/// <summary>
/// Simple static logger that writes to console with levels.
/// </summary>
public static class Logger
{
    public static bool DebugEnabled { get; set; } = true;

    public static void Debug(string message)
    {
        if (!DebugEnabled) return;
        WriteInfo("DEBUG", message, isError: false);
    }

    public static void Info(string message)
    {
        WriteInfo("INFO", message, isError: false);
    }

    public static void Warn(string message)
    {
        WriteInfo("WARN", message, isError: true);
    }

    public static void Error(string message)
    {
        WriteInfo("ERROR", message, isError: true);
    }

    public static void Error(Exception ex)
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
