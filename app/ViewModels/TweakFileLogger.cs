using System;
using System.IO;

namespace RegProbe.App.ViewModels;

internal static class TweakFileLogger
{
    public static void Log(string message)
    {
        try
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RegProbe",
                "Logs",
                "tweak-vm.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
        }
        catch
        {
            // Ignore logging failures
        }
    }
}
