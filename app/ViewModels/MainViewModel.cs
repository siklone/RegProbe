using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using RegProbe.App.Services;
using RegProbe.App.Services.TweakProviders;
using RegProbe.App.Utilities;
using RegProbe.Engine.Services;
using RegProbe.Infrastructure;

namespace RegProbe.App.ViewModels;

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IBusyService _busyService = new BusyService();
    private readonly TweaksViewModel _workspaceViewModel;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly MainRecoveryCoordinator _recoveryCoordinator;
    private readonly MainShellCoordinator _shellCoordinator;

    public MainViewModel()
    {
        LogToFile("========== APPLICATION STARTED ==========");

        var paths = AppPaths.FromEnvironment();
        var rollbackStore = new RollbackStateStore(paths);

        var providers = new ITweakProvider[]
        {
            new SystemTweakProvider(),
            new SystemRegistryTweakProvider(),
            new PrivacyTweakProvider(),
            new SecurityTweakProvider(),
            new NetworkTweakProvider(),
            new PowerTweakProvider(),
            new PeripheralTweakProvider(),
            new VisibilityTweakProvider(),
            new PerformanceTweakProvider(),
            new AudioTweakProvider(),
            new MiscTweakProvider()
        };

        _workspaceViewModel = new TweaksViewModel(providers, _busyService);
        _settingsViewModel = new SettingsViewModel();
        var configurationViewModel = new ConfigurationShellViewModel(_workspaceViewModel);
        var repairsViewModel = new RepairsShellViewModel(_workspaceViewModel);
        var aboutViewModel = new AboutViewModel();

        _recoveryCoordinator = new MainRecoveryCoordinator(rollbackStore, _workspaceViewModel, LogToFile);
        _recoveryCoordinator.PropertyChanged += OnRecoveryCoordinatorPropertyChanged;

        _shellCoordinator = new MainShellCoordinator(
            configurationViewModel,
            repairsViewModel,
            _settingsViewModel,
            aboutViewModel,
            LogToFile);
        _shellCoordinator.PropertyChanged += OnShellCoordinatorPropertyChanged;
        _shellCoordinator.Initialize();

        _ = Task.Run(async () =>
        {
            await _recoveryCoordinator.InitializeAsync();
        });
    }

    public IBusyService BusyService => _busyService;

    public string AppVersionLabel => AppInfo.VersionLabel;

    public string AppCopyrightLabel => AppInfo.CopyrightLabel;

    public ICommand RecoverPendingRollbacksCommand => _recoveryCoordinator.RecoverPendingRollbacksCommand;

    public ICommand DismissPendingRollbacksCommand => _recoveryCoordinator.DismissPendingRollbacksCommand;

    public RelayCommand ShowRepairsCommand => _shellCoordinator.ShowRepairsCommand;

    public RelayCommand ShowConfigurationCommand => _shellCoordinator.ShowConfigurationCommand;

    public RelayCommand ShowSettingsCommand => _shellCoordinator.ShowSettingsCommand;

    public RelayCommand ShowAboutCommand => _shellCoordinator.ShowAboutCommand;

    public RelayCommand FocusSearchCommand => _shellCoordinator.FocusSearchCommand;

    public RelayCommand ClearFiltersCommand => _shellCoordinator.ClearFiltersCommand;

    public ViewModelBase? CurrentViewModel => _shellCoordinator.CurrentViewModel;

    public bool IsConfigurationViewActive => _shellCoordinator.IsConfigurationViewActive;

    public bool IsRepairsViewActive => _shellCoordinator.IsRepairsViewActive;

    public bool IsSettingsViewActive => _shellCoordinator.IsSettingsViewActive;

    public bool IsAboutViewActive => _shellCoordinator.IsAboutViewActive;

    public bool HasPendingRollbacks => _recoveryCoordinator.HasPendingRollbacks;

    public int PendingRollbackCount => _recoveryCoordinator.PendingRollbackCount;

    public string PendingRollbackMessage => _recoveryCoordinator.PendingRollbackMessage;

    public bool IsRecovering => _recoveryCoordinator.IsRecovering;

    public void Dispose()
    {
        _recoveryCoordinator.PropertyChanged -= OnRecoveryCoordinatorPropertyChanged;
        _shellCoordinator.PropertyChanged -= OnShellCoordinatorPropertyChanged;

        if (_workspaceViewModel is IDisposable workspaceDisposable)
        {
            workspaceDisposable.Dispose();
        }

        if (_settingsViewModel is IDisposable settingsDisposable)
        {
            settingsDisposable.Dispose();
        }
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
