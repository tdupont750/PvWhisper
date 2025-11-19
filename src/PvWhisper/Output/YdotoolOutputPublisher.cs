using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using PvWhisper.Logging;

namespace PvWhisper.Output;

public sealed class YdotoolOutputPublisher : IOutputPublisher
{
    public async Task PublishAsync(string text, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Logger.Warn("Empty transcription; skipping ydotool write.");
            return;
        }

        // /usr/include/linux/input-event-codes.h
        const string backspaceToken = "{ydotool:KEY_BACKSPACE}";
        if (text.StartsWith(backspaceToken))
        {
            var remainder = text.Substring(backspaceToken.Length);
            await RunYdotoolAsync("ydotool key 14:1 14:0", string.Empty, token);
            text = remainder;
        }

        await RunYdotoolAsync("ydotool type --key-delay=3 --key-hold=2 --file=-", text, token);
    }

    private static async Task RunYdotoolAsync(string command, string stdin, CancellationToken token)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add(command);

            using var proc = Process.Start(psi);
            if (proc == null)
            {
                Logger.Warn("Failed to start ydotool process.");
                return;
            }

            if (!string.IsNullOrEmpty(stdin))
            {
                await proc.StandardInput.WriteAsync(stdin.AsMemory(), token);
                proc.StandardInput.Close();
            }

#if NET6_0_OR_GREATER
            await proc.WaitForExitAsync(token);
#else
            proc.WaitForExit();
#endif

            if (proc.ExitCode != 0)
            {
                var err = await proc.StandardError.ReadToEndAsync();
                Logger.Warn($"ydotool exited with code {proc.ExitCode}: {err}");
            }
            else
            {
                Logger.Info("Transcription sent via ydotool.");
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to send text via ydotool: {ex.Message}");
        }
    }
}
