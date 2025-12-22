using System.Collections.ObjectModel;

namespace WindowsOptimizer.App.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private NavigationItem? _selectedNavigationItem;
    private ViewModelBase? _currentViewModel;
    private string _searchText = string.Empty;
    private readonly RelayCommand _clearSearchCommand;

    public MainViewModel()
    {
        var dashboard = new DashboardViewModel();
        var tweaks = new TweaksViewModel();
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
