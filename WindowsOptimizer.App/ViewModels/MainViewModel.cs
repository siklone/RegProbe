using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine.Intelligence;
using WindowsOptimizer.Engine.Services;
using WindowsOptimizer.Infrastructure;
using WindowsOptimizer.Infrastructure.Services;
using WindowsOptimizer.App.Services.TweakProviders;
using WindowsOptimizer.App.Utilities;

using WindowsOptimizer.App.Services;

namespace WindowsOptimizer.App.ViewModels;

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private const string DashboardQuickActionScanKey = "scan";
    private const string DashboardQuickActionFastCleanKey = "fast-clean";
    private const string DashboardQuickActionMaintenanceKey = "maintenance";

    private NavigationItem? _selectedNavigationItem;
    private ViewModelBase? _currentViewModel;
    private string _searchText = string.Empty;
    private readonly RelayCommand _clearSearchCommand;
    public TrayViewModel TrayViewModel { get; } = new TrayViewModel();
    private readonly IHardwareDiscoveryService _hardwareDiscovery = new HardwareDiscoveryService();
    private readonly IRecommendationEngine _recommendationEngine = new RecommendationEngine();
    private readonly IRollbackStateStore _rollbackStore;
    private readonly IBusyService _busyService = new BusyService();
    private TweaksViewModel? _tweaksViewModel;
    private System.ComponentModel.PropertyChangedEventHandler? _tweaksPropertyChangedHandler;

    // Tray tooltip binding properties
    private int _optimizationScore;
    private int _tweaksApplied;
    private int _totalTweaksAvailable;

    // Crash recovery properties
    private bool _hasPendingRollbacks;
    private int _pendingRollbackCount;
    private string _pendingRollbackMessage = string.Empty;
    private bool _isRecovering;
    private bool _isStartupScanActive;
    private bool _isDashboardQuickActionRunning;
    private string _dashboardQuickActionKey = string.Empty;
    private string _dashboardQuickActionStatus = "Run a quick settings scan or a lightweight cleanup pass right from the dashboard.";

    public MainViewModel()
    {
        LogToFile("========== APPLICATION STARTED ==========");

        // Initialize rollback store for crash recovery
        var paths = AppPaths.FromEnvironment();
        _rollbackStore = new RollbackStateStore(paths);

        // Initialize crash recovery commands
        RecoverPendingRollbacksCommand = new RelayCommand(_ => _ = RecoverPendingRollbacksAsync(), _ => HasPendingRollbacks && !IsRecovering);
        DismissPendingRollbacksCommand = new RelayCommand(_ => _ = DismissPendingRollbacksAsync(), _ => HasPendingRollbacks && !IsRecovering);

        // Initialize all tweak providers
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

        var bloatware = new BloatwareViewModel();
        var startup = new StartupViewModel();
        var tweaks = new TweaksViewModel(providers, _busyService, bloatware, startup);
        _tweaksViewModel = tweaks;

        MonitorViewModel monitor;
        try
        {
            monitor = new MonitorViewModel(AppServices.MetricWorkerPool);
        }
        catch (Exception ex)
        {
            LogToFile($"MonitorViewModel creation failed: {ex.Message}");
            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                LogToFile(ex.StackTrace);
            }

            if (!string.IsNullOrWhiteSpace(ex.InnerException?.Message))
            {
                LogToFile($"Inner: {ex.InnerException.Message}");
            }

            throw; // Re-throw to see the actual error
        }

        var settings = new SettingsViewModel();
        var about = new AboutViewModel();
        var dashboard = new DashboardViewModel();

        NavigationItems = new ObservableCollection<NavigationItem>
        {
            new NavigationItem("dashboard", "Dashboard", "📊", dashboard),
            new NavigationItem("tweaks", "Configuration", "🛠️", tweaks),
            new NavigationItem("monitor", "Monitor", "📈", monitor),
            new NavigationItem("settings", "Settings", "⚙️", settings),
            new NavigationItem("about", "About", "ℹ️", about)
        };
        
        // Sync tray tooltip values from tweaks
        SyncTrayTooltipValues();

        // Named handler for proper unsubscription
        _tweaksPropertyChangedHandler = (s, e) =>
        {
            if (e.PropertyName is nameof(TweaksViewModel.GlobalOptimizationScore)
                or nameof(TweaksViewModel.ScorableTweaksMeasuredTotal)
                or nameof(TweaksViewModel.ScorableTweaksApplied))
            {
                SyncTrayTooltipValues();
            }

            if (e.PropertyName is nameof(TweaksViewModel.IsBulkRunning))
            {
                RaiseDashboardQuickActionCanExecuteChanged();
            }
        };
        tweaks.PropertyChanged += _tweaksPropertyChangedHandler;

        SelectedNavigationItem = NavigationItems[0];

        _clearSearchCommand = new RelayCommand(_ => SearchText = string.Empty, _ => !string.IsNullOrEmpty(SearchText));

        // Keyboard navigation commands
        NavigateToDashboardCommand = new RelayCommand(_ => NavigateToTab(0));
        NavigateToTweaksCommand = new RelayCommand(_ => NavigateToTab(1));
        NavigateToBloatwareCommand = new RelayCommand(_ => NavigateToTab(1)); // Redirect to Configuration
        NavigateToStartupCommand = new RelayCommand(_ => NavigateToTab(1));   // Redirect to Configuration
        NavigateToMonitorCommand = new RelayCommand(_ => NavigateToTab(2));
        NavigateToSettingsCommand = new RelayCommand(_ => NavigateToTab(3));
        NavigateToAboutCommand = new RelayCommand(_ => NavigateToTab(4));
        FocusSearchCommand = new RelayCommand(_ => OnFocusSearchRequested());
        ClearFiltersCommand = new RelayCommand(_ => OnClearFilters());
        RunDashboardScanCommand = new RelayCommand(_ => _ = RunDashboardScanAsync(), _ => CanRunDashboardQuickAction());
        RunFastCleanCommand = new RelayCommand(_ => _ = RunFastCleanAsync(), _ => CanRunDashboardQuickAction());
        OpenMaintenanceWorkspaceCommand = new RelayCommand(_ => OpenMaintenanceWorkspace(), _ => CanRunDashboardQuickAction());

        // Initialize Intelligence with error handling
        _ = Task.Run(async () =>
        {
            try
            {
                await InitializeIntelligenceAsync();
            }
            catch (Exception ex)
            {
                LogToFile($"Intelligence initialization failed: {ex.Message}");
            }
        });

        // Check for pending rollbacks from previous crashes with error handling
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

    private void SyncTrayTooltipValues()
    {
        if (_tweaksViewModel == null)
            return;

        // Sync for tray tooltip
        OptimizationScore = _tweaksViewModel.GlobalOptimizationScore;
        TweaksApplied = _tweaksViewModel.ScorableTweaksApplied;
        TotalTweaksAvailable = _tweaksViewModel.TotalTweaksAvailable;
    }

    public void Dispose()
    {
        // Unsubscribe event handlers to prevent memory leaks
        if (_tweaksViewModel != null && _tweaksPropertyChangedHandler != null)
        {
            _tweaksViewModel.PropertyChanged -= _tweaksPropertyChangedHandler;
        }

        TrayViewModel.Dispose();

        // Dispose all view models
        foreach (var item in NavigationItems)
        {
            if (item.ViewModel is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    public IBusyService BusyService => _busyService;

    public async Task RunStartupScanAsync(IProgress<StartupScanProgress>? progress = null, CancellationToken ct = default)
    {
        if (_tweaksViewModel == null || IsStartupScanActive)
        {
            return;
        }

        IsStartupScanActive = true;
        try
        {
            // Perform initial status check for all tweaks
            await _tweaksViewModel.DetectAllTweaksAsync(progress, ct, isStartupScan: true, forceRedetect: true, skipElevationPrompts: true, skipExpensiveOperations: true);
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

    private void QueueBackgroundInventoryRefresh()
    {
        if (_tweaksViewModel == null)
        {
            return;
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
        {
            _ = _tweaksViewModel.RefreshInventoryInBackgroundAsync(CancellationToken.None);
            return;
        }

        _ = dispatcher.InvokeAsync(async () =>
        {
            try
            {
                await Task.Delay(250);
                await _tweaksViewModel.RefreshInventoryInBackgroundAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                LogToFile($"Background inventory refresh failed: {ex.Message}");
            }
        }, DispatcherPriority.ContextIdle);
    }


    private async Task InitializeIntelligenceAsync()
    {
        try
        {
            var profile = await _hardwareDiscovery.GetHardwareProfileAsync();
            var recommendations = _recommendationEngine.GetRecommendations(profile);
            
            foreach (var item in NavigationItems)
            {
                if (item.ViewModel is TweaksViewModel tweaks)
                {
                    tweaks.ApplyRecommendations(recommendations);
                }
            }
        }
        catch
        {
            // Fail silently or log
        }
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    public string AppVersionLabel => AppInfo.VersionLabel;

    public string AppCopyrightLabel => AppInfo.CopyrightLabel;

    // Tray tooltip properties
    public int OptimizationScore
    {
        get => _optimizationScore;
        private set => SetProperty(ref _optimizationScore, value);
    }

    public int TweaksApplied
    {
        get => _tweaksApplied;
        private set => SetProperty(ref _tweaksApplied, value);
    }

    public int TotalTweaksAvailable
    {
        get => _totalTweaksAvailable;
        private set => SetProperty(ref _totalTweaksAvailable, value);
    }

    // Crash recovery properties
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
        private set
        {
            if (SetProperty(ref _isStartupScanActive, value))
            {
                RaiseDashboardQuickActionCanExecuteChanged();
            }
        }
    }

    public bool IsDashboardQuickActionRunning
    {
        get => _isDashboardQuickActionRunning;
        private set
        {
            if (SetProperty(ref _isDashboardQuickActionRunning, value))
            {
                RaiseDashboardQuickActionCanExecuteChanged();
            }
        }
    }

    public string DashboardQuickActionStatus
    {
        get => _dashboardQuickActionStatus;
        private set => SetProperty(ref _dashboardQuickActionStatus, value);
    }

    public string DashboardQuickActionKey
    {
        get => _dashboardQuickActionKey;
        private set
        {
            if (SetProperty(ref _dashboardQuickActionKey, value))
            {
                OnPropertyChanged(nameof(IsDashboardScanSelected));
                OnPropertyChanged(nameof(IsDashboardFastCleanSelected));
                OnPropertyChanged(nameof(IsDashboardMaintenanceSelected));
            }
        }
    }

    public bool IsDashboardScanSelected =>
        string.Equals(DashboardQuickActionKey, DashboardQuickActionScanKey, StringComparison.OrdinalIgnoreCase);

    public bool IsDashboardFastCleanSelected =>
        string.Equals(DashboardQuickActionKey, DashboardQuickActionFastCleanKey, StringComparison.OrdinalIgnoreCase);

    public bool IsDashboardMaintenanceSelected =>
        string.Equals(DashboardQuickActionKey, DashboardQuickActionMaintenanceKey, StringComparison.OrdinalIgnoreCase);

    public ICommand RecoverPendingRollbacksCommand { get; }

    public ICommand DismissPendingRollbacksCommand { get; }

    public NavigationItem? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            try
            {
                LogToFile($"SelectedNavigationItem setter: New value = {value?.Id}");
                if (SetProperty(ref _selectedNavigationItem, value))
                {
                    LogToFile($"SelectedNavigationItem: Setting CurrentViewModel to {_selectedNavigationItem?.Id}");
                    CurrentViewModel = _selectedNavigationItem?.ViewModel;
                    LogToFile($"SelectedNavigationItem: CurrentViewModel set successfully");
                    SyncSearchText();
                }
            }
            catch (Exception ex)
            {
                LogToFile($"CRASH in SelectedNavigationItem setter: {ex.Message}");
                LogToFile($"Stack: {ex.StackTrace}");
                throw;
            }
        }
    }

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            try
            {
                LogToFile($"CurrentViewModel setter: Setting to {value?.GetType().Name}");
                SetProperty(ref _currentViewModel, value);
                LogToFile($"CurrentViewModel setter: Set complete");
            }
            catch (Exception ex)
            {
                LogToFile($"CRASH in CurrentViewModel setter: {ex.Message}");
                LogToFile($"Stack: {ex.StackTrace}");
                throw;
            }
        }
    }

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

    // Keyboard Navigation Commands
    public RelayCommand NavigateToDashboardCommand { get; }
    public RelayCommand NavigateToTweaksCommand { get; }
    public RelayCommand NavigateToBloatwareCommand { get; }
    public RelayCommand NavigateToStartupCommand { get; }
    public RelayCommand NavigateToMonitorCommand { get; }
    public RelayCommand NavigateToSettingsCommand { get; }
    public RelayCommand NavigateToAboutCommand { get; }
    public RelayCommand FocusSearchCommand { get; }
    public RelayCommand ClearFiltersCommand { get; }
    public RelayCommand RunDashboardScanCommand { get; }
    public RelayCommand RunFastCleanCommand { get; }
    public RelayCommand OpenMaintenanceWorkspaceCommand { get; }

    public event Action? FocusSearchRequested;

    private void NavigateToTab(int index)
    {
        if (index >= 0 && index < NavigationItems.Count)
        {
            SelectedNavigationItem = NavigationItems[index];
        }
    }

    private void OnFocusSearchRequested()
    {
        // Navigate to Tweaks tab where search is available
        NavigateToTab(1);
        FocusSearchRequested?.Invoke();
    }

    private void OnClearFilters()
    {
        SearchText = string.Empty;
        if (_currentViewModel is TweaksViewModel tweaksViewModel)
        {
            tweaksViewModel.StatusFilter = string.Empty;
            tweaksViewModel.ShowFavoritesOnly = false;
        }
    }

    private void SyncSearchText()
    {
        if (_currentViewModel is TweaksViewModel tweaksViewModel)
        {
            tweaksViewModel.SearchText = _searchText;
        }
    }

    private bool CanRunDashboardQuickAction()
    {
        return _tweaksViewModel != null
            && !IsStartupScanActive
            && !IsDashboardQuickActionRunning
            && !_tweaksViewModel.IsBulkRunning;
    }

    private void RaiseDashboardQuickActionCanExecuteChanged()
    {
        RunDashboardScanCommand?.RaiseCanExecuteChanged();
        RunFastCleanCommand?.RaiseCanExecuteChanged();
        OpenMaintenanceWorkspaceCommand?.RaiseCanExecuteChanged();
    }

    private void OpenMaintenanceWorkspace()
    {
        if (_tweaksViewModel == null || !CanRunDashboardQuickAction())
        {
            return;
        }

        DashboardQuickActionKey = DashboardQuickActionMaintenanceKey;
        NavigateToTab(1);
        _tweaksViewModel.ShowMaintenanceCleanupWorkspace();
        DashboardQuickActionStatus = "Opened Maintenance with cleanup tasks front and center.";
    }

    private async Task RunDashboardScanAsync()
    {
        if (_tweaksViewModel == null || !CanRunDashboardQuickAction())
        {
            return;
        }

        DashboardQuickActionKey = DashboardQuickActionScanKey;
        IsDashboardQuickActionRunning = true;
        DashboardQuickActionStatus = "Scanning Windows settings and live tweak states...";

        try
        {
            using var busy = _busyService.Busy("Scanning Windows settings...");
            await _tweaksViewModel.RefreshInventoryInBackgroundAsync(CancellationToken.None);
            DashboardQuickActionStatus = "Quick scan finished. Current states are refreshed.";
        }
        catch (Exception ex)
        {
            DashboardQuickActionStatus = $"Quick scan could not finish: {ex.Message}";
            LogToFile($"Dashboard quick scan failed: {ex.Message}");
        }
        finally
        {
            IsDashboardQuickActionRunning = false;
        }
    }

    private async Task RunFastCleanAsync()
    {
        if (_tweaksViewModel == null || !CanRunDashboardQuickAction())
        {
            return;
        }

        DashboardQuickActionKey = DashboardQuickActionFastCleanKey;
        IsDashboardQuickActionRunning = true;
        DashboardQuickActionStatus = "Running Fast Clean on lightweight caches and cleanup tasks...";

        try
        {
            using var busy = _busyService.Busy("Running Fast Clean...");
            await _tweaksViewModel.RunFastCleanAsync(CancellationToken.None);
            DashboardQuickActionStatus = "Fast Clean completed. Lightweight cleanup tasks finished successfully.";
        }
        catch (Exception ex)
        {
            DashboardQuickActionStatus = $"Fast Clean could not finish: {ex.Message}";
            LogToFile($"Dashboard Fast Clean failed: {ex.Message}");
        }
        finally
        {
            IsDashboardQuickActionRunning = false;
        }
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
        if (_tweaksViewModel == null) return;

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
                    // Find the tweak in the view model and trigger rollback
                    var tweakVm = _tweaksViewModel.Tweaks.FirstOrDefault(t => t.Id == entry.TweakId);
                    if (tweakVm != null && tweakVm.IsApplied)
                    {
                        await tweakVm.RunRollbackAsync(CancellationToken.None);
                        successCount++;
                        LogToFile($"Crash recovery: Rolled back {entry.TweakId}");
                    }
                    else
                    {
                        // Tweak not found or already rolled back, mark as recovered
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
            var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WindowsOptimizer_Diagnostics.log");
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            System.IO.File.AppendAllText(logPath, $"[{timestamp}] {message}\n");
        }
        catch
        {
            // Ignore logging errors
        }
    }
}
