using System;
using System.IO;

namespace WindowsOptimizer.App.Utilities;

/// <summary>
/// Manages application paths for both installed and portable modes.
/// Portable mode is detected by the presence of a "portable.txt" file in the app directory.
/// </summary>
public static class ApplicationPaths
{
    private static readonly Lazy<bool> _isPortableMode = new(DetectPortableMode);
    private static readonly Lazy<string> _dataDirectory = new(GetDataDirectory);
    private static readonly Lazy<string> _logDirectory = new(GetLogDirectory);
    private static readonly Lazy<string> _configDirectory = new(GetConfigDirectory);

    /// <summary>
    /// Gets whether the application is running in portable mode.
    /// </summary>
    public static bool IsPortableMode => _isPortableMode.Value;

    /// <summary>
    /// Gets the application executable directory.
    /// </summary>
    public static string AppDirectory => AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>
    /// Gets the data directory for storing application data.
    /// </summary>
    public static string DataDirectory => _dataDirectory.Value;

    /// <summary>
    /// Gets the log directory for storing log files.
    /// </summary>
    public static string LogDirectory => _logDirectory.Value;

    /// <summary>
    /// Gets the config directory for storing configuration files.
    /// </summary>
    public static string ConfigDirectory => _configDirectory.Value;

    /// <summary>
    /// Gets the path to the settings file.
    /// </summary>
    public static string SettingsFilePath => Path.Combine(ConfigDirectory, "settings.json");

    /// <summary>
    /// Gets the path to the audit log directory.
    /// </summary>
    public static string AuditLogDirectory => Path.Combine(LogDirectory, "AuditLogs");

    /// <summary>
    /// Gets the path to the crash log directory.
    /// </summary>
    public static string CrashLogDirectory => Path.Combine(LogDirectory, "CrashLogs");

    /// <summary>
    /// Gets the path to the backup directory.
    /// </summary>
    public static string BackupDirectory => Path.Combine(DataDirectory, "Backups");

    private static bool DetectPortableMode()
    {
        var portableMarker = Path.Combine(AppDirectory, "portable.txt");
        return File.Exists(portableMarker);
    }

    private static string GetDataDirectory()
    {
        if (IsPortableMode)
        {
            var dir = Path.Combine(AppDirectory, "Data");
            EnsureDirectoryExists(dir);
            return dir;
        }

        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsOptimizer");
        EnsureDirectoryExists(appDataDir);
        return appDataDir;
    }

    private static string GetLogDirectory()
    {
        var dir = Path.Combine(DataDirectory, "Logs");
        EnsureDirectoryExists(dir);
        return dir;
    }

    private static string GetConfigDirectory()
    {
        var dir = Path.Combine(DataDirectory, "Config");
        EnsureDirectoryExists(dir);
        return dir;
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    /// <summary>
    /// Creates a portable.txt marker file to enable portable mode.
    /// </summary>
    public static void EnablePortableMode()
    {
        var portableMarker = Path.Combine(AppDirectory, "portable.txt");
        File.WriteAllText(portableMarker, $"Portable mode enabled on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    }

    /// <summary>
    /// Gets a summary of the current paths configuration.
    /// </summary>
    public static string GetPathsSummary()
    {
        return $"""
            Mode: {(IsPortableMode ? "Portable" : "Installed")}
            App Directory: {AppDirectory}
            Data Directory: {DataDirectory}
            Log Directory: {LogDirectory}
            Config Directory: {ConfigDirectory}
            Settings File: {SettingsFilePath}
            """;
    }
}
