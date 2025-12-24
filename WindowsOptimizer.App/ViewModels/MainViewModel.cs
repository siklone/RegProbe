using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine.Intelligence;
using WindowsOptimizer.App.Services.TweakProviders;

namespace WindowsOptimizer.App.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private NavigationItem? _selectedNavigationItem;
    private ViewModelBase? _currentViewModel;
    private string _searchText = string.Empty;
    private readonly RelayCommand _clearSearchCommand;
    private readonly IHardwareDiscoveryService _hardwareDiscovery = new HardwareDiscoveryService();
    private readonly IRecommendationEngine _recommendationEngine = new RecommendationEngine();

    public MainViewModel()
    {
        var dashboard = new DashboardViewModel();
        var systemProvider = new SystemTweakProvider();
        var tweaks = new TweaksViewModel(new ITweakProvider[] { systemProvider });
        var monitor = new MonitorViewModel();
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

        // Link Health Score
        tweaks.PropertyChanged += (s, e) => {
            if (e.PropertyName == nameof(TweaksViewModel.GlobalOptimizationScore)) {
                dashboard.OptimizationScore = tweaks.GlobalOptimizationScore;
            }
        };
        dashboard.OptimizationScore = tweaks.GlobalOptimizationScore;

        SelectedNavigationItem = NavigationItems[0];

        _clearSearchCommand = new RelayCommand(_ => SearchText = string.Empty, _ => !string.IsNullOrEmpty(SearchText));

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

    public NavigationItem? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            if (SetProperty(ref _selectedNavigationItem, value))
            {
                CurrentViewModel = _selectedNavigationItem?.ViewModel;
                SyncSearchText();
            }
        }
    }

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        private set => SetProperty(ref _currentViewModel, value);
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

    private void SyncSearchText()
    {
        if (_currentViewModel is TweaksViewModel tweaksViewModel)
        {
            tweaksViewModel.SearchText = _searchText;
        }
    }
}
