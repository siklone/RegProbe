using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using RegProbe.App.Utilities;
using RegProbe.Infrastructure;

namespace RegProbe.App.ViewModels;

public sealed class AboutWorkspaceCoordinator : ViewModelBase
{
    private readonly AppPaths _paths;
    private long _logFileSizeBytes;

    public AboutWorkspaceCoordinator()
    {
        _paths = AppPaths.FromEnvironment();
        LoadLogFileSize();

        OpenUrlCommand = new RelayCommand(parameter =>
        {
            if (parameter is not string url || string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Ignore link launch failures
            }
        });

        OpenLogFileCommand = new RelayCommand(_ => OpenLogFile());
    }

    public string Title => "About";

    public string AppVersion => AppInfo.Version;

    public string BuildConfiguration => AppInfo.BuildConfiguration;

    public string Framework => AppInfo.FrameworkLabel;

    public string Architecture => AppInfo.ArchitectureLabel;

    public string RepositoryUrl => AppInfo.RepositoryUrl;

    public ICommand OpenUrlCommand { get; }

    public ICommand OpenLogFileCommand { get; }

    public string LogFileSizeFormatted => FormatBytes(_logFileSizeBytes);

    private void LoadLogFileSize()
    {
        try
        {
            if (File.Exists(_paths.TweakLogFilePath))
            {
                var fileInfo = new FileInfo(_paths.TweakLogFilePath);
                _logFileSizeBytes = fileInfo.Length;
                OnPropertyChanged(nameof(LogFileSizeFormatted));
            }
        }
        catch
        {
            _logFileSizeBytes = 0;
        }
    }

    private void OpenLogFile()
    {
        try
        {
            if (File.Exists(_paths.TweakLogFilePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _paths.TweakLogFilePath,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Ignore log launch failures.
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024.0:F2} KB";
        }

        if (bytes < 1024 * 1024 * 1024)
        {
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
        }

        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }
}
