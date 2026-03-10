using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.Infrastructure;
using WindowsOptimizer.Infrastructure.Elevation;
using CoreServiceStartMode = WindowsOptimizer.Core.Services.ServiceStartMode;
using IServiceManager = WindowsOptimizer.Core.Services.IServiceManager;

namespace WindowsOptimizer.App.ViewModels;

public sealed class ServiceManagementPanelViewModel : ViewModelBase
{
    private readonly WindowsServiceCatalogService _catalogService = new();
    private readonly IServiceManager _serviceManager;
    private readonly IAppLogger _appLogger;
    private readonly bool _isElevatedHostAvailable;
    private readonly RelayCommand _refreshCommand;
    private readonly RelayCommand _openDocsCommand;
    private readonly RelayCommand _enableServiceCommand;
    private readonly RelayCommand _disableServiceCommand;
    private readonly RelayCommand _setFilterCommand;
    private readonly ObservableCollection<WindowsServiceCatalogEntry> _items = new();
    private readonly ICollectionView _itemsView;
    private string _searchText = string.Empty;
    private string _context = "Loading service inventory...";
    private string _statusMessage = "Loading services...";
    private bool _isRefreshing;
    private WindowsServiceCatalogEntry? _selectedService;
    private int _runningCount;
    private int _disabledCount;
    private int _driverCount;
    private ServiceListFilter _selectedFilter = ServiceListFilter.All;

    public ServiceManagementPanelViewModel(IServiceManager serviceManager, bool isElevatedHostAvailable, IAppLogger appLogger)
    {
        _serviceManager = serviceManager ?? throw new ArgumentNullException(nameof(serviceManager));
        _isElevatedHostAvailable = isElevatedHostAvailable;
        _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
        _itemsView = CollectionViewSource.GetDefaultView(_items);
        _itemsView.Filter = FilterItems;

        _refreshCommand = new RelayCommand(_ => _ = RefreshAsync(), _ => !IsRefreshing);
        _openDocsCommand = new RelayCommand(_ => OpenDocs(), _ => SelectedService is not null && !string.IsNullOrWhiteSpace(SelectedService.DocsLink));
        _enableServiceCommand = new RelayCommand(_ => _ = SetServiceStartModeAsync(CoreServiceStartMode.Manual), _ => CanEnableSelectedService);
        _disableServiceCommand = new RelayCommand(_ => _ = SetServiceStartModeAsync(CoreServiceStartMode.Disabled), _ => CanDisableSelectedService);
        _setFilterCommand = new RelayCommand(param => SetFilter(param as string));

        RefreshCommand = _refreshCommand;
        OpenDocsCommand = _openDocsCommand;
        EnableServiceCommand = _enableServiceCommand;
        DisableServiceCommand = _disableServiceCommand;
        SetFilterCommand = _setFilterCommand;
    }

    public ICommand RefreshCommand { get; }
    public ICommand OpenDocsCommand { get; }
    public ICommand EnableServiceCommand { get; }
    public ICommand DisableServiceCommand { get; }
    public ICommand SetFilterCommand { get; }

    public ICollectionView ItemsView => _itemsView;

    public string Headline => "Service Management";

    public string Detail => "Review Windows services with built-in descriptions, startup modes, and safe start-mode actions.";

    public string Context
    {
        get => _context;
        private set => SetProperty(ref _context, value);
    }

