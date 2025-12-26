using System.Diagnostics;
using System.Windows.Input;
using WindowsOptimizer.App.Utilities;

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

    public AboutViewModel()
    {
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
    }
}
