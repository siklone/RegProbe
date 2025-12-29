using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine.Intelligence;
using WindowsOptimizer.Engine.Services;
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

    // Tray tooltip binding properties
    private int _optimizationScore;
    private int _tweaksApplied;
    private int _totalTweaksAvailable;

    public MainViewModel()
    {
        LogToFile("========== APPLICATION STARTED ==========");

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
            new MiscTweakProvider()
        };

        var tweaks = new TweaksViewModel(providers);
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
