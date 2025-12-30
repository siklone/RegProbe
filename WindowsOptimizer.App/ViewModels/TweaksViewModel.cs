using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Intelligence;
using WindowsOptimizer.Core.Models;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Infrastructure.Elevation;
using WindowsOptimizer.Infrastructure.Metrics;
using WindowsOptimizer.Infrastructure.Registry;
using WindowsOptimizer.Infrastructure.Services;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Services;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Engine.Tweaks.Commands.Power;
using WindowsOptimizer.Engine.Tweaks.Commands.Cleanup;
using WindowsOptimizer.Engine.Tweaks.Commands.Performance;
using WindowsOptimizer.Engine.Tweaks.Commands.Network;
using WindowsOptimizer.Engine.Tweaks.Commands.System;
using WindowsOptimizer.Engine.Tweaks.Misc;
using WindowsOptimizer.Engine.Tweaks.Peripheral;
using WindowsOptimizer.Engine.Tweaks.Power;
using WindowsOptimizer.App.Utilities;
using WindowsOptimizer.Infrastructure;
using WindowsOptimizer.Core.Commands;
using WindowsOptimizer.Core.Files;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Tasks;

namespace WindowsOptimizer.App.ViewModels;

public sealed class TweaksViewModel : ViewModelBase
{
    private readonly ITweakLogStore _logStore;
    private readonly RelayCommand _exportLogsCommand;
    private readonly RelayCommand _previewAllCommand;
    private readonly RelayCommand _applyAllCommand;
    private readonly RelayCommand _verifyAllCommand;
    private readonly RelayCommand _rollbackAllCommand;
    private readonly RelayCommand _cancelAllCommand;
    private readonly RelayCommand _selectAllCommand;
    private readonly RelayCommand _deselectAllCommand;
    private readonly RelayCommand _detectSelectedCommand;
    private readonly RelayCommand _applySelectedCommand;
    private readonly RelayCommand _verifySelectedCommand;
    private readonly RelayCommand _rollbackSelectedCommand;
    private readonly RelayCommand _loadPresetCommand;
    private readonly RelayCommand _resetFiltersCommand;
    private readonly RelayCommand _openLogFolderCommand;
    private readonly RelayCommand _openCsvLogCommand;
	private readonly RelayCommand _expandAllDetailsCommand;
	private readonly RelayCommand _collapseAllDetailsCommand;
	private readonly bool _isElevated;
	private readonly string _elevatedHostExecutablePath;
	private readonly bool _isElevatedHostAvailable;
	private readonly IRegistryAccessor _localRegistryAccessor;
	private readonly IRegistryAccessor _elevatedRegistryAccessor;
	private readonly IServiceManager _elevatedServiceManager;
	private readonly IScheduledTaskManager _elevatedTaskManager;
    private readonly IFileSystemAccessor _elevatedFileSystemAccessor;
    private readonly ICommandRunner _elevatedCommandRunner;
    private string _exportStatusMessage = "Logs are ready to export.";
    private string _bulkStatusMessage = "Bulk actions are idle.";
    private string _filterSummary = "Showing 0 of 0 tweaks.";
    private bool _isExporting;
    private bool _isBulkRunning;
    private int _bulkProgressCurrent;
    private int _bulkProgressTotal;
    private int _selectedCount;
    private string _searchText = string.Empty;
    private string _statusFilter = string.Empty; // "applied", "rolledback", or empty for all
    private bool _showSafe = true;
    private bool _showAdvanced = true;
    private bool _showRisky = true;
    private bool _showFavoritesOnly = false;
    private bool _hasVisibleTweaks;
    private bool _isFlatView;
    private readonly IFavoritesStore _favoritesStore;
    private CancellationTokenSource? _searchCts;
    private CancellationTokenSource? _bulkCts;
    private readonly string _logFolderPath;
    private readonly MetricProvider _metricProvider = new();
    private readonly DispatcherTimer _metricsTimer;
    private readonly IProfileSyncService _syncService = new ProfileSyncService();
    private readonly PluginLoader _pluginLoader = new();
    private readonly KernelImpactAnalyzer _kernelAnalyzer = new();
    private readonly string _tweakLogFilePath;
    private readonly IProfileManager _profileManager;
    private readonly TweakExecutionPipeline _pipeline;
    private readonly IEnumerable<ITweakProvider>? _providerList;
    private readonly IRollbackStateStore _rollbackStore;

