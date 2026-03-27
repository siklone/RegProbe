using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using RegProbe.App.Services;
using RegProbe.App.Services.TweakProviders;
using RegProbe.App.Utilities;
using RegProbe.Engine.Services;
using RegProbe.Infrastructure;

namespace RegProbe.App.ViewModels;

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private NavigationItem? _selectedNavigationItem;
    private ViewModelBase? _currentViewModel;
    private string _searchText = string.Empty;
    private readonly RelayCommand _clearSearchCommand;
    private readonly IRollbackStateStore _rollbackStore;
    private readonly IBusyService _busyService = new BusyService();
    private TweaksViewModel? _tweaksViewModel;
    private System.ComponentModel.PropertyChangedEventHandler? _tweaksPropertyChangedHandler;

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

        var bloatware = new BloatwareViewModel();
        var startup = new StartupViewModel();
        var tweaks = new TweaksViewModel(providers, _busyService, bloatware, startup);
        _tweaksViewModel = tweaks;

        var settings = new SettingsViewModel();
        var about = new AboutViewModel();

        NavigationItems = new ObservableCollection<NavigationItem>
        {
            new NavigationItem("tweaks", "Configuration", tweaks),
            new NavigationItem("settings", "Settings", settings),
            new NavigationItem("about", "About", about)
        };

        _tweaksPropertyChangedHandler = (_, _) => { };
        tweaks.PropertyChanged += _tweaksPropertyChangedHandler;

        SelectedNavigationItem = NavigationItems[0];

        _clearSearchCommand = new RelayCommand(_ => SearchText = string.Empty, _ => !string.IsNullOrEmpty(SearchText));

        NavigateToTweaksCommand = new RelayCommand(_ => NavigateToTab(0));
        NavigateToSettingsCommand = new RelayCommand(_ => NavigateToTab(1));
        NavigateToAboutCommand = new RelayCommand(_ => NavigateToTab(2));
        FocusSearchCommand = new RelayCommand(_ => OnFocusSearchRequested());
        ClearFiltersCommand = new RelayCommand(_ => OnClearFilters());

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

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    public IBusyService BusyService => _busyService;

    public string AppVersionLabel => AppInfo.VersionLabel;

    public string AppCopyrightLabel => AppInfo.CopyrightLabel;

    public ICommand RecoverPendingRollbacksCommand { get; }

    public ICommand DismissPendingRollbacksCommand { get; }

    public RelayCommand NavigateToTweaksCommand { get; }

    public RelayCommand NavigateToSettingsCommand { get; }

    public RelayCommand NavigateToAboutCommand { get; }

    public RelayCommand FocusSearchCommand { get; }

    public RelayCommand ClearFiltersCommand { get; }

    public event Action? FocusSearchRequested;

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
                    LogToFile("SelectedNavigationItem: CurrentViewModel set successfully");
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
        if (_tweaksViewModel == null || IsStartupScanActive)
        {
            return;
        }

        IsStartupScanActive = true;
        try
        {
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

    public void Dispose()
    {
        if (_tweaksViewModel != null && _tweaksPropertyChangedHandler != null)
        {
            _tweaksViewModel.PropertyChanged -= _tweaksPropertyChangedHandler;
        }

        foreach (var item in NavigationItems)
        {
            if (item.ViewModel is IDisposable disposable)
            {
                disposable.Dispose();
            }
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

    private void NavigateToTab(int index)
    {
        if (index >= 0 && index < NavigationItems.Count)
        {
            SelectedNavigationItem = NavigationItems[index];
        }
    }

    private void OnFocusSearchRequested()
    {
        NavigateToTab(0);
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
        if (_tweaksViewModel == null)
        {
            return;
        }

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
                    var tweakVm = _tweaksViewModel.Tweaks.FirstOrDefault(t => t.Id == entry.TweakId);
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
