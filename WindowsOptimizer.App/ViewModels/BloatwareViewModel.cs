using System.Collections.ObjectModel;
using System.Windows.Input;
using WindowsOptimizer.App.Models;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.Utilities;

namespace WindowsOptimizer.App.ViewModels;

public sealed class BloatwareViewModel : ViewModelBase
{
    private readonly BloatwareService _bloatwareService;
    private string _statusMessage = "Loading installed apps...";
    private bool _isLoading;
    private bool _isUninstalling;
    private string _searchText = string.Empty;

    public BloatwareViewModel()
    {
        _bloatwareService = new BloatwareService();
        
        RefreshCommand = new RelayCommand(async _ => await LoadAppsAsync());
        
        _ = LoadAppsAsync();
    }

    public string Title => "Bloatware Remover";

    public ObservableCollection<AppxPackageViewModel> Apps { get; } = new();

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

    public bool IsUninstalling
    {
        get => _isUninstalling;
        set => SetProperty(ref _isUninstalling, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                FilterApps();
            }
        }
    }

    public ICommand RefreshCommand { get; }

    private async Task LoadAppsAsync()
    {
        IsLoading = true;
        StatusMessage = "Scanning installed UWP apps...";
        Apps.Clear();

        try
        {
            var apps = await _bloatwareService.GetInstalledAppsAsync();

            foreach (var app in apps)
            {
                var safetyLevel = _bloatwareService.GetSafetyLevel(app.PackageFullName);
                var vm = new AppxPackageViewModel(app, safetyLevel, this);
                Apps.Add(vm);
            }

            StatusMessage = $"Found {Apps.Count} installed apps";
            FilterApps();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading apps: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void FilterApps()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            foreach (var app in Apps)
            {
                app.IsVisible = true;
            }
        }
        else
        {
            var searchLower = SearchText.ToLower();
            foreach (var app in Apps)
            {
                app.IsVisible = app.DisplayName.ToLower().Contains(searchLower) ||
                                app.Publisher.ToLower().Contains(searchLower);
            }
        }

        var visibleCount = Apps.Count(a => a.IsVisible);
        StatusMessage = $"Showing {visibleCount} of {Apps.Count} apps";
    }

    public async Task UninstallAppAsync(AppxPackageViewModel appVm)
    {
        IsUninstalling = true;
        StatusMessage = $"Uninstalling {appVm.DisplayName}...";

        try
        {
            var result = await _bloatwareService.UninstallAppAsync(appVm.PackageFullName);

            if (result.Success)
            {
                Apps.Remove(appVm);
                StatusMessage = $"✓ Successfully uninstalled {appVm.DisplayName}";
            }
            else
            {
                StatusMessage = $"✗ Failed: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Error: {ex.Message}";
        }
        finally
        {
            IsUninstalling = false;
        }
    }
}

public class AppxPackageViewModel : ViewModelBase
{
    private readonly AppxPackageInfo _model;
    private readonly PackageSafetyLevel _safetyLevel;
    private readonly BloatwareViewModel _parentVm;
    private bool _isVisible = true;

    public AppxPackageViewModel(AppxPackageInfo model, PackageSafetyLevel safetyLevel, BloatwareViewModel parentVm)
    {
        _model = model;
        _safetyLevel = safetyLevel;
        _parentVm = parentVm;

        UninstallCommand = new RelayCommand(
            async _ => await _parentVm.UninstallAppAsync(this),
            _ => !_parentVm.IsUninstalling && _safetyLevel != PackageSafetyLevel.Critical
        );
    }

    public string PackageFullName => _model.PackageFullName;
    public string DisplayName => _model.DisplayName;
    public string Publisher => _model.Publisher;
    public string Version => _model.Version;
    public string SizeFormatted => FormatBytes(_model.SizeInBytes ?? 0);

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public string SafetyText => _safetyLevel switch
    {
        PackageSafetyLevel.Safe => "Safe",
        PackageSafetyLevel.Caution => "Caution",
        PackageSafetyLevel.Critical => "Protected",
        _ => "Unknown"
    };

    public string SafetyColor => _safetyLevel switch
    {
        PackageSafetyLevel.Safe => "#4CAF50",      // Green
        PackageSafetyLevel.Caution => "#FF9800",   // Orange
        PackageSafetyLevel.Critical => "#F44336",  // Red
        _ => "#9E9E9E"
    };

    public bool CanUninstall => _safetyLevel != PackageSafetyLevel.Critical;

    public ICommand UninstallCommand { get; }

    private string FormatBytes(long bytes)
    {
        if (bytes == 0) return "Unknown";
        
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