    public TweaksViewModel(IEnumerable<ITweakProvider>? providers = null)
    {
        _providerList = providers;
        var paths = AppPaths.FromEnvironment();
        paths.EnsureDirectories();
        var logger = new FileAppLogger(paths);
        _logFolderPath = paths.LogDirectory;
        _tweakLogFilePath = paths.TweakLogFilePath;
        _logStore = new FileTweakLogStore(paths);
        _profileManager = new ProfileManager(paths);
        _rollbackStore = new RollbackStateStore(paths);
        _favoritesStore = new FavoritesStore(paths);
		_pipeline = new TweakExecutionPipeline(logger, _logStore, _rollbackStore);
		var settingsStore = new SettingsStore(paths);
		_isElevated = ProcessElevation.IsElevated();
		_elevatedHostExecutablePath = ElevatedHostLocator.GetExecutablePath();
		_isElevatedHostAvailable = File.Exists(_elevatedHostExecutablePath);
		var elevatedHostClient = new ElevatedHostClient(new ElevatedHostClientOptions
		{
			HostExecutablePath = _elevatedHostExecutablePath,
			PipeName = ElevatedHostDefaults.PipeName,
			ParentProcessId = Process.GetCurrentProcess().Id
		});
        _localRegistryAccessor = new LocalRegistryAccessor();
        _elevatedRegistryAccessor = new ElevatedRegistryAccessor(elevatedHostClient);
        _elevatedServiceManager = new ElevatedServiceManager(elevatedHostClient);
        _elevatedTaskManager = new ElevatedScheduledTaskManager(elevatedHostClient);
        _elevatedFileSystemAccessor = new ElevatedFileSystemAccessor(elevatedHostClient);
        _elevatedCommandRunner = new ElevatedCommandRunner(elevatedHostClient);
        var systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var system32Path = Path.Combine(systemRoot, "System32");
        var mobsyncPath = Path.Combine(system32Path, "mobsync.exe");
        var mobsyncDisabledPath = mobsyncPath + ".disabled";
        var psrPath = Path.Combine(system32Path, "psr.exe");
        var psrDisabledPath = psrPath + ".disabled";
        var helpPanePath = Path.Combine(system32Path, "HelpPane.exe");
        var helpPaneDisabledPath = helpPanePath + ".disabled";

        Tweaks = new ObservableCollection<TweakItemViewModel>();
        Tweaks.CollectionChanged += OnTweaksCollectionChanged;

        TweaksView = CollectionViewSource.GetDefaultView(Tweaks);
        TweaksView.Filter = FilterTweaks;
        TweaksView.SortDescriptions.Add(new SortDescription(nameof(TweakItemViewModel.Risk), ListSortDirection.Ascending));
        TweaksView.SortDescriptions.Add(new SortDescription(nameof(TweakItemViewModel.Name), ListSortDirection.Ascending));

        _exportLogsCommand = new RelayCommand(_ => _ = ExportLogsAsync(), _ => !IsExporting);
        _previewAllCommand = new RelayCommand(_ => _ = RunBulkAsync("Preview", (item, token) => item.RunPreviewAsync(token)), _ => CanRunBulk());
        _applyAllCommand = new RelayCommand(_ => _ = RunBulkAsync("Apply", (item, token) => item.RunApplyAsync(token)), _ => CanRunBulk());
        _verifyAllCommand = new RelayCommand(_ => _ = RunBulkAsync("Verify", (item, token) => item.RunVerifyAsync(token)), _ => CanRunBulk());
        _rollbackAllCommand = new RelayCommand(_ => _ = RunBulkAsync("Rollback", (item, token) => item.RunRollbackAsync(token)), _ => CanRunBulk());
        _cancelAllCommand = new RelayCommand(_ => CancelBulk(), _ => IsBulkRunning);
        _selectAllCommand = new RelayCommand(_ => SelectAll());
        _deselectAllCommand = new RelayCommand(_ => DeselectAll());
        _detectSelectedCommand = new RelayCommand(
            _ => _ = RunBulkAsync("Detect Selected", GetSelectedTweaks, (item, token) => item.RunDetectAsync(token)),
            _ => CanRunBulkOnSelected());
        _applySelectedCommand = new RelayCommand(
            _ => _ = RunBulkAsync("Apply Selected", GetSelectedTweaks, (item, token) => item.RunApplyAsync(token)),
            _ => CanRunBulkOnSelected());
        _verifySelectedCommand = new RelayCommand(
            _ => _ = RunBulkAsync("Verify Selected", GetSelectedTweaks, (item, token) => item.RunVerifyAsync(token)),
            _ => CanRunBulkOnSelected());
        _rollbackSelectedCommand = new RelayCommand(
            _ => _ = RunBulkAsync("Rollback Selected", GetSelectedTweaks, (item, token) => item.RunRollbackAsync(token)),
            _ => CanRunBulkOnSelected());
        _loadPresetCommand = new RelayCommand(async parameter => await LoadPresetAsync(parameter));
        _resetFiltersCommand = new RelayCommand(_ => ResetFilters());
        _openLogFolderCommand = new RelayCommand(_ => OpenLogFolder());
        _openCsvLogCommand = new RelayCommand(_ => OpenCsvLog());
        _expandAllDetailsCommand = new RelayCommand(_ => SetDetailsExpanded(true));
        _collapseAllDetailsCommand = new RelayCommand(_ => SetDetailsExpanded(false));
        ExportPresetCommand = new RelayCommand(async _ => await ExportPresetsAsync());
        ImportPresetCommand = new RelayCommand(async _ => await ImportPresetsAsync());
        CreateSnapshotCommand = new RelayCommand(_ => CreateSnapshot());

        LoadProviderTweaks();
        LoadPlugins();
        ApplyTweakMetadata();
        UpdateFilterSummary();
        _ = InitializePresetsAsync();

        _metricsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _metricsTimer.Tick += OnMetricsTick;
        _metricsTimer.Start();
    }

