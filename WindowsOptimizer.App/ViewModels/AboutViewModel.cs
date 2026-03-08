using System.Diagnostics;
using System.Windows.Input;
using WindowsOptimizer.App.Utilities;
using WindowsOptimizer.Infrastructure;

namespace WindowsOptimizer.App.ViewModels;

public sealed class AboutViewModel : ViewModelBase
{
    public string Title => "About";

    public string AppVersion => AppInfo.Version;

    public string BuildConfiguration => AppInfo.BuildConfiguration;

    public string Framework => AppInfo.FrameworkLabel;

    public string Architecture => AppInfo.ArchitectureLabel;

    public string RepositoryUrl => AppInfo.RepositoryUrl;

    public ICommand OpenUrlCommand { get; }
    public ICommand OpenLogFileCommand { get; }

    private readonly AppPaths _paths;
    private long _logFileSizeBytes;

    public string LogFileSizeFormatted => FormatBytes(_logFileSizeBytes);

    public AboutViewModel()
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

    private void LoadLogFileSize()
    {
        try
        {
            if (System.IO.File.Exists(_paths.TweakLogFilePath))
            {
                var fileInfo = new System.IO.FileInfo(_paths.TweakLogFilePath);
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
            if (System.IO.File.Exists(_paths.TweakLogFilePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _paths.TweakLogFilePath,
                    UseShellExecute = true
                });
            }
        }
        catch { }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F2} KB";
        if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }
}
