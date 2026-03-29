using System.Linq;
using System.Threading;
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
    private readonly IRollbackStateStore _rollbackStore;
    private readonly IBusyService _busyService = new BusyService();
    private readonly TweaksViewModel _workspaceViewModel;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly AboutViewModel _aboutViewModel;

    private bool _hasPendingRollbacks;
    private int _pendingRollbackCount;
    private string _pendingRollbackMessage = string.Empty;
    private bool _isRecovering;
    private bool _isStartupScanActive;

    public MainViewModel()
    {
        LogToFile("========== APPLICATION STARTED ==========");

        var paths = AppPaths.FromEnvironment();
        _rollbackStore = new RollbackStateStore(paths);

        RecoverPendingRollbacksCommand = new RelayCommand(_ => _ = RecoverPendingRollbacksAsync(), _ => HasPendingRollbacks && !IsRecovering);
        DismissPendingRollbacksCommand = new RelayCommand(_ => _ = DismissPendingRollbacksAsync(), _ => HasPendingRollbacks && !IsRecovering);

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
        _settingsViewModel = new SettingsViewModel();
        _aboutViewModel = new AboutViewModel();

        _clearSearchCommand = new RelayCommand(_ => SearchText = string.Empty, _ => !string.IsNullOrEmpty(SearchText));

        ShowConfigurationCommand = new RelayCommand(_ => ShowConfiguration());
        ShowWorkspaceCommand = new RelayCommand(_ => ShowWorkspace());
        ShowSettingsCommand = new RelayCommand(_ => ShowSettings());
        ShowAboutCommand = new RelayCommand(_ => ShowAbout());
        FocusSearchCommand = new RelayCommand(_ => OnFocusSearchRequested());
        ClearFiltersCommand = new RelayCommand(_ => OnClearFilters());

        ShowConfiguration();

        _ = Task.Run(async () =>
        {
            try
            {
                await CheckPendingRollbacksAsync();
            }
            catch (Exception ex)
            {
                LogToFile($"Pending rollback check failed: {ex.Message}");
            }
        });
    }

    public IBusyService BusyService => _busyService;

    public string AppVersionLabel => AppInfo.VersionLabel;

    public string AppCopyrightLabel => AppInfo.CopyrightLabel;

    public ICommand RecoverPendingRollbacksCommand { get; }

    public ICommand DismissPendingRollbacksCommand { get; }

    public RelayCommand ShowWorkspaceCommand { get; }

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
                    OnPropertyChanged(nameof(IsWorkspaceViewActive));
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

    public bool IsConfigurationViewActive =>
        ReferenceEquals(CurrentViewModel, _workspaceViewModel) &&
        _workspaceViewModel.SelectedWorkspace == ConfigurationWorkspaceKind.Settings;

    public bool IsWorkspaceViewActive =>
        ReferenceEquals(CurrentViewModel, _workspaceViewModel) &&
        _workspaceViewModel.SelectedWorkspace == ConfigurationWorkspaceKind.Maintenance;

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
        get => _hasPendingRollbacks;
        private set
        {
            if (SetProperty(ref _hasPendingRollbacks, value))
            {
                ((RelayCommand)RecoverPendingRollbacksCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DismissPendingRollbacksCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public int PendingRollbackCount
    {
        get => _pendingRollbackCount;
        private set => SetProperty(ref _pendingRollbackCount, value);
    }

    public string PendingRollbackMessage
    {
        get => _pendingRollbackMessage;
        private set => SetProperty(ref _pendingRollbackMessage, value);
    }

    public bool IsRecovering
    {
        get => _isRecovering;
        private set
        {
            if (SetProperty(ref _isRecovering, value))
            {
                ((RelayCommand)RecoverPendingRollbacksCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DismissPendingRollbacksCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsStartupScanActive
    {
        get => _isStartupScanActive;
        private set => SetProperty(ref _isStartupScanActive, value);
    }

    public async Task RunStartupScanAsync(IProgress<StartupScanProgress>? progress = null, CancellationToken ct = default)
    {
        if (IsStartupScanActive)
        {
            return;
        }

        IsStartupScanActive = true;
        try
        {
            await _workspaceViewModel.DetectAllTweaksAsync(progress, ct, isStartupScan: true, forceRedetect: true, skipElevationPrompts: true, skipExpensiveOperations: true);
            QueueBackgroundInventoryRefresh();
        }
        catch (Exception ex)
        {
            LogToFile($"Startup scan failed: {ex.Message}");
        }
        finally
        {
            IsStartupScanActive = false;
        }
    }

    public void Dispose()
    {
        _workspaceViewModel.PropertyChanged -= OnWorkspaceViewModelPropertyChanged;

        if (_workspaceViewModel is IDisposable workspaceDisposable)
        {
            workspaceDisposable.Dispose();
        }

        if (_settingsViewModel is IDisposable settingsDisposable)
        {
            settingsDisposable.Dispose();
        }
    }

    private void QueueBackgroundInventoryRefresh()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
        {
            _ = _workspaceViewModel.RefreshInventoryInBackgroundAsync(CancellationToken.None);
            return;
        }

        _ = dispatcher.InvokeAsync(async () =>
        {
            try
            {
                await Task.Delay(250);
                await _workspaceViewModel.RefreshInventoryInBackgroundAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                LogToFile($"Background inventory refresh failed: {ex.Message}");
            }
        }, DispatcherPriority.ContextIdle);
    }

    private void ShowConfiguration()
    {
        _workspaceViewModel.SelectedWorkspace = ConfigurationWorkspaceKind.Settings;
        CurrentViewModel = _workspaceViewModel;
        SyncSearchText();
        RaiseShellViewStateChanged();
    }

    private void ShowWorkspace()
    {
        _workspaceViewModel.SelectedWorkspace = ConfigurationWorkspaceKind.Maintenance;
        CurrentViewModel = _workspaceViewModel;
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
        ShowConfiguration();
        SearchText = string.Empty;
        _workspaceViewModel.StatusFilter = string.Empty;
        _workspaceViewModel.ShowFavoritesOnly = false;
    }

    private void OnWorkspaceViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!ReferenceEquals(CurrentViewModel, _workspaceViewModel))
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
        OnPropertyChanged(nameof(IsWorkspaceViewActive));
        OnPropertyChanged(nameof(IsSettingsViewActive));
        OnPropertyChanged(nameof(IsAboutViewActive));
    }

    private void SyncSearchText()
    {
        _workspaceViewModel.SearchText = _searchText;
    }

    private async Task CheckPendingRollbacksAsync()
    {
        try
        {
            var pending = await _rollbackStore.GetPendingRollbacksAsync(CancellationToken.None);
            if (pending.Count > 0)
            {
                PendingRollbackCount = pending.Count;
                PendingRollbackMessage = pending.Count == 1
                    ? $"1 tweak was not properly rolled back after a crash: {pending[0].TweakId}"
                    : $"{pending.Count} tweaks were not properly rolled back after a crash.";
                HasPendingRollbacks = true;
                LogToFile($"Crash recovery: Found {pending.Count} pending rollbacks");
            }
        }
        catch (Exception ex)
        {
            LogToFile($"Crash recovery check failed: {ex.Message}");
        }
    }

    private async Task RecoverPendingRollbacksAsync()
    {
        IsRecovering = true;
        LogToFile("Crash recovery: Starting rollback recovery...");

        try
        {
            var pending = await _rollbackStore.GetPendingRollbacksAsync(CancellationToken.None);
            var successCount = 0;

            foreach (var entry in pending)
            {
                try
                {
                    var tweakVm = _workspaceViewModel.Tweaks.FirstOrDefault(t => t.Id == entry.TweakId);
                    if (tweakVm != null && tweakVm.IsApplied)
                    {
                        await tweakVm.RunRollbackAsync(CancellationToken.None);
                        successCount++;
                        LogToFile($"Crash recovery: Rolled back {entry.TweakId}");
                    }
                    else
                    {
                        await _rollbackStore.MarkRolledBackAsync(entry.TweakId, CancellationToken.None);
                        successCount++;
                        LogToFile($"Crash recovery: Marked {entry.TweakId} as recovered (not found or already rolled back)");
                    }
                }
                catch (Exception ex)
                {
                    LogToFile($"Crash recovery: Failed to rollback {entry.TweakId}: {ex.Message}");
                }
            }

            if (successCount == pending.Count)
            {
                HasPendingRollbacks = false;
                PendingRollbackMessage = string.Empty;
                PendingRollbackCount = 0;
                LogToFile("Crash recovery: All rollbacks completed successfully");
            }
            else
            {
                PendingRollbackMessage = $"Recovery completed with {pending.Count - successCount} failures. Check logs for details.";
            }
        }
        catch (Exception ex)
        {
            LogToFile($"Crash recovery failed: {ex.Message}");
            PendingRollbackMessage = $"Recovery failed: {ex.Message}";
        }
        finally
        {
            IsRecovering = false;
        }
    }

    private async Task DismissPendingRollbacksAsync()
    {
        LogToFile("Crash recovery: User dismissed pending rollbacks");

        try
        {
            await _rollbackStore.ClearAllAsync(CancellationToken.None);
            HasPendingRollbacks = false;
            PendingRollbackMessage = string.Empty;
            PendingRollbackCount = 0;
        }
        catch (Exception ex)
        {
            LogToFile($"Failed to clear pending rollbacks: {ex.Message}");
        }
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