    public string FilterStatusText => $"{VisibleCount} shown";

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                RefreshItemsView();
            }
        }
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set
        {
            if (SetProperty(ref _isRefreshing, value))
            {
                _refreshCommand.RaiseCanExecuteChanged();
                _enableServiceCommand.RaiseCanExecuteChanged();
                _disableServiceCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int TotalCount => _items.Count;

    public int RunningCount
    {
        get => _runningCount;
        private set => SetProperty(ref _runningCount, value);
    }

    public int DisabledCount
    {
        get => _disabledCount;
        private set => SetProperty(ref _disabledCount, value);
    }

    public int DriverCount
    {
        get => _driverCount;
        private set => SetProperty(ref _driverCount, value);
    }

    public int VisibleCount => _itemsView.Cast<object>().Count();

    public bool HasItems => _itemsView.Cast<object>().Any();
    public bool IsAllFilterSelected => _selectedFilter == ServiceListFilter.All;
    public bool IsRunningFilterSelected => _selectedFilter == ServiceListFilter.Running;
    public bool IsDisabledFilterSelected => _selectedFilter == ServiceListFilter.Disabled;
    public bool IsDriversFilterSelected => _selectedFilter == ServiceListFilter.Drivers;

    public WindowsServiceCatalogEntry? SelectedService
    {
        get => _selectedService;
        set
        {
            if (SetProperty(ref _selectedService, value))
            {
                OnPropertyChanged(nameof(SelectedDescription));
                OnPropertyChanged(nameof(SelectedIdentity));
                OnPropertyChanged(nameof(SelectedState));
                OnPropertyChanged(nameof(SelectedWhatItDoes));
                OnPropertyChanged(nameof(SelectedTechnicalDetails));
                _openDocsCommand.RaiseCanExecuteChanged();
                _enableServiceCommand.RaiseCanExecuteChanged();
                _disableServiceCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string SelectedDescription => SelectedService?.DescriptionOrFallback ?? "Select a service to see what it does and how Windows starts it.";

    public string SelectedIdentity => SelectedService == null
        ? "No service selected"
        : $"{SelectedService.DisplayName} ({SelectedService.Name})";

    public string SelectedState => SelectedService == null
        ? "Choose a service from the list."
        : $"{SelectedService.Status} | {SelectedService.StartType} | {SelectedService.KindText}";

    public string SelectedWhatItDoes => SelectedService?.DescriptionOrFallback ?? string.Empty;

    public string SelectedTechnicalDetails => SelectedService == null
        ? string.Empty
        : $"Account: {FormatValue(SelectedService.Account)}\nGroup: {FormatValue(SelectedService.Group)}\nPath: {FormatValue(SelectedService.BinaryPath)}\nRegistry: {FormatValue(SelectedService.RegistryPath)}";

    public bool CanEnableSelectedService =>
        _isElevatedHostAvailable &&
        !IsRefreshing &&
        SelectedService is not null &&
        !SelectedService.IsDriver &&
        SelectedService.StartMode == CoreServiceStartMode.Disabled;

    public bool CanDisableSelectedService =>
        _isElevatedHostAvailable &&
        !IsRefreshing &&
        SelectedService is not null &&
        !SelectedService.IsDriver &&
        SelectedService.StartMode != CoreServiceStartMode.Disabled &&
        SelectedService.StartMode != CoreServiceStartMode.Unknown;

    public async Task RefreshAsync()
    {
        if (IsRefreshing)
        {
            return;
        }

        IsRefreshing = true;
        StatusMessage = "Refreshing services...";

        try
        {
            var entries = await Task.Run(() => _catalogService.Collect(), CancellationToken.None);
            _items.Clear();
            foreach (var entry in entries)
            {
                _items.Add(entry);
            }

            RunningCount = _items.Count(static entry => entry.Status.Equals("Running", StringComparison.OrdinalIgnoreCase));
            DisabledCount = _items.Count(static entry => entry.StartMode == CoreServiceStartMode.Disabled);
            DriverCount = _items.Count(static entry => entry.IsDriver);
            Context = $"{_items.Count} entries | {RunningCount} running | {DisabledCount} disabled | {DriverCount} drivers";
            StatusMessage = _items.Count == 0 ? "No services were detected." : "Services loaded.";

            OnPropertyChanged(nameof(TotalCount));
            RefreshItemsView();
        }
        catch (Exception ex)
        {
            _appLogger.Log(LogLevel.Error, "Service inventory refresh failed", ex);
            StatusMessage = $"Service inventory failed: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private bool FilterItems(object obj)
    {
        if (obj is not WindowsServiceCatalogEntry entry)
        {
            return false;
        }

        var searchMatches = string.IsNullOrWhiteSpace(_searchText)
            || entry.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || entry.DisplayName.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || entry.DescriptionOrFallback.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || entry.Status.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || entry.StartType.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || entry.KindText.Contains(_searchText, StringComparison.OrdinalIgnoreCase);

        return searchMatches && MatchesSelectedFilter(entry);
    }

    private void OpenDocs()
    {
        if (SelectedService == null || string.IsNullOrWhiteSpace(SelectedService.DocsLink))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = SelectedService.DocsLink,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore shell launch failures.
        }
    }

    private async Task SetServiceStartModeAsync(CoreServiceStartMode startMode)
    {
        if (SelectedService == null || IsRefreshing)
        {
            return;
        }

        var actionText = startMode == CoreServiceStartMode.Disabled ? "Disable service" : "Enable service";
        var prompt = startMode == CoreServiceStartMode.Disabled
            ? $"Disable '{SelectedService.DisplayName}'? This can affect Windows or installed apps."
            : $"Set '{SelectedService.DisplayName}' to Manual start? Windows can still start it when required.";

        if (MessageBox.Show(prompt, actionText, MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            IsRefreshing = true;
            await _serviceManager.SetStartModeAsync(SelectedService.Name, startMode, CancellationToken.None);
            _appLogger.Log(LogLevel.Info, $"Service management: {SelectedService.Name} -> {startMode}");
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _appLogger.Log(LogLevel.Error, $"Service management failed for {SelectedService.Name}", ex);
            MessageBox.Show($"Failed to update the service start mode.\n{ex.Message}", "Services", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private void SetFilter(string? filterKey)
    {
        var filter = filterKey?.ToLowerInvariant() switch
        {
            "running" => ServiceListFilter.Running,
            "disabled" => ServiceListFilter.Disabled,
            "drivers" => ServiceListFilter.Drivers,
            _ => ServiceListFilter.All
        };

        if (_selectedFilter == filter)
        {
            return;
        }

        _selectedFilter = filter;
        OnPropertyChanged(nameof(IsAllFilterSelected));
        OnPropertyChanged(nameof(IsRunningFilterSelected));
        OnPropertyChanged(nameof(IsDisabledFilterSelected));
        OnPropertyChanged(nameof(IsDriversFilterSelected));
        RefreshItemsView();
    }

    private bool MatchesSelectedFilter(WindowsServiceCatalogEntry entry)
    {
        return _selectedFilter switch
        {
            ServiceListFilter.Running => entry.Status.Equals("Running", StringComparison.OrdinalIgnoreCase),
            ServiceListFilter.Disabled => entry.StartMode == CoreServiceStartMode.Disabled,
            ServiceListFilter.Drivers => entry.IsDriver,
            _ => true
        };
    }

    private void RefreshItemsView()
    {
        _itemsView.Refresh();

        if (SelectedService == null || !_itemsView.Cast<WindowsServiceCatalogEntry>().Contains(SelectedService))
        {
            SelectedService = _itemsView.Cast<WindowsServiceCatalogEntry>().FirstOrDefault();
        }

        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(VisibleCount));
        OnPropertyChanged(nameof(FilterStatusText));
    }

    private static string FormatValue(string? value) => string.IsNullOrWhiteSpace(value) ? "Not exposed by Windows" : value;

    private enum ServiceListFilter
    {
        All,
        Running,
        Disabled,
        Drivers
    }
}
