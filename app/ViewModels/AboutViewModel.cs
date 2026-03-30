using System.ComponentModel;
using System.Windows.Input;

namespace RegProbe.App.ViewModels;

public sealed class AboutViewModel : ViewModelBase
{
    private readonly AboutWorkspaceCoordinator _coordinator = new();

    public AboutViewModel()
    {
        _coordinator.PropertyChanged += OnCoordinatorPropertyChanged;
    }

    public string Title => _coordinator.Title;

    public string AppVersion => _coordinator.AppVersion;

    public string BuildConfiguration => _coordinator.BuildConfiguration;

    public string Framework => _coordinator.Framework;

    public string Architecture => _coordinator.Architecture;

    public string RepositoryUrl => _coordinator.RepositoryUrl;

    public ICommand OpenUrlCommand => _coordinator.OpenUrlCommand;

    public ICommand OpenLogFileCommand => _coordinator.OpenLogFileCommand;

    public string LogFileSizeFormatted => _coordinator.LogFileSizeFormatted;

    private void OnCoordinatorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e.PropertyName);
    }
}
