using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.ComponentModel;
using RegProbe.App.Services;
using RegProbe.App.Services.TweakProviders;
using RegProbe.App.Utilities;
using RegProbe.Engine.Services;
using RegProbe.Infrastructure;

namespace RegProbe.App.ViewModels;

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private ViewModelBase? _currentViewModel;
    private string _searchText = string.Empty;
    private readonly RelayCommand _clearSearchCommand;
    private readonly IBusyService _busyService = new BusyService();
    private readonly TweaksViewModel _workspaceViewModel;
    private readonly ConfigurationShellViewModel _configurationViewModel;
    private readonly RepairsShellViewModel _repairsViewModel;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly AboutViewModel _aboutViewModel;
    private readonly MainRecoveryCoordinator _recoveryCoordinator;

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
        _workspaceViewModel.PropertyChanged += OnWorkspaceViewModelPropertyChanged;
        _configurationViewModel = new ConfigurationShellViewModel(_workspaceViewModel);
        _repairsViewModel = new RepairsShellViewModel(_workspaceViewModel);
        _settingsViewModel = new SettingsViewModel();
        _aboutViewModel = new AboutViewModel();
        _recoveryCoordinator = new MainRecoveryCoordinator(rollbackStore, _workspaceViewModel, LogToFile);
        _recoveryCoordinator.PropertyChanged += OnRecoveryCoordinatorPropertyChanged;

        _clearSearchCommand = new RelayCommand(_ => SearchText = string.Empty, _ => !string.IsNullOrEmpty(SearchText));

        ShowConfigurationCommand = new RelayCommand(_ => ShowConfiguration());
        ShowRepairsCommand = new RelayCommand(_ => ShowRepairs());
        ShowSettingsCommand = new RelayCommand(_ => ShowSettings());
        ShowAboutCommand = new RelayCommand(_ => ShowAbout());
        FocusSearchCommand = new RelayCommand(_ => OnFocusSearchRequested());
        ClearFiltersCommand = new RelayCommand(_ => OnClearFilters());

        ShowConfiguration();

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

    public RelayCommand ShowRepairsCommand { get; }

    public RelayCommand ShowConfigurationCommand { get; }

    public RelayCommand ShowSettingsCommand { get; }

    public RelayCommand ShowAboutCommand { get; }

    public RelayCommand FocusSearchCommand { get; }

    public RelayCommand ClearFiltersCommand { get; }

    public event Action? FocusSearchRequested;

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            try
            {
                LogToFile($"CurrentViewModel setter: Setting to {value?.GetType().Name}");
                if (SetProperty(ref _currentViewModel, value))
                {
                    OnPropertyChanged(nameof(IsConfigurationViewActive));
                    OnPropertyChanged(nameof(IsRepairsViewActive));
                    OnPropertyChanged(nameof(IsSettingsViewActive));
                    OnPropertyChanged(nameof(IsAboutViewActive));
                }

                LogToFile("CurrentViewModel setter: Set complete");
            }
            catch (Exception ex)
            {
                LogToFile($"CRASH in CurrentViewModel setter: {ex.Message}");
                LogToFile($"Stack: {ex.StackTrace}");
                throw;
            }
        }
    }

    public bool IsConfigurationViewActive => ReferenceEquals(CurrentViewModel, _configurationViewModel);

    public bool IsRepairsViewActive => ReferenceEquals(CurrentViewModel, _repairsViewModel);

    public bool IsSettingsViewActive => ReferenceEquals(CurrentViewModel, _settingsViewModel);

    public bool IsAboutViewActive => ReferenceEquals(CurrentViewModel, _aboutViewModel);

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                SyncSearchText();
                _clearSearchCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public RelayCommand ClearSearchCommand => _clearSearchCommand;

    public bool HasPendingRollbacks
    {
        get => _recoveryCoordinator.HasPendingRollbacks;
    }

    public int PendingRollbackCount
    {
        get => _recoveryCoordinator.PendingRollbackCount;
    }

    public string PendingRollbackMessage
    {
        get => _recoveryCoordinator.PendingRollbackMessage;
    }

    public bool IsRecovering
    {
        get => _recoveryCoordinator.IsRecovering;
    }

    public void Dispose()
    {
        _workspaceViewModel.PropertyChanged -= OnWorkspaceViewModelPropertyChanged;
        _recoveryCoordinator.PropertyChanged -= OnRecoveryCoordinatorPropertyChanged;

        if (_workspaceViewModel is IDisposable workspaceDisposable)
        {
            workspaceDisposable.Dispose();
        }

        if (_settingsViewModel is IDisposable settingsDisposable)
        {
            settingsDisposable.Dispose();
        }
    }

    private void ShowConfiguration()
    {
        _configurationViewModel.ShowConfigurationWorkspace();
        CurrentViewModel = _configurationViewModel;
        SyncSearchText();
        RaiseShellViewStateChanged();
    }

    private void ShowRepairs()
    {
        _workspaceViewModel.SelectedWorkspace = ConfigurationWorkspaceKind.Maintenance;
        CurrentViewModel = _repairsViewModel;
        SyncSearchText();
        RaiseShellViewStateChanged();
    }

    private void ShowSettings()
    {
        CurrentViewModel = _settingsViewModel;
        RaiseShellViewStateChanged();
    }

    private void ShowAbout()
    {
        CurrentViewModel = _aboutViewModel;
        RaiseShellViewStateChanged();
    }

    private void OnFocusSearchRequested()
    {
        ShowConfiguration();
        FocusSearchRequested?.Invoke();
    }

    private void OnClearFilters()
    {
        _configurationViewModel.ClearFilters();
        CurrentViewModel = _configurationViewModel;
        RaiseShellViewStateChanged();
        SearchText = string.Empty;
    }

    private void OnWorkspaceViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!ReferenceEquals(CurrentViewModel, _configurationViewModel) &&
            !ReferenceEquals(CurrentViewModel, _repairsViewModel))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(e.PropertyName) ||
            e.PropertyName == nameof(TweaksViewModel.SelectedWorkspace))
        {
            RaiseShellViewStateChanged();
        }
    }

    private void RaiseShellViewStateChanged()
    {
        OnPropertyChanged(nameof(IsConfigurationViewActive));
        OnPropertyChanged(nameof(IsRepairsViewActive));
        OnPropertyChanged(nameof(IsSettingsViewActive));
        OnPropertyChanged(nameof(IsAboutViewActive));
    }

    private void OnRecoveryCoordinatorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName))
        {
            return;
        }

        OnPropertyChanged(e.PropertyName);
    }

    private void SyncSearchText()
    {
        _workspaceViewModel.SearchText = _searchText;
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