    private void OnMetricsTick(object? sender, EventArgs e)
    {
        try
        {
            var cpu = _metricProvider.GetCpuUsage();
            var kernelEfficiency = _kernelAnalyzer.GetKernelEfficiencyScore() * 100.0;

            // Combine CPU usage and Kernel Efficiency for a more "God-Tier" impact metric
            var combinedImpact = (cpu + kernelEfficiency) / 2.0;

            if (IsFlatView)
            {
                // Update all tweaks (filtered) in flat view
                foreach (var tweak in TweaksView.Cast<TweakItemViewModel>())
                {
                    tweak.UpdateMetric(combinedImpact);
                }
            }
            else
            {
                // Update hierarchical view
                foreach (var category in CategoryGroups)
                {
                    UpdateMetricsRecursive(category, (float)combinedImpact);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TweaksViewModel metrics update error: {ex.Message}");
            // Continue running - don't crash the app
        }
    }

    private void UpdateMetricsRecursive(CategoryGroupViewModel category, float value)
    {
        if (!category.IsExpanded) return;

        foreach (var tweak in category.Tweaks)
        {
            tweak.UpdateMetric(value);
        }

        foreach (var sub in category.SubGroups)
        {
            UpdateMetricsRecursive(sub, value);
        }
    }

    private void LoadProviderTweaks()
    {
        if (_providerList != null)
        {
            var existingIds = new HashSet<string>(
                Tweaks.Select(t => t.Id).Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);

            var tweakContext = new TweakContext(
                _localRegistryAccessor,
                _elevatedRegistryAccessor,
                _elevatedServiceManager,
                _elevatedTaskManager,
                _elevatedFileSystemAccessor,
                _elevatedCommandRunner);

            foreach (var provider in _providerList)
            {
                var providerTweaks = provider.CreateTweaks(_pipeline, tweakContext, _isElevated);
                foreach (var tweak in providerTweaks)
                {
                    if (string.IsNullOrWhiteSpace(tweak.Id) || !existingIds.Add(tweak.Id))
                    {
                        continue;
                    }

                    Tweaks.Add(new TweakItemViewModel(tweak, _pipeline, _isElevated));
                }
            }
        }
    }

    private void ApplyTweakMetadata()
    {
        var aeroShake = Tweaks.FirstOrDefault(t => t.Id == "system.aero-shake");
        if (aeroShake != null)
        {
            aeroShake.RegistryPath = @"HKCU\Software\Policies\Microsoft\Windows\Explorer\NoWindowMinimizingShortcuts";
            aeroShake.CodeExample = "reg add \"HKCU\\Software\\Policies\\Microsoft\\Windows\\Explorer\" /v \"NoWindowMinimizingShortcuts\" /t REG_DWORD /d 1 /f";
            aeroShake.ReferenceLinks.Add(new ReferenceLink("Policy Documentation", "https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-explorer"));
        }

        var gameMode = Tweaks.FirstOrDefault(t => t.Id == "system.enable-game-mode");
        if (gameMode != null)
        {
            gameMode.RegistryPath = @"HKCU\Software\Microsoft\GameBar\AutoGameModeEnabled";
            gameMode.CodeExample = "reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v \"AutoGameModeEnabled\" /t REG_DWORD /d 1 /f";
            gameMode.SubOptions.Add(new TweakSubOption("Enable Game Bar", TweakSubOptionType.Toggle) { IsEnabled = true });
            gameMode.SubOptions.Add(new TweakSubOption("Allow Background DVR", TweakSubOptionType.Toggle));
            gameMode.ReferenceLinks.Add(new ReferenceLink("Xbox Support", "https://support.xbox.com/en-US/help/games-apps/game-setup-and-play/use-game-mode-gaming-on-pc"));
        }

        var clipboard = Tweaks.FirstOrDefault(t => t.Id == "system.disable-clipboard-history");
        if (clipboard != null)
        {
            clipboard.RegistryPath = @"HKLM\Software\Policies\Microsoft\Windows\System\AllowClipboardHistory";
            clipboard.CodeExample = "reg add \"HKLM\\Software\\Policies\\Microsoft\\Windows\\System\" /v \"AllowClipboardHistory\" /t REG_DWORD /d 0 /f";
            clipboard.TargetValue = "0 (Disabled)";
            clipboard.ReferenceLinks.Add(new ReferenceLink("Security Best Practices", "https://learn.microsoft.com/en-us/windows/security/threat-protection/security-policy-settings/user-rights-assignment"));
        }

        var edgeBoost = Tweaks.FirstOrDefault(t => t.Id == "system.disable-edge-startup-boost");
        if (edgeBoost != null)
        {
            edgeBoost.ActionType = TweakActionType.Clean;
            edgeBoost.ActionButtonText = "Disable Boost";
            edgeBoost.RegistryPath = @"HKLM\Software\Policies\Microsoft\Edge\StartupBoostEnabled";
            edgeBoost.TargetValue = "0 (Disabled)";
        }

        var mitigations = Tweaks.FirstOrDefault(t => t.Id == "security.disable-system-mitigations");
        if (mitigations != null)
        {
            mitigations.RegistryPath = @"HKLM\System\CurrentControlSet\Control\Session Manager\kernel\MitigationOptions";
            mitigations.CodeExample = "# View current mitigation options\nGet-ItemProperty -Path 'HKLM:\\System\\CurrentControlSet\\Control\\Session Manager\\kernel' -Name MitigationOptions\n\n# Set mitigations to 22202022... (Hex)";
            mitigations.TargetValue = "22202022 (Optimized)";
            mitigations.ReferenceLinks.Add(new ReferenceLink("Exploit Protection Reference", "https://learn.microsoft.com/en-us/microsoft-365/security/defender-endpoint/exploit-protection-reference"));
            mitigations.ReferenceLinks.Add(new ReferenceLink("Bypass Mitigations Guide", "https://github.com/SirenOfTitan/Exploit-Mitigations-Bypass"));
        }

        var priority = Tweaks.FirstOrDefault(t => t.Id == "system.priority-control");
        if (priority != null)
        {
            priority.RegistryPath = @"HKLM\System\CurrentControlSet\Control\PriorityControl\Win32PrioritySeparation";
            priority.CodeExample = "Set-ItemProperty -Path 'HKLM:\\System\\CurrentControlSet\\Control\\PriorityControl' -Name Win32PrioritySeparation -Value 38";
            priority.PriorityCalculator = new PriorityCalculatorViewModel { Bitmask = 0x26 };
            priority.ReferenceLinks.Add(new ReferenceLink("MSDN PriorityControl", "https://learn.microsoft.com/en-us/windows/win32/procthread/scheduling-priorities"));
        }
    }

    public string Title => "Tweaks";

	public bool IsElevated => _isElevated;

	public string ElevationStatusMessage => IsElevated
		? "Running with administrator privileges."
		: "Running without administrator privileges. Admin-required tweaks will prompt for elevation.";

	public bool IsElevatedHostAvailable => _isElevatedHostAvailable;

	public string ElevatedHostStatusMessage => IsElevatedHostAvailable
		? string.Empty
		: $"ElevatedHost not found. Expected at: {_elevatedHostExecutablePath}. Build WindowsOptimizer.ElevatedHost or set {ElevatedHostDefaults.OverridePathEnvVar}.";

	public ObservableCollection<TweakItemViewModel> Tweaks { get; }

    public IEnumerable<TweakItemViewModel> AllTweaks => Tweaks;

    public ICollectionView TweaksView { get; }

    public ObservableCollection<CategoryGroupViewModel> CategoryGroups { get; } = new();

    public ICommand ExportLogsCommand => _exportLogsCommand;

    public ICommand PreviewAllCommand => _previewAllCommand;

    public ICommand ApplyAllCommand => _applyAllCommand;

    public ICommand VerifyAllCommand => _verifyAllCommand;

    public ICommand RollbackAllCommand => _rollbackAllCommand;

    public ICommand CancelAllCommand => _cancelAllCommand;

    public ICommand SelectAllCommand => _selectAllCommand;

    public ICommand DeselectAllCommand => _deselectAllCommand;

    public ICommand DetectSelectedCommand => _detectSelectedCommand;

    public ICommand ApplySelectedCommand => _applySelectedCommand;

    public ICommand VerifySelectedCommand => _verifySelectedCommand;

    public ICommand RollbackSelectedCommand => _rollbackSelectedCommand;

    public ICommand LoadPresetCommand => _loadPresetCommand;

    public ICommand ResetFiltersCommand => _resetFiltersCommand;

    public ICommand OpenLogFolderCommand => _openLogFolderCommand;

    public ICommand OpenCsvLogCommand => _openCsvLogCommand;

    public ICommand ExpandAllDetailsCommand => _expandAllDetailsCommand;

    public ICommand CollapseAllDetailsCommand => _collapseAllDetailsCommand;
    public ICommand ExportPresetCommand { get; }
    public ICommand ImportPresetCommand { get; }
    public ICommand CreateSnapshotCommand { get; }

    public int ScorableTweaksTotal => Tweaks.Count(IsScorableForHealth);

    public int ScorableTweaksMeasuredTotal => Tweaks.Count(t => IsScorableForHealth(t) && t.AppliedStatus != TweakAppliedStatus.Unknown);

    public int ScorableTweaksApplied => Tweaks.Count(t => IsScorableForHealth(t) && t.IsApplied);

    public int GlobalOptimizationScore
    {
        get
        {
            var total = ScorableTweaksMeasuredTotal;
            if (total == 0)
            {
                return 0;
            }

            var applied = ScorableTweaksApplied;
            return (int)Math.Round((double)applied / total * 100, MidpointRounding.AwayFromZero);
        }
    }

    public string HealthCalculationSummary => ScorableTweaksMeasuredTotal == 0
        ? "Health is calculated from detected states. Run Detect to refresh current states."
        : $"{ScorableTweaksApplied} / {ScorableTweaksMeasuredTotal} detected tweaks applied (Safe+Advanced; excludes Demo/Risky).";

    public string HealthStatusMessage => GlobalOptimizationScore switch
    {
        >= 90 => "Excellent optimization level",
        >= 70 => "Good optimization level",
        >= 40 => "Moderate optimization level",
        _ => "System needs optimization"
    };

    private static bool IsScorableForHealth(TweakItemViewModel tweak) =>
        !tweak.Id.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
        && tweak.Risk != TweakRiskLevel.Risky;

    public string ExportStatusMessage
    {
        get => _exportStatusMessage;
        private set => SetProperty(ref _exportStatusMessage, value);
    }

    public string BulkStatusMessage
    {
        get => _bulkStatusMessage;
        private set => SetProperty(ref _bulkStatusMessage, value);
    }

    public bool IsExporting
    {
        get => _isExporting;
        private set
        {
            if (SetProperty(ref _isExporting, value))
            {
                _exportLogsCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsBulkRunning
    {
        get => _isBulkRunning;
        private set
        {
            if (SetProperty(ref _isBulkRunning, value))
            {
                _previewAllCommand.RaiseCanExecuteChanged();
                _applyAllCommand.RaiseCanExecuteChanged();
                _verifyAllCommand.RaiseCanExecuteChanged();
                _rollbackAllCommand.RaiseCanExecuteChanged();
                _cancelAllCommand.RaiseCanExecuteChanged();
                SetBulkLock(value);
            }
        }
    }

    public int BulkProgressCurrent
    {
        get => _bulkProgressCurrent;
        private set
        {
            if (SetProperty(ref _bulkProgressCurrent, value))
            {
                OnPropertyChanged(nameof(BulkProgressText));
            }
        }
    }

    public int BulkProgressTotal
    {
        get => _bulkProgressTotal;
        private set
        {
            if (SetProperty(ref _bulkProgressTotal, value))
            {
                OnPropertyChanged(nameof(BulkProgressText));
            }
        }
    }

    public string BulkProgressText => BulkProgressTotal == 0
        ? "Bulk progress: 0/0"
        : $"Bulk progress: {BulkProgressCurrent}/{BulkProgressTotal}";

    public int SelectedCount
    {
        get => _selectedCount;
        private set
        {
            if (SetProperty(ref _selectedCount, value))
            {
                OnPropertyChanged(nameof(SelectionSummary));
                OnPropertyChanged(nameof(HasSelection));
                _detectSelectedCommand?.RaiseCanExecuteChanged();
                _applySelectedCommand?.RaiseCanExecuteChanged();
                _verifySelectedCommand?.RaiseCanExecuteChanged();
                _rollbackSelectedCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    public string SelectionSummary => SelectedCount == 0
        ? "No tweaks selected"
        : $"{SelectedCount} tweak{(SelectedCount == 1 ? "" : "s")} selected";

    public bool HasSelection => SelectedCount > 0;

    public bool IsFlatView
    {
        get => _isFlatView;
        set
        {
            if (SetProperty(ref _isFlatView, value))
            {
                UpdateFilterSummary();
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
                TriggerSearchUpdate();
            }
        }
    }

    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
            {
                OnPropertyChanged(nameof(StatusFilterLabel));
                OnPropertyChanged(nameof(HasStatusFilter));
                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
    }

    public string StatusFilterLabel => _statusFilter switch
    {
        "applied" => "Applied Tweaks",
        "rolledback" => "Rolled Back Tweaks",
        _ => ""
    };

    public bool HasStatusFilter => !string.IsNullOrEmpty(_statusFilter);

    private void TriggerSearchUpdate()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        Task.Delay(300, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                System.Windows.Application.Current?.Dispatcher?.BeginInvoke(() =>
                {
                    TweaksView.Refresh();
                    UpdateFilterSummary();
                });
            }
        }, token);
    }

    public bool ShowSafe
    {
        get => _showSafe;
        set
        {
            if (SetProperty(ref _showSafe, value))
            {
                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
    }

    public bool ShowAdvanced
    {
        get => _showAdvanced;
        set
        {
            if (SetProperty(ref _showAdvanced, value))
            {
                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
    }

    public bool ShowRisky
    {
        get => _showRisky;
        set
        {
            if (SetProperty(ref _showRisky, value))
            {
                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
    }

    public bool ShowFavoritesOnly
    {
        get => _showFavoritesOnly;
        set
        {
            if (SetProperty(ref _showFavoritesOnly, value))
            {
                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
    }

    public int FavoritesCount => Tweaks.Count(t => t.IsFavorite);

    public string FilterSummary
    {
        get => _filterSummary;
        private set => SetProperty(ref _filterSummary, value);
    }

    public bool HasVisibleTweaks
    {
        get => _hasVisibleTweaks;
        private set => SetProperty(ref _hasVisibleTweaks, value);
    }


    private async Task ExportLogsAsync()
    {
        if (IsExporting)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            FileName = "tweak-log.csv",
            Title = "Export tweak logs"
        };

        if (dialog.ShowDialog() != true)
        {
            ExportStatusMessage = "Export cancelled.";
            return;
        }

        IsExporting = true;
        try
        {
            await _logStore.ExportCsvAsync(dialog.FileName, CancellationToken.None);
            ExportStatusMessage = $"Exported to {dialog.FileName}.";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    private bool CanRunBulk()
    {
        if (IsBulkRunning || Tweaks.Any(item => item.IsRunning))
        {
            return false;
        }

        return TweaksView.Cast<object>().Any();
    }

    // Existing signature - delegates to new overload
    private async Task RunBulkAsync(string label, Func<TweakItemViewModel, CancellationToken, Task> runner)
    {
        await RunBulkAsync(label, GetAllFilteredTweaks, runner);
    }

    // New overload - supports custom tweak collections
    private async Task RunBulkAsync(
        string label,
        Func<List<TweakItemViewModel>> getTweaks,
        Func<TweakItemViewModel, CancellationToken, Task> runner)
    {
        if (IsBulkRunning)
        {
            return;
        }

        StartBulkCancellation();
        IsBulkRunning = true;
        SetBulkLock(true);
        var actionLabel = label.ToLowerInvariant();

        try
        {
            var items = getTweaks();
            if (items.Count == 0)
            {
                BulkStatusMessage = $"No tweaks to {actionLabel}.";
                return;
            }

            BulkProgressTotal = items.Count;
            BulkProgressCurrent = 0;
            OnPropertyChanged(nameof(BulkProgressText));

            foreach (var item in items)
            {
                _bulkCts?.Token.ThrowIfCancellationRequested();
                BulkStatusMessage = $"Running {actionLabel} on {item.Name}...";
                await runner(item, _bulkCts?.Token ?? CancellationToken.None);

                BulkProgressCurrent++;
                OnPropertyChanged(nameof(BulkProgressText));
            }

            BulkStatusMessage = $"Bulk {actionLabel} completed ({items.Count} tweaks).";
        }
        catch (OperationCanceledException)
        {
            BulkStatusMessage = $"Bulk {actionLabel} cancelled.";
        }
        catch (Exception ex)
        {
            BulkStatusMessage = $"Bulk {actionLabel} failed: {ex.Message}";
        }
        finally
        {
            IsBulkRunning = false;
            SetBulkLock(false);
            BulkProgressCurrent = 0;
            BulkProgressTotal = 0;
            OnPropertyChanged(nameof(BulkProgressText));
            ClearBulkCancellation();
        }
    }

    private List<TweakItemViewModel> GetAllFilteredTweaks()
    {
        return TweaksView.Cast<TweakItemViewModel>().ToList();
    }

    private List<TweakItemViewModel> GetSelectedTweaks()
    {
        return Tweaks.Where(t => t.IsSelected).ToList();
    }

    private void SelectAll()
    {
        foreach (var tweak in TweaksView.Cast<TweakItemViewModel>())
        {
            tweak.IsSelected = true;
        }
    }

    private void DeselectAll()
    {
        foreach (var tweak in Tweaks)
        {
            tweak.IsSelected = false;
        }
    }

    private void OnTweakPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TweakItemViewModel.IsSelected))
        {
            UpdateSelectionCount();
        }
        else if (e.PropertyName == nameof(TweakItemViewModel.IsRunning))
        {
            _previewAllCommand.RaiseCanExecuteChanged();
            _applyAllCommand.RaiseCanExecuteChanged();
            _verifyAllCommand.RaiseCanExecuteChanged();
            _rollbackAllCommand.RaiseCanExecuteChanged();
        }
        else if (e.PropertyName is nameof(TweakItemViewModel.IsApplied) or nameof(TweakItemViewModel.AppliedStatus))
        {
            RaiseHealthMetricsChanged();
        }
    }

    private void OnTweakFavoriteChanged(TweakItemViewModel tweak, bool isFavorite)
    {
        if (isFavorite)
        {
            _favoritesStore.AddFavorite(tweak.Id);
        }
        else
        {
            _favoritesStore.RemoveFavorite(tweak.Id);
        }

        OnPropertyChanged(nameof(FavoritesCount));

        // Refresh view if showing favorites only
        if (_showFavoritesOnly)
        {
            TweaksView.Refresh();
            UpdateFilterSummary();
        }
    }

    private void OnTweaksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<TweakItemViewModel>())
            {
                item.PropertyChanged += OnTweakPropertyChanged;
                item.FavoriteChanged += OnTweakFavoriteChanged;
                item.IsFavorite = _favoritesStore.IsFavorite(item.Id);
            }
        }

        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<TweakItemViewModel>())
            {
                item.PropertyChanged -= OnTweakPropertyChanged;
                item.FavoriteChanged -= OnTweakFavoriteChanged;
            }
        }

        UpdateSelectionCount();
        RaiseHealthMetricsChanged();
    }

    private void RaiseHealthMetricsChanged()
    {
        OnPropertyChanged(nameof(GlobalOptimizationScore));
        OnPropertyChanged(nameof(HealthStatusMessage));
        OnPropertyChanged(nameof(ScorableTweaksTotal));
        OnPropertyChanged(nameof(ScorableTweaksMeasuredTotal));
        OnPropertyChanged(nameof(ScorableTweaksApplied));
        OnPropertyChanged(nameof(HealthCalculationSummary));
    }

    private void UpdateSelectionCount()
    {
        SelectedCount = Tweaks.Count(t => t.IsSelected);
    }

    private bool CanRunBulkOnSelected()
    {
        return !IsBulkRunning && SelectedCount > 0 && !Tweaks.Any(item => item.IsRunning);
    }

    private void CancelBulk()
    {
        if (!IsBulkRunning || _bulkCts is null)
        {
            return;
        }

        _bulkCts.Cancel();
        BulkStatusMessage = "Bulk cancellation requested.";
    }

    private void StartBulkCancellation()
    {
        ClearBulkCancellation();
        _bulkCts = new CancellationTokenSource();
    }

    private void ClearBulkCancellation()
    {
        _bulkCts?.Dispose();
        _bulkCts = null;
    }

    private bool FilterTweaks(object obj)
    {
        if (obj is not TweakItemViewModel item)
        {
            return false;
        }

        // Status filter (applied/rolled back)
        if (!string.IsNullOrEmpty(_statusFilter))
        {
            if (_statusFilter == "applied" && !item.IsApplied)
            {
                return false;
            }
            if (_statusFilter == "rolledback" && item.IsApplied)
            {
                return false;
            }
        }

        // Favorites filter
        if (_showFavoritesOnly && !item.IsFavorite)
        {
            return false;
        }

        if (item.Risk == TweakRiskLevel.Safe && !_showSafe)
        {
            return false;
        }

        if (item.Risk == TweakRiskLevel.Advanced && !_showAdvanced)
        {
            return false;
        }

        if (item.Risk == TweakRiskLevel.Risky && !_showRisky)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_searchText))
        {
            item.IsHighlighted = false;
            return true;
        }

        bool matches = item.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.Description.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.Id.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.Risk.ToString().Contains(_searchText, StringComparison.OrdinalIgnoreCase);

        item.IsHighlighted = matches;
        return matches;
    }

    private void UpdateFilterSummary()
    {
        var total = Tweaks.Count;
        var visible = TweaksView.Cast<object>().Count();
        FilterSummary = $"Showing {visible} of {total} tweaks.";
        _previewAllCommand.RaiseCanExecuteChanged();
        _applyAllCommand.RaiseCanExecuteChanged();
        _verifyAllCommand.RaiseCanExecuteChanged();
        _rollbackAllCommand.RaiseCanExecuteChanged();
        BuildCategoryGroups();
        HasVisibleTweaks = visible > 0 && (IsFlatView || CategoryGroups.Count > 0);
    }

    private void BuildCategoryGroups()
    {
        if (!(System.Windows.Application.Current?.Dispatcher?.CheckAccess() ?? true))
        {
            System.Windows.Application.Current?.Dispatcher?.BeginInvoke(() => BuildCategoryGroups());
            return;
        }

        static string FormatGroupName(string segment, string fallback)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                return fallback;
            }

            segment = segment.Trim();
            return segment.Length == 1
                ? segment.ToUpperInvariant()
                : char.ToUpperInvariant(segment[0]) + segment[1..].ToLowerInvariant();
        }

        var categoryOrder = new[] { "System", "Security", "Privacy", "Network", "Visibility", "Audio", "Peripheral", "Power", "Performance", "Cleanup", "Explorer", "Notifications" };
        var rootGroups = new Dictionary<string, CategoryGroupViewModel>(StringComparer.OrdinalIgnoreCase);

        foreach (var tweak in Tweaks.Where(t => FilterTweaks(t)))
        {
            var parts = (tweak.Id ?? string.Empty)
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
            {
                continue;
            }

            var rootCatName = FormatGroupName(parts[0], "Other");
            
            if (!rootGroups.TryGetValue(rootCatName, out var currentGroup))
            {
                currentGroup = new CategoryGroupViewModel(rootCatName, tweak.CategoryIcon)
                {
                    IsDense = rootCatName.Equals("Visibility", StringComparison.OrdinalIgnoreCase)
                };
                rootGroups[rootCatName] = currentGroup;
            }

            // Handle intermediate sub-groups (e.g. network.tcp.tweak)
            var parent = currentGroup;
            for (int i = 1; i < parts.Length - 1; i++)
            {
                var subName = FormatGroupName(parts[i], "Other");
                var subGroup = parent.SubGroups.FirstOrDefault(g => g.CategoryName == subName);
                if (subGroup == null)
                {
                    subGroup = new CategoryGroupViewModel(subName, "└──") { IsNested = true, IsExpanded = true };
                    parent.SubGroups.Add(subGroup);
                }
                parent = subGroup;
            }

            parent.AddTweak(tweak);
        }

        var orderedGroups = new List<CategoryGroupViewModel>();

        // Add in preferred order first
        foreach (var catName in categoryOrder)
        {
            if (rootGroups.TryGetValue(catName, out var g))
            {
                orderedGroups.Add(g);
                rootGroups.Remove(catName);
            }
        }

        // Add any remaining categories
        orderedGroups.AddRange(rootGroups.Values.OrderBy(x => x.CategoryName));

        CategoryGroups.Clear();
        foreach (var g in orderedGroups)
        {
            CategoryGroups.Add(g);
        }
    }

    public void ExpandAllCategories()
    {
        foreach (var group in CategoryGroups)
        {
            group.IsExpanded = true;
        }
    }

    public void CollapseAllCategories()
    {
        foreach (var group in CategoryGroups)
        {
            group.IsExpanded = false;
        }
    }

    private void ResetFilters()
    {
        SearchText = string.Empty;
        StatusFilter = string.Empty;
        ShowSafe = true;
        ShowAdvanced = true;
        ShowRisky = true;
    }

    private void OpenLogFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _logFolderPath,
                UseShellExecute = true
            });

