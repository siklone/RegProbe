using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine.Intelligence;
using WindowsOptimizer.Engine.Services;
using WindowsOptimizer.Infrastructure;
using WindowsOptimizer.Infrastructure.Services;
using WindowsOptimizer.App.Services.TweakProviders;
using WindowsOptimizer.App.Utilities;

namespace WindowsOptimizer.App.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private NavigationItem? _selectedNavigationItem;
    private ViewModelBase? _currentViewModel;
    private string _searchText = string.Empty;
    private readonly RelayCommand _clearSearchCommand;
    private readonly IHardwareDiscoveryService _hardwareDiscovery = new HardwareDiscoveryService();
    private readonly IRecommendationEngine _recommendationEngine = new RecommendationEngine();
    private readonly IRollbackStateStore _rollbackStore;
    private TweaksViewModel? _tweaksViewModel;

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

    public MainViewModel()
    {
        LogToFile("========== APPLICATION STARTED ==========");

        // Initialize rollback store for crash recovery
        var paths = AppPaths.FromEnvironment();
        _rollbackStore = new RollbackStateStore(paths);

        // Initialize crash recovery commands
        RecoverPendingRollbacksCommand = new RelayCommand(_ => _ = RecoverPendingRollbacksAsync(), _ => HasPendingRollbacks && !IsRecovering);
        DismissPendingRollbacksCommand = new RelayCommand(_ => _ = DismissPendingRollbacksAsync(), _ => HasPendingRollbacks && !IsRecovering);

        var dashboard = new DashboardViewModel();

        // Initialize all tweak providers
        var providers = new ITweakProvider[]
        {
            new SystemTweakProvider(),
            new PrivacyTweakProvider(),
            new SecurityTweakProvider(),
            new NetworkTweakProvider(),
            new PowerTweakProvider(),
            new PeripheralTweakProvider(),
            new VisibilityTweakProvider(),
            new PerformanceTweakProvider(),
            new AudioTweakProvider(),
            new MiscTweakProvider(),
            new LegacyTweakProvider()
        };

        var tweaks = new TweaksViewModel(providers);
        _tweaksViewModel = tweaks;
        dashboard.SetTweaksViewModel(tweaks);

        MonitorViewModel monitor;
        try
        {
            monitor = new MonitorViewModel();
            System.Diagnostics.Debug.WriteLine("MonitorViewModel created successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CRITICAL: Failed to create MonitorViewModel: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException?.Message}");
            throw; // Re-throw to see the actual error
        }

        var settings = new SettingsViewModel();
        var about = new AboutViewModel();

        NavigationItems = new ObservableCollection<NavigationItem>
        {
            new("dashboard", "Dashboard", "📊", dashboard),
            new("tweaks", "Tweaks", "⚙️", tweaks),
            new("monitor", "Monitor", "📈", monitor),
            new("settings", "Settings", "⚡", settings),
            new("about", "About", "ℹ️", about)
        };

        // Link Health Score + Counts (live from Tweaks)
        void SyncDashboardHealth()
        {
            dashboard.TotalTweaksAvailable = tweaks.Tweaks.Count;
            dashboard.TweaksApplied = tweaks.ScorableTweaksApplied;
            dashboard.HealthTweaksTotal = tweaks.ScorableTweaksMeasuredTotal;
            dashboard.HealthTweaksApplied = tweaks.ScorableTweaksApplied;
            dashboard.OptimizationScore = tweaks.GlobalOptimizationScore;

            // Sync for tray tooltip
            OptimizationScore = tweaks.GlobalOptimizationScore;
            TweaksApplied = tweaks.ScorableTweaksApplied;
            TotalTweaksAvailable = tweaks.Tweaks.Count;
        }

        tweaks.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(TweaksViewModel.GlobalOptimizationScore)
                or nameof(TweaksViewModel.ScorableTweaksMeasuredTotal)
                or nameof(TweaksViewModel.ScorableTweaksApplied))
            {
                SyncDashboardHealth();
            }
        };
        SyncDashboardHealth();

        SelectedNavigationItem = NavigationItems[0];

        // Set up navigation callback for Dashboard category links
        dashboard.NavigateToCategoryRequested = (category) =>
        {
            // Navigate to Tweaks tab
            var tweaksNav = NavigationItems.FirstOrDefault(n => n.Id == "tweaks");
            if (tweaksNav != null)
            {
                SelectedNavigationItem = tweaksNav;
                // Clear status filter and set search filter to the category
                tweaks.StatusFilter = "";
                SearchText = category;
            }
        };

        // Set up status filter callback for Dashboard stat cards (applied/rolled back)
        dashboard.NavigateToStatusFilterRequested = (status) =>
        {
            // Navigate to Tweaks tab
            var tweaksNav = NavigationItems.FirstOrDefault(n => n.Id == "tweaks");
            if (tweaksNav != null)
            {
                SelectedNavigationItem = tweaksNav;
                // Clear search text and set status filter
                SearchText = "";
                tweaks.StatusFilter = status;
            }
        };

        _clearSearchCommand = new RelayCommand(_ => SearchText = string.Empty, _ => !string.IsNullOrEmpty(SearchText));

        // Keyboard navigation commands
        NavigateToDashboardCommand = new RelayCommand(_ => NavigateToTab(0));
        NavigateToTweaksCommand = new RelayCommand(_ => NavigateToTab(1));
        NavigateToMonitorCommand = new RelayCommand(_ => NavigateToTab(2));
        NavigateToSettingsCommand = new RelayCommand(_ => NavigateToTab(3));
        NavigateToAboutCommand = new RelayCommand(_ => NavigateToTab(4));
        FocusSearchCommand = new RelayCommand(_ => OnFocusSearchRequested());
        ClearFiltersCommand = new RelayCommand(_ => OnClearFilters());

        // Initialize Intelligence
        Task.Run(InitializeIntelligenceAsync);

        // Check for pending rollbacks from previous crashes
        Task.Run(CheckPendingRollbacksAsync);

    }

    public async Task RunStartupScanAsync(IProgress<StartupScanProgress>? progress = null, CancellationToken ct = default)
    {
        if (NavigationItems.FirstOrDefault(n => n.Id == "dashboard")?.ViewModel is DashboardViewModel dashboard)
        {
            await RunStartupScanAsync(dashboard, progress, ct);
        }
    }

    private async Task RunStartupScanAsync(DashboardViewModel dashboard, IProgress<StartupScanProgress>? progress, CancellationToken ct)
    {
        if (_tweaksViewModel == null || IsStartupScanActive)
        {
            return;
        }

        IsStartupScanActive = true;
        try
        {
            await dashboard.RunScanAsync(progress, ct);
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
        private set => SetProperty(ref _isStartupScanActive, value);
    }

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
    public RelayCommand NavigateToMonitorCommand { get; }
    public RelayCommand NavigateToSettingsCommand { get; }
    public RelayCommand NavigateToAboutCommand { get; }
    public RelayCommand FocusSearchCommand { get; }
    public RelayCommand ClearFiltersCommand { get; }

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
            var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WindowsOptimizer_Debug.log");
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            System.IO.File.AppendAllText(logPath, $"[{timestamp}] {message}\n");
        }
        catch
        {
            // Ignore logging errors
        }
    }
}
