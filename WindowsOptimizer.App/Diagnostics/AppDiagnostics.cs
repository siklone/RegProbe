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
                // Determine a writable log path. Prefer the system temp folder but
                // fall back to the application base directory if Temp is not set
                // or not writable (some environments launch processes without
                // TEMP/TMP set). Swallowing exceptions above hid this case.
                string? temp = null;
                try
                {
                    temp = Path.GetTempPath();
                }
                catch
                {
                    temp = null;
                }

                string logPath;
                if (!string.IsNullOrWhiteSpace(temp))
                {
                    try
                    {
                        // Ensure the directory exists and is writable
                        var testDir = Path.GetFullPath(temp);
                        if (Directory.Exists(testDir))
                        {
                            // Attempt a quick write to validate permissions
                            var testFile = Path.Combine(testDir, "__windowsoptimizer_tmp_write_test.txt");
                            try
                            {
                                File.AppendAllText(testFile, "test");
                                File.Delete(testFile);
                                logPath = Path.Combine(testDir, "WindowsOptimizer_Diagnostics.log");
                            }
                            catch
                            {
                                // Not writable, fall back
                                logPath = Path.Combine(AppContext.BaseDirectory, "WindowsOptimizer_Diagnostics.log");
                            }
                        }
                        else
                        {
                            logPath = Path.Combine(AppContext.BaseDirectory, "WindowsOptimizer_Diagnostics.log");
                        }
                    }
                    catch
                    {
                        logPath = Path.Combine(AppContext.BaseDirectory, "WindowsOptimizer_Diagnostics.log");
                    }
                }
                else
                {
                    logPath = Path.Combine(AppContext.BaseDirectory, "WindowsOptimizer_Diagnostics.log");
                }

                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                var line = $"[{timestamp}] {message}\n";

                try
                {
                    // Use a FileStream with shared read access so other tools can
                    // read the log while the app is running. This sometimes fails
                    // due to antivirus/locking; fall through to other fallbacks.
                    var bytes = System.Text.Encoding.UTF8.GetBytes(line);
                    using (var fs = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.Read))
                    {
                        fs.Write(bytes, 0, bytes.Length);
                    }
                }
                catch (Exception ex1)
                {
                    try
                    {
                        // Last-resort simple append
                        File.AppendAllText(logPath, line);
                    }
                    catch (Exception ex2)
                    {
                        // If even that fails, try to write a tiny marker file in
                        // the application folder so we can see that logging failed
                        // and capture the exception message for diagnosis.
                        try
                        {
                            var marker = Path.Combine(AppContext.BaseDirectory, $"WindowsOptimizer_Debug_write_error_{DateTime.Now:yyyyMMddHHmmssfff}.txt");
                            File.AppendAllText(marker, ex1 + "\n" + ex2 + "\n" + line);
                        }
                        catch
                        {
                            // Give up silently - diagnostics must not crash the app
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore logging errors
        }
    }
}

