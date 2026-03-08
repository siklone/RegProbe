using System.Collections.ObjectModel;
using System.Windows.Input;
using WindowsOptimizer.App.Models;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.Utilities;

namespace WindowsOptimizer.App.ViewModels;

public sealed class StartupViewModel : ViewModelBase
{
    private readonly StartupService _startupService;
    private string _statusMessage = "Loading startup items...";
    private bool _isLoading;
    private bool _isProcessing;

    public StartupViewModel()
    {
        _startupService = new StartupService();
        
        RefreshCommand = new RelayCommand(async _ => await LoadStartupItemsAsync());
        
        _ = LoadStartupItemsAsync();
    }

    public string Title => "Startup Manager";

    public ObservableCollection<StartupItemViewModel> StartupItems { get; } = new();

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        set => SetProperty(ref _isProcessing, value);
    }

    public ICommand RefreshCommand { get; }

    private async Task LoadStartupItemsAsync()
    {
        IsLoading = true;
        StatusMessage = "Scanning startup items...";
        StartupItems.Clear();

        try
        {
            var items = await _startupService.GetAllStartupItemsAsync();

            foreach (var item in items.OrderBy(i => i.IsEnabled ? 0 : 1).ThenBy(i => i.Name))
            {
                var vm = new StartupItemViewModel(item, this);
                StartupItems.Add(vm);
            }

            StatusMessage = $"Found {StartupItems.Count} startup items ({StartupItems.Count(i => i.IsEnabled)} enabled)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading startup items: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ToggleStartupItemAsync(StartupItemViewModel itemVm)
    {
        IsProcessing = true;
        StatusMessage = itemVm.IsEnabled ? "Disabling..." : "Enabling...";

        try
        {
            bool success;
            
            if (itemVm.IsEnabled)
            {
                success = await _startupService.DisableStartupItemAsync(itemVm.Model);
            }
            else
            {
                success = await _startupService.EnableStartupItemAsync(itemVm.Model);
            }

            if (success)
            {
                // Reload to reflect changes
                await LoadStartupItemsAsync();
                StatusMessage = itemVm.IsEnabled ? "Item disabled successfully" : "Item enabled successfully";
            }
            else
            {
                StatusMessage = "Failed to change startup item";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }
}

public class StartupItemViewModel : ViewModelBase
{
    private readonly StartupItem _model;
    private readonly StartupViewModel _parentVm;

    public StartupItemViewModel(StartupItem model, StartupViewModel parentVm)
    {
        _model = model;
        _parentVm = parentVm;

        ToggleCommand = new RelayCommand(
            async _ => await _parentVm.ToggleStartupItemAsync(this),
            _ => !_parentVm.IsProcessing
        );
    }

    public StartupItem Model => _model;

    public string Name => _model.Name;
    public string Command => _model.Command;
    public string Publisher => _model.Publisher;
    public string Location => FormatLocation(_model.Location);
    public bool IsEnabled => _model.IsEnabled;
    
    public string ImpactText => _model.Impact switch
    {
        StartupImpact.Low => "Low",
        StartupImpact.Medium => "Medium",
        StartupImpact.High => "High",
        _ => "Unknown"
    };

    public string ImpactColor => _model.Impact switch
    {
        StartupImpact.Low => "#4CAF50",      // Green
        StartupImpact.Medium => "#FF9800",   // Orange
        StartupImpact.High => "#F44336",     // Red
        _ => "#9E9E9E"                       // Gray
    };

    public string StatusText => IsEnabled ? "Enabled" : "Disabled";
    public string StatusColor => IsEnabled ? "#4CAF50" : "#9E9E9E";
    
    public string ButtonText => IsEnabled ? "Disable" : "Enable";

    public ICommand ToggleCommand { get; }

    private string FormatLocation(StartupLocation location) => location switch
    {
        StartupLocation.RegistryCurrentUser => "Registry (User)",
        StartupLocation.RegistryLocalMachine => "Registry (System)",
        StartupLocation.StartupFolderUser => "Startup Folder (User)",
        StartupLocation.StartupFolderCommon => "Startup Folder (Common)",
        StartupLocation.TaskScheduler => "Task Scheduler",
        _ => "Unknown"
    };
}
