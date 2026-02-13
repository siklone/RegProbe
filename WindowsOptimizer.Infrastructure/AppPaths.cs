using System;
using System.IO;

namespace WindowsOptimizer.Infrastructure;

public sealed class AppPaths
{
    public const string AppFolderName = "WindowsOptimizerSuite";

    public AppPaths(string baseAppDataPath)
    {
        if (string.IsNullOrWhiteSpace(baseAppDataPath))
        {
            throw new ArgumentException("Base app data path is required.", nameof(baseAppDataPath));
        }

        BaseAppDataPath = baseAppDataPath;
    }

    public string BaseAppDataPath { get; }

    public string AppDataRoot => Path.Combine(BaseAppDataPath, AppFolderName);

    public string SettingsFilePath => Path.Combine(AppDataRoot, "settings.json");

    public string LogDirectory => Path.Combine(AppDataRoot, "logs");

    public string LogFilePath => Path.Combine(LogDirectory, "app.log");

    public string TweakLogFilePath => Path.Combine(LogDirectory, "tweak-log.csv");

    public string ProfilesDirectory => Path.Combine(AppDataRoot, "profiles");

    public string PresetsDirectory => Path.Combine(AppDataRoot, "presets");

    public string HardwareDatabasePath => Path.Combine(AppDataRoot, "hardware.db");

    public string NohutoScanStateFilePath => Path.Combine(AppDataRoot, "nohuto-scan-state.json");

    public string NohutoAnalysisReportPath => Path.Combine(LogDirectory, "nohuto-analysis.json");

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(AppDataRoot);
        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(ProfilesDirectory);
        Directory.CreateDirectory(PresetsDirectory);
    }

    public static AppPaths FromEnvironment()
    {
        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return new AppPaths(basePath);
    }
}