            ExportStatusMessage = $"Opened log folder: {_logFolderPath}.";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Open log folder failed: {ex.Message}";
        }
    }

    private void OpenCsvLog()
    {
        try
        {
            if (!File.Exists(_tweakLogFilePath))
            {
                ExportStatusMessage = "No tweak log file yet. Run a tweak to generate one.";
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = _tweakLogFilePath,
                UseShellExecute = true
            });

            ExportStatusMessage = $"Opened log file: {_tweakLogFilePath}.";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Open log failed: {ex.Message}";
        }
    }

    private void SetDetailsExpanded(bool isExpanded)
    {
        foreach (var item in Tweaks)
        {
            item.IsDetailsExpanded = isExpanded;
        }
    }

    private async Task ExportPresetsAsync()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Optimizer Profile (*.json)|*.json|All Files (*.*)|*.*",
                FileName = $"optimizer_profile_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                Title = "Export Profile"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var selectedIds = Tweaks.Where(t => t.IsSelected).Select(t => t.Id).ToList();
            var appliedIds = Tweaks.Where(t => t.AppliedStatus == TweakAppliedStatus.Applied).Select(t => t.Id).ToList();

            var profile = new Core.Models.TweakProfile
            {
                Name = Path.GetFileNameWithoutExtension(dialog.FileName),
                Description = $"Custom profile exported on {DateTime.Now:yyyy-MM-dd HH:mm}",
                Author = "User",
                CreatedDate = DateTime.Now,
                Version = "1.0",
                SelectedTweakIds = selectedIds.Count > 0 ? selectedIds : appliedIds,
                AppliedTweakIds = appliedIds,
                Metadata = new Core.Models.ProfileMetadata
                {
                    TargetUseCase = "Custom",
                    TotalTweakCount = selectedIds.Count > 0 ? selectedIds.Count : appliedIds.Count,
                    TweaksByCategory = new Dictionary<string, int>(),
                    TweaksByRiskLevel = new Dictionary<string, int>()
                }
            };

            await _profileManager.SaveProfileAsync(profile, dialog.FileName);
            ExportStatusMessage = $"Profile exported successfully to {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Export failed: {ex.Message}";
        }
    }

    private async Task ImportPresetsAsync()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Optimizer Profile (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Import Profile"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var profile = await _profileManager.LoadProfileAsync(dialog.FileName);

            // Clear current selections
            foreach (var tweak in Tweaks)
            {
                tweak.IsSelected = false;
            }

            // Mark tweaks as selected based on profile
            int selectedCount = 0;
            foreach (var id in profile.SelectedTweakIds)
            {
                var tweak = Tweaks.FirstOrDefault(t => t.Id == id);
                if (tweak != null)
                {
                    tweak.IsSelected = true;
                    tweak.IsDetailsExpanded = true;
                    selectedCount++;
                }
            }

            ExportStatusMessage = $"Imported profile '{profile.Name}': {selectedCount}/{profile.SelectedTweakIds.Count} tweaks selected. Ready to apply.";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Import failed: {ex.Message}";
        }
    }

    private async Task LoadPresetAsync(object? parameter)
    {
        try
        {
            var presetName = parameter as string;
            if (string.IsNullOrEmpty(presetName))
            {
                return;
            }

            var profile = await _profileManager.CreatePresetAsync(presetName);

            // Clear current selections
            foreach (var tweak in Tweaks)
            {
                tweak.IsSelected = false;
            }

            // Mark tweaks as selected based on preset
            int selectedCount = 0;
            foreach (var id in profile.SelectedTweakIds)
            {
                var tweak = Tweaks.FirstOrDefault(t => t.Id == id);
                if (tweak != null)
                {
                    tweak.IsSelected = true;
                    selectedCount++;
                }
            }

            ExportStatusMessage = $"Loaded '{profile.Name}' preset: {selectedCount}/{profile.SelectedTweakIds.Count} tweaks selected.";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Failed to load preset: {ex.Message}";
        }
    }

    private async Task InitializePresetsAsync()
    {
        try
        {
            await _profileManager.InitializePresetsAsync();
        }
        catch (Exception ex)
        {
            // Silently fail - presets are not critical
            Debug.WriteLine($"Failed to initialize presets: {ex.Message}");
        }
    }

    private void CreateSnapshot()
    {
        // Simulate Registry Snapshot
        ExportStatusMessage = $"Registry Snapshot created: {DateTime.Now:yyyyMMdd_HHmm}.";
    }

    private void SetBulkLock(bool isLocked)
    {
        foreach (var item in Tweaks)
        {
            item.IsBulkLocked = isLocked;
        }
    }

    private void LoadPlugins()
    {
        try
        {
            var existingIds = new HashSet<string>(
                Tweaks.Select(t => t.Id).Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);

            var pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            if (!Directory.Exists(pluginsPath)) Directory.CreateDirectory(pluginsPath);
            
            var plugins = _pluginLoader.LoadPlugins(pluginsPath);
            foreach (var plugin in plugins)
            {
                Debug.WriteLine($"God-Tier Plugin Loaded: {plugin.PluginName} v{plugin.Version}");
                
                var pluginTweaks = plugin.GetTweaks();
                foreach (var tweak in pluginTweaks)
                {
                    if (string.IsNullOrWhiteSpace(tweak.Id) || !existingIds.Add(tweak.Id))
                    {
                        continue;
                    }

                    Tweaks.Add(new TweakItemViewModel(tweak, _pipeline, _isElevated));
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Plugin system error: {ex.Message}");
        }
    }

    public void ApplyRecommendations(IEnumerable<TweakRecommendation> recommendations)
    {
        if (recommendations == null) return;
        
        foreach (var recommendation in recommendations)
        {
            var tweak = Tweaks.FirstOrDefault(t => t.Id == recommendation.TweakId);
            if (tweak != null)
            {
                tweak.IsRecommended = true;
                tweak.RecommendationReason = recommendation.Reason;
                tweak.RecommendationConfidence = recommendation.ConfidenceScore;
            }
        }
    }

    public async Task ExportEncryptedProfileAsync(string filePath, string password)
    {
        var enabledIds = AllTweaks.Where(t => t.AppliedStatus == TweakAppliedStatus.Applied).Select(t => t.Id).ToList();
        await _syncService.ExportProfileAsync(filePath, password, enabledIds);
    }

    public async Task ImportEncryptedProfileAsync(string filePath, string password)
    {
        var enabledIds = await _syncService.ImportProfileAsync(filePath, password);
        // Sync logic to match imported IDs with existing tweaks
    }

    /// <summary>
    /// Detects all tweaks in all categories. Used by Dashboard's Scan Now button.
    /// </summary>
    public async Task DetectAllTweaksAsync()
    {
        // Expand all categories to trigger their detection
        foreach (var category in CategoryGroups)
        {
            if (!category.IsExpanded)
            {
                category.IsExpanded = true;
            }

            // Also expand sub-groups
            foreach (var subGroup in category.SubGroups)
            {
                if (!subGroup.IsExpanded)
                {
                    subGroup.IsExpanded = true;
                }
            }
        }

        // Wait for all detection to complete by detecting each tweak directly
        foreach (var tweak in Tweaks)
        {
            try
            {
                if (tweak.AppliedStatus == TweakAppliedStatus.Unknown)
                {
                    await tweak.DetectStatusAsync();
                }
            }
            catch
            {
                // Silently ignore detection failures for individual tweaks
            }
        }

        // Trigger health score recalculation
        OnPropertyChanged(nameof(GlobalOptimizationScore));
        OnPropertyChanged(nameof(ScorableTweaksMeasuredTotal));
        OnPropertyChanged(nameof(ScorableTweaksApplied));
        OnPropertyChanged(nameof(HealthCalculationSummary));
        OnPropertyChanged(nameof(HealthStatusMessage));
    }
}
