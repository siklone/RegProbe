using System;
using System.ComponentModel;
using System.Windows.Input;
using RegProbe.App.Services;
using RegProbe.App.Utilities;

namespace RegProbe.App.ViewModels;

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly MainCompositionCoordinator _compositionCoordinator;
    private readonly MainRecoveryCoordinator _recoveryCoordinator;
    private readonly MainShellCoordinator _shellCoordinator;

    public MainViewModel()
    {
        LogToFile("========== APPLICATION STARTED ==========");
        _compositionCoordinator = new MainCompositionCoordinator(LogToFile);
        _recoveryCoordinator = _compositionCoordinator.RecoveryCoordinator;
        _recoveryCoordinator.PropertyChanged += OnRecoveryCoordinatorPropertyChanged;

        _shellCoordinator = _compositionCoordinator.ShellCoordinator;
        _shellCoordinator.PropertyChanged += OnShellCoordinatorPropertyChanged;
        _compositionCoordinator.Initialize();
    }

    public IBusyService BusyService => _compositionCoordinator.BusyService;

    public string AppVersionLabel => AppInfo.VersionLabel;

    public string AppCopyrightLabel => AppInfo.CopyrightLabel;

    public ICommand RecoverPendingRollbacksCommand => _recoveryCoordinator.RecoverPendingRollbacksCommand;

    public ICommand DismissPendingRollbacksCommand => _recoveryCoordinator.DismissPendingRollbacksCommand;

    public RelayCommand ShowRepairsCommand => _shellCoordinator.ShowRepairsCommand;

    public RelayCommand ShowConfigurationCommand => _shellCoordinator.ShowConfigurationCommand;

    public RelayCommand ShowAboutCommand => _shellCoordinator.ShowAboutCommand;

    public RelayCommand FocusSearchCommand => _shellCoordinator.FocusSearchCommand;

    public RelayCommand ClearFiltersCommand => _shellCoordinator.ClearFiltersCommand;

    public ViewModelBase? CurrentViewModel => _shellCoordinator.CurrentViewModel;

    public bool IsConfigurationViewActive => _shellCoordinator.IsConfigurationViewActive;

    public bool IsRepairsViewActive => _shellCoordinator.IsRepairsViewActive;

    public bool IsAboutViewActive => _shellCoordinator.IsAboutViewActive;

    public bool HasPendingRollbacks => _recoveryCoordinator.HasPendingRollbacks;

    public int PendingRollbackCount => _recoveryCoordinator.PendingRollbackCount;

    public string PendingRollbackMessage => _recoveryCoordinator.PendingRollbackMessage;

    public bool IsRecovering => _recoveryCoordinator.IsRecovering;

    public void Dispose()
    {
        _recoveryCoordinator.PropertyChanged -= OnRecoveryCoordinatorPropertyChanged;
        _shellCoordinator.PropertyChanged -= OnShellCoordinatorPropertyChanged;
        _compositionCoordinator.Dispose();
    }

    private void OnRecoveryCoordinatorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName))
        {
            return;
        }

        OnPropertyChanged(e.PropertyName);
    }

    private void OnShellCoordinatorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName))
        {
            return;
        }

        OnPropertyChanged(e.PropertyName);
    }

    private static void LogToFile(string message)
    {
        try
        {
            var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RegProbe_Diagnostics.log");
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            System.IO.File.AppendAllText(logPath, $"[{timestamp}] {message}\n");
        }
        catch
        {
        }
    }
}
