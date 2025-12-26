using System;
using System.IO;
using System.Text;

namespace WindowsOptimizer.App.Diagnostics;

public static class AppDiagnostics
{
    private static readonly object Gate = new();

    public static void Log(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        WriteToFile(message, null);
    }

    public static void LogException(string context, Exception exception)
    {
        if (exception is null)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{context}: {exception.GetType().Name}: {exception.Message}");
        sb.AppendLine(exception.StackTrace);
        if (exception.InnerException is not null)
        {
            sb.AppendLine($"Inner: {exception.InnerException.GetType().Name}: {exception.InnerException.Message}");
            sb.AppendLine(exception.InnerException.StackTrace);
        }

        WriteToFile(sb.ToString(), exception);
    }

    private static void WriteToFile(string message, Exception? exception)
    {
        try
        {
            lock (Gate)
            {
                var logPath = Path.Combine(Path.GetTempPath(), "WindowsOptimizer_Debug.log");
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                File.AppendAllText(logPath, $"[{timestamp}] {message}\n");
            }
        }
        catch
        {
            // Ignore logging errors
        }
    }
}

