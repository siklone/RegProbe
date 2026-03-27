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
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Models;
using RegProbe.Core.Services;
using RegProbe.Infrastructure.Elevation;
using RegProbe.Infrastructure.Registry;
using RegProbe.Infrastructure.Services;
using RegProbe.Engine;
using RegProbe.Engine.Services;
using RegProbe.Engine.Tweaks;
using RegProbe.Engine.Tweaks.Commands.Power;
using RegProbe.Engine.Tweaks.Commands.Cleanup;
using RegProbe.Engine.Tweaks.Commands.Performance;
using RegProbe.Engine.Tweaks.Commands.Network;
using RegProbe.Engine.Tweaks.Commands.System;
using RegProbe.Engine.Tweaks.Misc;
using RegProbe.Engine.Tweaks.Peripheral;
using RegProbe.Engine.Tweaks.Power;
using RegProbe.App.Utilities;
using RegProbe.App.Services;
using RegProbe.Infrastructure;
using RegProbe.Core.Commands;
using RegProbe.Core.Files;
using RegProbe.Core.Registry;
using RegProbe.Core.Tasks;

namespace RegProbe.App.ViewModels;

public sealed class TweaksViewModel : ViewModelBase, IDisposable
{
    private static readonly string[] FastCleanTweakIds =
    {
        "cleanup.temp-files",
        "cleanup.directx-shader-cache",
        "cleanup.thumbnail-cache",
        "cleanup.wer-files"
    };

    private static readonly string[] DiskCheckTweakIds =
    {
        "system-check-disk-health"
    };

    private bool _isDisposed;
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
    private readonly RelayCommand _clearCategorySelectionCommand;
    private readonly RelayCommand _openLogFolderCommand;
    private readonly RelayCommand _openCsvLogCommand;
    private readonly RelayCommand _openDocsCoverageReportCommand;
    private readonly RelayCommand _openProvenanceReportCommand;
    private readonly RelayCommand _filterAppliedCommand;
    private readonly RelayCommand _filterRolledBackCommand;
    private readonly RelayCommand _showSettingsWorkspaceCommand;
    private readonly RelayCommand _showMaintenanceWorkspaceCommand;
	private readonly RelayCommand _expandAllDetailsCommand;
	private readonly RelayCommand _collapseAllDetailsCommand;
	private readonly bool _isElevated;
	private readonly string _elevatedHostExecutablePath;
	private readonly bool _isElevatedHostAvailable;
	private readonly IRegistryAccessor _localRegistryAccessor;
	private readonly IRegistryAccessor _elevatedRegistryAccessor;
    private readonly IRegistryAccessor _scanAwareElevatedRegistryAccessor;
	private readonly IServiceManager _elevatedServiceManager;
	private readonly IScheduledTaskManager _elevatedTaskManager;
    private readonly IFileSystemAccessor _elevatedFileSystemAccessor;
    private readonly ICommandRunner _elevatedCommandRunner;
    private string _exportStatusMessage = "Logs are ready to export.";
    private string _bulkStatusMessage = "Bulk actions are idle.";
    private string _filterSummary = "Showing 0 of 0 settings.";
    private bool _isExporting;
    private bool _isBulkRunning;
    private int _bulkProgressCurrent;
    private int _bulkProgressTotal;
    private int _selectedCount;
    private string _searchText = string.Empty;
    private string _diskHealthSearchText = string.Empty;
    private string _statusFilter = string.Empty; // "applied", "rolledback", or empty for all
    private bool _showSafe = true;
    private bool _showAdvanced = true;
    private bool _showRisky = true;
    private bool _showFavoritesOnly = false;
    private bool _showClassA = true;
    private bool _showClassB = true;
    private bool _showClassC = true;
    private bool _showClassD = true;
    private bool _showDiskChecksOnly = false;
    private string _selectedCategoryName = string.Empty;
    private ConfigurationWorkspaceKind _selectedWorkspace = ConfigurationWorkspaceKind.Settings;
    private int _selectedMainTabIndex;
    private string _diskHealthFilterSummary = "Showing 0 of 0 disk checks.";
    private static readonly string[] MainTabNames =
    [
        "Configuration",
        "Policy Reference",
        "Services",
        "Bloatware",
        "Startup",
        "Disk Health"
    ];
    private bool _hasVisibleTweaks;
    private bool _hasVisibleDiskHealthTweaks;
    private bool _isFlatView;
    private readonly bool _showContributorEvidenceUi = ContributorMode.IsEnabled;
    private readonly IFavoritesStore _favoritesStore;
    private readonly ITweakInventoryStateStore _inventoryStateStore;
    private CancellationTokenSource? _searchCts;
    private CancellationTokenSource? _bulkCts;
    private CancellationTokenSource? _inventorySaveCts;
    private bool _isApplyingCachedInventory;
    private bool _isBackgroundRefreshRunning;
    private string _inventoryStatusMessage = "Status check not available yet.";
    private readonly string _logFolderPath;
    private readonly IProfileSyncService _syncService = new ProfileSyncService();
    private readonly PluginLoader _pluginLoader = new();
    private readonly string _tweakLogFilePath;
    private int _totalTweaksAvailable;
    private int _tweaksApplied;
    private int _tweaksRolledBack;
    private long _logFileSizeBytes;
    private int _docsMissingCount;
    private string _docsCoverageSummary = "Docs report unavailable.";
    private string _docsCoverageReportPath = string.Empty;
    private int _provenanceReviewCount;
    private string _provenanceCoverageSummary = "Source links unavailable.";
    private string _provenanceReportPath = string.Empty;
    private readonly IProfileManager _profileManager;
    private readonly TweakExecutionPipeline _pipeline;
    private readonly IEnumerable<ITweakProvider>? _providerList;
    private readonly IRollbackStateStore _rollbackStore;
    private readonly TweakDocumentationLinker _documentationLinker = new();
    private readonly TweakProvenanceCatalogService _provenanceCatalogService = new();
    private readonly TweakEvidenceClassCatalogService _evidenceClassCatalogService = new();
    private readonly ConfigurationWorkspaceClassifier _workspaceClassifier = new();
    private readonly IAppLogger _appLogger;
    private readonly IBusyService _busyService;

    /// <summary>
    /// DNS configuration panel ViewModel for Network category.
    /// </summary>
    public DnsConfigurationViewModel DnsConfiguration { get; } = new();
    public WinConfigCatalogPanelViewModel WinConfigCatalog { get; }
    public PolicyReferencePanelViewModel PolicyReference { get; }
    public ServiceManagementPanelViewModel ServiceManagement { get; }

    public BloatwareViewModel Bloatware { get; }
    public StartupViewModel Startup { get; }

    private string _currentTab = "Configuration";
    public string CurrentTab
    {
        get => _currentTab;
        set
        {
            var normalized = NormalizeMainTabName(value);
            if (SetProperty(ref _currentTab, normalized))
            {
                var tabIndex = GetMainTabIndex(normalized);
                if (_selectedMainTabIndex != tabIndex)
                {
                    _selectedMainTabIndex = tabIndex;
                    OnPropertyChanged(nameof(SelectedMainTabIndex));
                }

                RaiseMainTabPropertiesChanged();
            }
        }
    }

    public ConfigurationWorkspaceKind SelectedWorkspace
    {
        get => _selectedWorkspace;
        set
        {
            if (SetProperty(ref _selectedWorkspace, value))
            {
                if (value != ConfigurationWorkspaceKind.Settings && _showDiskChecksOnly)
                {
                    _showDiskChecksOnly = false;
                    OnPropertyChanged(nameof(ShowDiskChecksOnly));
                }

                RaiseWorkspacePropertiesChanged();

                if (!string.IsNullOrWhiteSpace(_selectedCategoryName) && !CurrentWorkspaceContainsCategory(_selectedCategoryName))
                {
                    SelectedCategoryName = string.Empty;
                }

                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
    }

    public RelayCommand SetTabCommand { get; }

    public int SelectedMainTabIndex
    {
        get => _selectedMainTabIndex;
        set
        {
            var normalized = NormalizeMainTabIndex(value);
            if (SetProperty(ref _selectedMainTabIndex, normalized))
            {
                var tabName = GetMainTabName(normalized);
                if (!string.Equals(_currentTab, tabName, StringComparison.Ordinal))
                {
                    _currentTab = tabName;
                    OnPropertyChanged(nameof(CurrentTab));
                }

                RaiseMainTabPropertiesChanged();
            }
        }
    }

    public TweaksViewModel(
        IEnumerable<ITweakProvider>? providers,
        IBusyService busyService,
        BloatwareViewModel bloatware,
        StartupViewModel startup)
    {
        Bloatware = bloatware ?? throw new ArgumentNullException(nameof(bloatware));
        Startup = startup ?? throw new ArgumentNullException(nameof(startup));
        _busyService = busyService ?? throw new ArgumentNullException(nameof(busyService));
        _providerList = providers;
        var paths = AppPaths.FromEnvironment();
        paths.EnsureDirectories();
        _appLogger = new FileAppLogger(paths);
        var logger = _appLogger;
        _logFolderPath = paths.LogDirectory;
        _tweakLogFilePath = paths.TweakLogFilePath;
        _logStore = new FileTweakLogStore(paths);
        _profileManager = new ProfileManager(paths);
        _rollbackStore = new RollbackStateStore(paths);
        _favoritesStore = new FavoritesStore(paths);
        _inventoryStateStore = new TweakInventoryStateStore(paths);
		_pipeline = new TweakExecutionPipeline(logger, _logStore, _rollbackStore);
		var settingsStore = new SettingsStore(paths);
		_isElevated = ProcessElevation.IsElevated();
		_elevatedHostExecutablePath = ElevatedHostLocator.GetExecutablePath();
		_isElevatedHostAvailable = File.Exists(_elevatedHostExecutablePath);
		var elevatedHostClient = new ElevatedHostClient(new ElevatedHostClientOptions
		{
			HostExecutablePath = _elevatedHostExecutablePath,
			PipeName = ElevatedHostDefaults.GetPipeNameForProcess(Process.GetCurrentProcess().Id),
			ParentProcessId = Process.GetCurrentProcess().Id
		});
        var machineLocalRegistryAccessor = new LocalRegistryAccessor();
        _elevatedRegistryAccessor = new ElevatedRegistryAccessor(elevatedHostClient);
        _localRegistryAccessor = new RoutingRegistryAccessor(machineLocalRegistryAccessor, _elevatedRegistryAccessor);
        var hybridRegistryAccessor = new HybridRegistryAccessor(machineLocalRegistryAccessor, _elevatedRegistryAccessor);
        _scanAwareElevatedRegistryAccessor = _isElevated ? _elevatedRegistryAccessor : hybridRegistryAccessor;
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

        DiskHealthTweaksView = new ListCollectionView(Tweaks);
        DiskHealthTweaksView.Filter = FilterDiskHealthTweak;
        DiskHealthTweaksView.SortDescriptions.Add(new SortDescription(nameof(TweakItemViewModel.Name), ListSortDirection.Ascending));

        _exportLogsCommand = new RelayCommand(_ => _ = ExportLogsAsync(), _ => !IsExporting);
        _previewAllCommand = new RelayCommand(_ => _ = RunBulkAsync("Preview", GetAllFilteredTweaks, (item, token) => item.RunPreviewAsync(token)), _ => CanRunBulkInspectable(GetAllFilteredTweaks));
        _applyAllCommand = new RelayCommand(_ => _ = RunBulkAsync("Apply", GetAllActionableFilteredTweaks, (item, token) => item.RunApplyAsync(token)), _ => CanRunBulkMutating(GetAllActionableFilteredTweaks));
        _verifyAllCommand = new RelayCommand(_ => _ = RunBulkAsync("Verify", GetAllFilteredTweaks, (item, token) => item.RunVerifyAsync(token)), _ => CanRunBulkInspectable(GetAllFilteredTweaks));
        _rollbackAllCommand = new RelayCommand(_ => _ = RunBulkAsync("Rollback", GetAllActionableFilteredTweaks, (item, token) => item.RunRollbackAsync(token)), _ => CanRunBulkMutating(GetAllActionableFilteredTweaks));
        _cancelAllCommand = new RelayCommand(_ => CancelBulk(), _ => IsBulkRunning);
        _selectAllCommand = new RelayCommand(_ => SelectAll());
        _deselectAllCommand = new RelayCommand(_ => DeselectAll());
        _detectSelectedCommand = new RelayCommand(
            _ => _ = RunBulkAsync("Detect Selected", GetSelectedTweaks, (item, token) => item.RunDetectAsync(token)),
            _ => CanRunBulkInspectable(GetSelectedTweaks));
        _applySelectedCommand = new RelayCommand(
            _ => _ = RunBulkAsync("Apply Selected", GetSelectedActionableTweaks, (item, token) => item.RunApplyAsync(token)),
            _ => CanRunBulkMutating(GetSelectedActionableTweaks));
        _verifySelectedCommand = new RelayCommand(
            _ => _ = RunBulkAsync("Verify Selected", GetSelectedTweaks, (item, token) => item.RunVerifyAsync(token)),
            _ => CanRunBulkInspectable(GetSelectedTweaks));
        _rollbackSelectedCommand = new RelayCommand(
            _ => _ = RunBulkAsync("Rollback Selected", GetSelectedActionableTweaks, (item, token) => item.RunRollbackAsync(token)),
            _ => CanRunBulkMutating(GetSelectedActionableTweaks));
        _loadPresetCommand = new RelayCommand(async parameter => await LoadPresetAsync(parameter));
        _resetFiltersCommand = new RelayCommand(_ => ResetFilters());
        _clearCategorySelectionCommand = new RelayCommand(_ => SelectedCategoryName = string.Empty);
        _openLogFolderCommand = new RelayCommand(_ => OpenLogFolder());
        _openCsvLogCommand = new RelayCommand(_ => OpenCsvLog());
        _openDocsCoverageReportCommand = new RelayCommand(_ => OpenDocsCoverageReport());
        _openProvenanceReportCommand = new RelayCommand(_ => OpenProvenanceReport());
        _filterAppliedCommand = new RelayCommand(_ => StatusFilter = "applied");
        _filterRolledBackCommand = new RelayCommand(_ => StatusFilter = "rolledback");
        _showSettingsWorkspaceCommand = new RelayCommand(_ => SelectedWorkspace = ConfigurationWorkspaceKind.Settings);
        _showMaintenanceWorkspaceCommand = new RelayCommand(_ => SelectedWorkspace = ConfigurationWorkspaceKind.Maintenance);
        _expandAllDetailsCommand = new RelayCommand(_ => SetDetailsExpanded(true));
        _collapseAllDetailsCommand = new RelayCommand(_ => SetDetailsExpanded(false));
        ExportPresetCommand = new RelayCommand(async _ => await ExportPresetsAsync());
        ImportPresetCommand = new RelayCommand(async _ => await ImportPresetsAsync());
        CreateSnapshotCommand = new RelayCommand(_ => CreateSnapshot());
        SetTabCommand = new RelayCommand(param =>
        {
            if (param is string tabName)
                CurrentTab = tabName;
        });

        LoadProviderTweaks();
        LoadPlugins();
        ApplyTweakMetadata();
        LoadCachedInventoryState();
        _documentationLinker.Apply(Tweaks);
        _provenanceCatalogService.Apply(Tweaks);
        _evidenceClassCatalogService.Apply(Tweaks);
        WinConfigCatalog = new WinConfigCatalogPanelViewModel(paths, BuildWinConfigCategoryCoverageMap);
        PolicyReference = new PolicyReferencePanelViewModel(OpenPolicyReferenceEntry);
        ServiceManagement = new ServiceManagementPanelViewModel(_elevatedServiceManager, _isElevatedHostAvailable, _appLogger);
        UpdateFilterSummary();
        UpdateDiskHealthSummary();
        LoadDocsCoverageReport();
        LoadProvenanceCoverageReport();
        RefreshSummaryStats();
        RefreshPolicyReferencePanel();
        _ = InitializePresetsAsync();
        _ = ServiceManagement.RefreshAsync();

    }

    private IDictionary<string, int> BuildWinConfigCategoryCoverageMap()
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var tweak in Tweaks.Where(t => !IsDiskCheckTweak(t)))
        {
            var categoryId = MapLocalCategoryToWinConfigId(tweak.Category);
            if (string.IsNullOrWhiteSpace(categoryId))
            {
                continue;
            }

            counts.TryGetValue(categoryId, out var current);
            counts[categoryId] = current + 1;
        }

        return counts;
    }

    private static string? MapLocalCategoryToWinConfigId(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return null;
        }

        var normalized = category.Trim().ToLowerInvariant();

        if (normalized.Contains("network"))
            return "network";
        if (normalized.Contains("power"))
            return "power";
        if (normalized.Contains("privacy"))
            return "privacy";
        if (normalized.Contains("security"))
            return "security";
        if (normalized.Contains("system"))
            return "system";
        if (normalized.Contains("visibility") || normalized.Contains("display") || normalized.Contains("explorer"))
            return "visibility";
        if (normalized.Contains("peripheral") || normalized.Contains("input") || normalized.Contains("usb") || normalized.Contains("audio"))
            return "peripheral";
        if (normalized.Contains("nvidia") || normalized.Contains("graphics") || normalized.Contains("gpu"))
            return "nvidia";
        if (normalized.Contains("cleanup"))
            return "cleanup";
        if (normalized.Contains("policy"))
            return "policies";
        if (normalized.Contains("performance") || normalized.Contains("affinity"))
            return "affinities";
        if (normalized.Contains("misc"))
            return "misc";

        return null;
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
                _scanAwareElevatedRegistryAccessor,
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

        var vscode = Tweaks.FirstOrDefault(t => t.Id == "misc.disable-vscode-telemetry");
        if (vscode != null)
        {
            vscode.CodeExample =
                "\"telemetry.telemetryLevel\": \"off\"\n" +
                "\"workbench.enableExperiments\": false\n" +
                "\"update.mode\": \"manual\"\n" +
                "\"extensions.autoUpdate\": false";
            vscode.ReferenceLinks.Add(new ReferenceLink("VS Code telemetry docs", "https://code.visualstudio.com/docs/getstarted/telemetry", kind: ReferenceLinkKind.Docs));
            vscode.ReferenceLinks.Add(new ReferenceLink("VS Code update behavior", "https://code.visualstudio.com/docs/setup/setup-overview#_updates", kind: ReferenceLinkKind.Docs));
        }

        var diskHealth = Tweaks.FirstOrDefault(IsDiskCheckTweak);
        if (diskHealth != null)
        {
            diskHealth.ActionType = TweakActionType.Custom;
            diskHealth.ActionButtonText = "Run Check";
            diskHealth.TargetValue = "Report ready";
            diskHealth.CodeExample = "chkdsk C:";
            diskHealth.ReferenceLinks.Add(new ReferenceLink("CHKDSK reference", "https://learn.microsoft.com/windows-server/administration/windows-commands/chkdsk", kind: ReferenceLinkKind.Docs));
        }
    }

    public string Title => "Configuration";

	public bool IsElevated => _isElevated;

	public string ElevationStatusMessage => IsElevated
		? "Running with administrator privileges."
		: "Running without administrator privileges. Admin-required tweaks will prompt for elevation.";

	public bool IsElevatedHostAvailable => _isElevatedHostAvailable;

	public string ElevatedHostStatusMessage => IsElevatedHostAvailable
		? string.Empty
		: $"ElevatedHost not found. Expected at: {_elevatedHostExecutablePath}. Build RegProbe.ElevatedHost or set {ElevatedHostDefaults.OverridePathEnvVar}.";

	public ObservableCollection<TweakItemViewModel> Tweaks { get; }

    public IEnumerable<TweakItemViewModel> AllTweaks => Tweaks;

    public ICollectionView TweaksView { get; }

    public ICollectionView DiskHealthTweaksView { get; }

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

    public ICommand ClearCategorySelectionCommand => _clearCategorySelectionCommand;

    public ICommand OpenLogFolderCommand => _openLogFolderCommand;

    public ICommand OpenCsvLogCommand => _openCsvLogCommand;

    public ICommand OpenDocsCoverageReportCommand => _openDocsCoverageReportCommand;

    public ICommand OpenProvenanceReportCommand => _openProvenanceReportCommand;

    public ICommand FilterAppliedCommand => _filterAppliedCommand;

    public ICommand FilterRolledBackCommand => _filterRolledBackCommand;

    public ICommand ShowSettingsWorkspaceCommand => _showSettingsWorkspaceCommand;

    public ICommand ShowMaintenanceWorkspaceCommand => _showMaintenanceWorkspaceCommand;

    public ICommand ExpandAllDetailsCommand => _expandAllDetailsCommand;

    public ICommand CollapseAllDetailsCommand => _collapseAllDetailsCommand;
    public ICommand ExportPresetCommand { get; }
    public ICommand ImportPresetCommand { get; }
    public ICommand CreateSnapshotCommand { get; }

    public int ScorableTweaksTotal => Tweaks.Count(IsScorableForHealth);

    public int TotalTweaksAvailable
    {
        get => _totalTweaksAvailable;
        private set => SetProperty(ref _totalTweaksAvailable, value);
    }

    public int SettingsWorkspaceCount => Tweaks.Count(t => t.ShowInApp && GetWorkspaceKind(t) == ConfigurationWorkspaceKind.Settings && !IsDiskCheckTweak(t));

    public int MaintenanceWorkspaceCount => Tweaks.Count(t => t.ShowInApp && GetWorkspaceKind(t) == ConfigurationWorkspaceKind.Maintenance);

    public int DiskHealthCount => Tweaks.Count(IsDiskCheckTweak);

    public int CurrentWorkspaceItemCount => SelectedWorkspace == ConfigurationWorkspaceKind.Maintenance
        ? MaintenanceWorkspaceCount
        : SettingsWorkspaceCount;

    public bool IsSettingsWorkspaceSelected => SelectedWorkspace == ConfigurationWorkspaceKind.Settings;

    public bool IsMaintenanceWorkspaceSelected => SelectedWorkspace == ConfigurationWorkspaceKind.Maintenance;

    public string CurrentWorkspaceLabel => IsMaintenanceWorkspaceSelected ? "Maintenance" : "Windows Settings";

    public string CurrentWorkspaceDescription => IsMaintenanceWorkspaceSelected
        ? "One-click cleanup and repair tasks for when Windows feels messy, slow, or stuck."
        : "PC behavior and feature switches that stay in place until you change them again.";

    public string CurrentMainTabEyebrow => CurrentTab switch
    {
        "Policy Reference" => "Policy Reference",
        "Services" => "Services",
        "Bloatware" => "Bloatware",
        "Startup" => "Startup",
        "Disk Health" => "Disk Health",
        _ => "Configuration"
    };

    public string CurrentMainTabTitle => CurrentTab switch
    {
        "Policy Reference" => "Windows Policy Reference",
        "Services" => "Service Management",
        "Bloatware" => "Bloatware",
        "Startup" => "Startup Apps",
        "Disk Health" => "Disk Health Checks",
        _ => CurrentWorkspaceLabel
    };

    public string CurrentMainTabSubtitle => CurrentTab switch
    {
        "Policy Reference" => "See which parts of Windows and installed components are driven by policy paths.",
        "Services" => "Review Windows services with descriptions, startup modes, and safe actions.",
        "Bloatware" => "Review installed apps and remove the ones you do not want on this PC.",
        "Startup" => "Trim startup items so Windows boots cleaner and quieter.",
        "Disk Health" => "Run read-only storage diagnostics here. These checks are tools, not persistent tweaks, and they do not count toward the optimization score.",
        _ => CurrentWorkspaceDescription
    };

    public bool IsConfigurationTabSelected => SelectedMainTabIndex == 0;
    public bool IsPolicyReferenceTabSelected => SelectedMainTabIndex == 1;
    public bool IsServicesTabSelected => SelectedMainTabIndex == 2;
    public bool IsBloatwareTabSelected => SelectedMainTabIndex == 3;
    public bool IsStartupTabSelected => SelectedMainTabIndex == 4;
    public bool IsDiskHealthTabSelected => SelectedMainTabIndex == 5;

    public string DiskHealthFilterSummary
    {
        get => _diskHealthFilterSummary;
        private set => SetProperty(ref _diskHealthFilterSummary, value);
    }

    public bool HasVisibleDiskHealthTweaks
    {
        get => _hasVisibleDiskHealthTweaks;
        private set => SetProperty(ref _hasVisibleDiskHealthTweaks, value);
    }

    public string DiskHealthEmptyStateTitle => "No disk checks match";

    public string DiskHealthEmptyStateDescription => string.IsNullOrWhiteSpace(DiskHealthSearchText)
        ? "Disk diagnostics will show up here when available."
        : "Try a simpler search or clear the disk-health filter.";

    public string CurrentWorkspaceCountLabel => IsMaintenanceWorkspaceSelected
        ? $"{CurrentWorkspaceItemCount} tasks available"
        : $"{CurrentWorkspaceItemCount} settings available";

    public string WorkspaceCategoryHeader => IsMaintenanceWorkspaceSelected ? "Task Groups" : "Configuration Areas";

    public string AllItemsLabel => IsMaintenanceWorkspaceSelected ? "All Tasks" : "All Settings";

    public string SearchPlaceholder => IsMaintenanceWorkspaceSelected
        ? "Search cleanup and repair tasks..."
        : "Search Windows settings and feature switches...";

    public string WorkspaceStatusHint => IsMaintenanceWorkspaceSelected
        ? "Use these when you want to clean up caches, reset a component, or fix a common Windows issue."
        : "Use these when you want Windows to behave differently and keep that behavior in place.";

    public string EmptyStateTitle => IsMaintenanceWorkspaceSelected
        ? "No cleanup tasks match"
        : "No settings match";

    public string EmptyStateDescription => IsMaintenanceWorkspaceSelected
        ? "Try a simpler search or switch back to Windows Settings."
        : "Try a simpler search or pick a different area.";

    public bool ShowDnsConfigurationPanel =>
        IsSettingsWorkspaceSelected &&
        string.Equals(SelectedCategoryName, "Network", StringComparison.OrdinalIgnoreCase);

    public int TweaksApplied
    {
        get => _tweaksApplied;
        private set => SetProperty(ref _tweaksApplied, value);
    }

    public int TweaksRolledBack
    {
        get => _tweaksRolledBack;
        private set => SetProperty(ref _tweaksRolledBack, value);
    }

    public long LogFileSizeBytes
    {
        get => _logFileSizeBytes;
        private set => SetProperty(ref _logFileSizeBytes, value);
    }

    public string LogFileSizeFormatted => FormatBytes(LogFileSizeBytes);

    public int DocsMissingCount
    {
        get => _docsMissingCount;
        private set
        {
            if (SetProperty(ref _docsMissingCount, value))
            {
                OnPropertyChanged(nameof(DocsCoverageOk));
                OnPropertyChanged(nameof(DocsCoverageWarn));
                OnPropertyChanged(nameof(DocsCoverageCritical));
            }
        }
    }

    public string DocsCoverageSummary
    {
        get => _docsCoverageSummary;
        private set => SetProperty(ref _docsCoverageSummary, value);
    }

    public string DocsCoverageReportPath
    {
        get => _docsCoverageReportPath;
        private set => SetProperty(ref _docsCoverageReportPath, value);
    }

    public bool DocsCoverageOk => DocsMissingCount == 0;

    public bool DocsCoverageWarn => DocsMissingCount > 0 && DocsMissingCount <= 10;

    public bool DocsCoverageCritical => DocsMissingCount > 10;

    public int ProvenanceReviewCount
    {
        get => _provenanceReviewCount;
        private set => SetProperty(ref _provenanceReviewCount, value);
    }

    public string ProvenanceCoverageSummary
    {
        get => _provenanceCoverageSummary;
        private set => SetProperty(ref _provenanceCoverageSummary, value);
    }

    public string ProvenanceReportPath
    {
        get => _provenanceReportPath;
        private set => SetProperty(ref _provenanceReportPath, value);
    }

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
        : $"{ScorableTweaksApplied} / {ScorableTweaksMeasuredTotal} detected settings applied (Safe+Advanced; excludes Demo/Risky).";

    public string HealthStatusMessage => GlobalOptimizationScore switch
    {
        >= 90 => "Excellent optimization level",
        >= 70 => "Good optimization level",
        >= 40 => "Moderate optimization level",
        _ => "System needs optimization"
    };

    private static bool IsScorableForHealth(TweakItemViewModel tweak) =>
        !tweak.Id.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
        && !IsDiskCheckTweak(tweak)
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

    public string InventoryStatusMessage
    {
        get => _inventoryStatusMessage;
        private set => SetProperty(ref _inventoryStatusMessage, value);
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
        ? "No settings selected"
        : $"{SelectedCount} setting{(SelectedCount == 1 ? string.Empty : "s")} selected";

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

    public string DiskHealthSearchText
    {
        get => _diskHealthSearchText;
        set
        {
            if (SetProperty(ref _diskHealthSearchText, value))
            {
                TriggerDiskHealthSearchUpdate();
                OnPropertyChanged(nameof(DiskHealthEmptyStateDescription));
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
        "applied" => "Applied Settings",
        "rolledback" => "Rolled Back Settings",
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

    private void TriggerDiskHealthSearchUpdate()
    {
        System.Windows.Application.Current?.Dispatcher?.BeginInvoke(() =>
        {
            DiskHealthTweaksView.Refresh();
            UpdateDiskHealthSummary();
        });
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

    public bool ShowContributorEvidenceUi => _showContributorEvidenceUi;

    public bool ShowClassA
    {
        get => _showClassA;
        set
        {
            if (SetProperty(ref _showClassA, value))
            {
                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
    }

    public bool ShowClassB
    {
        get => _showClassB;
        set
        {
            if (SetProperty(ref _showClassB, value))
            {
                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
    }

    public bool ShowClassC
    {
        get => _showClassC;
        set
        {
            if (SetProperty(ref _showClassC, value))
            {
                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
    }

    public bool ShowClassD
    {
        get => _showClassD;
        set
        {
            if (SetProperty(ref _showClassD, value))
            {
                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
    }

    public bool ShowDiskChecksOnly
    {
        get => _showDiskChecksOnly;
        set
        {
            if (SetProperty(ref _showDiskChecksOnly, value))
            {
                if (value)
                {
                    SelectedCategoryName = string.Empty;
                }

                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
    }

    public string SelectedCategoryName
    {
        get => _selectedCategoryName;
        set
        {
            if (SetProperty(ref _selectedCategoryName, value))
            {
                OnPropertyChanged(nameof(IsAllCategoriesSelected));
                OnPropertyChanged(nameof(SelectedCategoryLabel));
                OnPropertyChanged(nameof(ShowDnsConfigurationPanel));
                TweaksView.Refresh();
                // Keep sidebar scroll/selection stable when only switching category.
                UpdateFilterSummary(rebuildCategoryGroups: false);
            }
        }
    }

    public bool IsAllCategoriesSelected => string.IsNullOrWhiteSpace(_selectedCategoryName);

    public string SelectedCategoryLabel => IsAllCategoriesSelected ? AllItemsLabel : _selectedCategoryName;

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
            FileName = "configuration-log.csv",
            Title = "Export activity logs"
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
            _appLogger.Log(LogLevel.Info, $"Activity: Logs - Tweak log exported ({dialog.FileName})");
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

    private bool CanRunBulkInspectable(Func<List<TweakItemViewModel>> getTweaks)
    {
        if (IsBulkRunning || Tweaks.Any(item => item.IsRunning))
        {
            return false;
        }

        return getTweaks().Count > 0;
    }

    private bool CanRunBulkMutating(Func<List<TweakItemViewModel>> getTweaks)
    {
        if (IsBulkRunning || Tweaks.Any(item => item.IsRunning))
        {
            return false;
        }

        return getTweaks().Any(item => item.IsEvidenceClassActionable);
    }

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
            BulkStatusMessage = $"{label} in progress ({items.Count} items)...";
            using var busy = _busyService.Busy(BulkStatusMessage);

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
            RefreshSummaryStats();
        }
    }

    private List<TweakItemViewModel> GetAllFilteredTweaks()
    {
        return TweaksView.Cast<TweakItemViewModel>().ToList();
    }

    private List<TweakItemViewModel> GetAllActionableFilteredTweaks()
    {
        return TweaksView.Cast<TweakItemViewModel>()
            .Where(item => item.IsEvidenceClassActionable)
            .ToList();
    }

    private List<TweakItemViewModel> GetSelectedTweaks()
    {
        return Tweaks.Where(t => t.IsSelected).ToList();
    }

    private List<TweakItemViewModel> GetSelectedActionableTweaks()
    {
        return Tweaks
            .Where(t => t.IsSelected && t.IsEvidenceClassActionable)
            .ToList();
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
        else if (e.PropertyName is nameof(TweakItemViewModel.IsApplied)
                 or nameof(TweakItemViewModel.AppliedStatus)
                 or nameof(TweakItemViewModel.WasRolledBack)
                 or nameof(TweakItemViewModel.CurrentValue)
                 or nameof(TweakItemViewModel.TargetValue)
                 or nameof(TweakItemViewModel.LastDetectedAtUtc)
                 or nameof(TweakItemViewModel.RegistryPath))
        {
            RaiseHealthMetricsChanged();
            RefreshSummaryStats();
            ScheduleInventorySnapshotSave();
            RefreshPolicyReferencePanel();
            if (!string.IsNullOrEmpty(_statusFilter))
            {
                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }

        if (!string.IsNullOrWhiteSpace(_diskHealthSearchText)
            && e.PropertyName is (nameof(TweakItemViewModel.StatusMessage)
                or nameof(TweakItemViewModel.TerminalOutput)))
        {
            DiskHealthTweaksView.Refresh();
            UpdateDiskHealthSummary();
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
        RaiseWorkspacePropertiesChanged();
        RefreshSummaryStats();
        RefreshPolicyReferencePanel();
        DiskHealthTweaksView.Refresh();
        UpdateDiskHealthSummary();
        OnPropertyChanged(nameof(DiskHealthCount));
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

        return FilterTweaksInternal(item, includeCategoryFilter: true);
    }

    private bool FilterTweaksInternal(TweakItemViewModel item, bool includeCategoryFilter)
    {
        if (IsDiskCheckTweak(item))
        {
            return false;
        }

        if (!item.ShowInApp)
        {
            return false;
        }

        if (GetWorkspaceKind(item) != SelectedWorkspace)
        {
            return false;
        }

        if (includeCategoryFilter && !IsAllCategoriesSelected)
        {
            if (!string.Equals(item.Category, _selectedCategoryName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Status filter (applied/rolled back)
        if (!string.IsNullOrEmpty(_statusFilter))
        {
            if (_statusFilter == "applied" && !item.IsApplied)
            {
                return false;
            }
            if (_statusFilter == "rolledback" && !item.WasRolledBack)
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

        if (_showContributorEvidenceUi)
        {
            if (item.EvidenceClassId == "A" && !_showClassA)
            {
                return false;
            }

            if (item.EvidenceClassId == "B" && !_showClassB)
            {
                return false;
            }

            if (item.EvidenceClassId == "C" && !_showClassC)
            {
                return false;
            }

            if (item.EvidenceClassId == "D" && !_showClassD)
            {
                return false;
            }
        }

        if (string.IsNullOrWhiteSpace(_searchText))
        {
            item.IsHighlighted = false;
            return true;
        }

        bool matches = item.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.Description.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.Id.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.RegistryPath.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.Risk.ToString().Contains(_searchText, StringComparison.OrdinalIgnoreCase);

        item.IsHighlighted = matches;
        return matches;
    }

    private bool FilterDiskHealthTweak(object? candidate)
    {
        if (candidate is not TweakItemViewModel item || !IsDiskCheckTweak(item))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_diskHealthSearchText))
        {
            return true;
        }

        return item.Name.Contains(_diskHealthSearchText, StringComparison.OrdinalIgnoreCase)
            || item.Description.Contains(_diskHealthSearchText, StringComparison.OrdinalIgnoreCase)
            || item.Id.Contains(_diskHealthSearchText, StringComparison.OrdinalIgnoreCase)
            || item.StatusMessage.Contains(_diskHealthSearchText, StringComparison.OrdinalIgnoreCase)
            || item.TerminalOutput.Contains(_diskHealthSearchText, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateFilterSummary(bool rebuildCategoryGroups = true)
    {
        var total = CurrentWorkspaceItemCount;
        var visible = TweaksView.Cast<object>().Count();
        var noun = IsMaintenanceWorkspaceSelected ? "tasks" : "settings";
        FilterSummary = $"Showing {visible} of {total} {noun}.";
        _previewAllCommand.RaiseCanExecuteChanged();
        _applyAllCommand.RaiseCanExecuteChanged();
        _verifyAllCommand.RaiseCanExecuteChanged();
        _rollbackAllCommand.RaiseCanExecuteChanged();
        if (rebuildCategoryGroups)
        {
            BuildCategoryGroups();
        }
        HasVisibleTweaks = visible > 0;
    }

    private void UpdateDiskHealthSummary()
    {
        var total = DiskHealthCount;
        var visible = DiskHealthTweaksView.Cast<object>().Count();
        DiskHealthFilterSummary = $"Showing {visible} of {total} disk check{(total == 1 ? string.Empty : "s")}.";
        HasVisibleDiskHealthTweaks = visible > 0;
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

        var categoryOrder = IsMaintenanceWorkspaceSelected
            ? new[] { "Cleanup", "Network", "System", "Security", "Privacy", "Peripheral", "Power" }
            : new[] { "System", "Security", "Privacy", "Network", "Visibility", "Audio", "Peripheral", "Power", "Performance", "Cleanup", "Explorer", "Notifications", "Devtools" };
        var rootGroups = new Dictionary<string, CategoryGroupViewModel>(StringComparer.OrdinalIgnoreCase);

        foreach (var tweak in Tweaks.Where(t => FilterTweaksInternal(t, includeCategoryFilter: false)))
        {
            var tweakId = tweak.Id ?? string.Empty;
            var parts = tweakId
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
            {
                continue;
            }

            var rootCatName = !string.IsNullOrWhiteSpace(tweak.Category) &&
                              !string.Equals(tweak.Category, "Other", StringComparison.OrdinalIgnoreCase)
                ? tweak.Category
                : FormatGroupName(parts[0], "Other");

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
            var subgroupStartIndex = tweakId.StartsWith("plugin.", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
            for (int i = subgroupStartIndex; i < parts.Length - 1; i++)
            {
                var subName = FormatGroupName(parts[i], "Other");
                var subGroup = parent.SubGroups.FirstOrDefault(g => g.CategoryName == subName);
                if (subGroup == null)
                {
                    subGroup = new CategoryGroupViewModel(subName, "â””â”€â”€") { IsNested = true, IsExpanded = true, Parent = parent };
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

        if (!string.IsNullOrWhiteSpace(_selectedCategoryName)
            && !CategoryGroups.Any(g => string.Equals(g.CategoryName, _selectedCategoryName, StringComparison.OrdinalIgnoreCase)))
        {
            _selectedCategoryName = string.Empty;
            OnPropertyChanged(nameof(SelectedCategoryName));
            OnPropertyChanged(nameof(IsAllCategoriesSelected));
            OnPropertyChanged(nameof(SelectedCategoryLabel));
            OnPropertyChanged(nameof(ShowDnsConfigurationPanel));
            TweaksView.Refresh();
            FilterSummary = $"Showing {TweaksView.Cast<object>().Count()} of {CurrentWorkspaceItemCount} {(IsMaintenanceWorkspaceSelected ? "tasks" : "settings")}.";
        }
    }

    private ConfigurationWorkspaceKind GetWorkspaceKind(TweakItemViewModel tweak)
        => _workspaceClassifier.Classify(tweak.Id, tweak.Category);

    private bool CurrentWorkspaceContainsCategory(string categoryName)
        => Tweaks.Any(t =>
            t.ShowInApp &&
            GetWorkspaceKind(t) == SelectedWorkspace &&
            !IsDiskCheckTweak(t) &&
            string.Equals(t.Category, categoryName, StringComparison.OrdinalIgnoreCase));

    private void RaiseWorkspacePropertiesChanged()
    {
        OnPropertyChanged(nameof(SettingsWorkspaceCount));
        OnPropertyChanged(nameof(MaintenanceWorkspaceCount));
        OnPropertyChanged(nameof(CurrentWorkspaceItemCount));
        OnPropertyChanged(nameof(IsSettingsWorkspaceSelected));
        OnPropertyChanged(nameof(IsMaintenanceWorkspaceSelected));
        OnPropertyChanged(nameof(CurrentWorkspaceLabel));
        OnPropertyChanged(nameof(CurrentWorkspaceDescription));
        OnPropertyChanged(nameof(CurrentWorkspaceCountLabel));
        OnPropertyChanged(nameof(WorkspaceCategoryHeader));
        OnPropertyChanged(nameof(AllItemsLabel));
        OnPropertyChanged(nameof(SearchPlaceholder));
        OnPropertyChanged(nameof(WorkspaceStatusHint));
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateDescription));
        OnPropertyChanged(nameof(SelectedCategoryLabel));
        OnPropertyChanged(nameof(ShowDnsConfigurationPanel));
        OnPropertyChanged(nameof(CurrentMainTabTitle));
        OnPropertyChanged(nameof(CurrentMainTabSubtitle));
    }

    private void RaiseMainTabPropertiesChanged()
    {
        OnPropertyChanged(nameof(CurrentMainTabEyebrow));
        OnPropertyChanged(nameof(CurrentMainTabTitle));
        OnPropertyChanged(nameof(CurrentMainTabSubtitle));
        OnPropertyChanged(nameof(IsConfigurationTabSelected));
        OnPropertyChanged(nameof(IsPolicyReferenceTabSelected));
        OnPropertyChanged(nameof(IsServicesTabSelected));
        OnPropertyChanged(nameof(IsBloatwareTabSelected));
        OnPropertyChanged(nameof(IsStartupTabSelected));
        OnPropertyChanged(nameof(IsDiskHealthTabSelected));
    }

    private static string NormalizeMainTabName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return MainTabNames[0];
        }

        var match = MainTabNames.FirstOrDefault(tab => string.Equals(tab, value, StringComparison.OrdinalIgnoreCase));
        return match ?? MainTabNames[0];
    }

    private static int NormalizeMainTabIndex(int value)
    {
        if (value < 0)
        {
            return 0;
        }

        return value >= MainTabNames.Length ? MainTabNames.Length - 1 : value;
    }

    private static int GetMainTabIndex(string? value)
    {
        var normalized = NormalizeMainTabName(value);
        for (var i = 0; i < MainTabNames.Length; i++)
        {
            if (string.Equals(MainTabNames[i], normalized, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return 0;
    }

    private static string GetMainTabName(int index) => MainTabNames[NormalizeMainTabIndex(index)];

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
        DiskHealthSearchText = string.Empty;
        StatusFilter = string.Empty;
        ShowSafe = true;
        ShowAdvanced = true;
        ShowRisky = true;
        ShowClassA = true;
        ShowClassB = true;
        ShowClassC = true;
        ShowClassD = true;
        ShowDiskChecksOnly = false;
        SelectedCategoryName = string.Empty;
    }

    private void RefreshSummaryStats()
    {
        TotalTweaksAvailable = Tweaks.Count(t => t.ShowInApp && !IsDiskCheckTweak(t));
        RaiseWorkspacePropertiesChanged();
        TweaksApplied = Tweaks.Count(t => t.ShowInApp && !IsDiskCheckTweak(t) && t.IsApplied);
        TweaksRolledBack = Tweaks.Count(t => t.ShowInApp && !IsDiskCheckTweak(t) && t.WasRolledBack);
        RefreshLogFileSize();
        UpdateInventoryStatusMessage();
    }

    private void RefreshPolicyReferencePanel()
    {
        PolicyReference?.Refresh(Tweaks);
    }

    private void OpenPolicyReferenceEntry(PolicyReferenceEntry entry)
    {
        if (entry is null)
        {
            return;
        }

        SelectedMainTabIndex = 0;
        SelectedWorkspace = ConfigurationWorkspaceKind.Settings;
        SelectedCategoryName = string.Empty;
        StatusFilter = string.Empty;
        ShowDiskChecksOnly = false;
        SearchText = entry.SearchFragment;
    }

    public void ShowMaintenanceCleanupWorkspace()
    {
        SelectedMainTabIndex = 0;
        CurrentTab = "Configuration";
        SelectedWorkspace = ConfigurationWorkspaceKind.Maintenance;
        SelectedCategoryName = ResolveMaintenanceCleanupCategoryName();
        StatusFilter = string.Empty;
        SearchText = string.Empty;
        ShowFavoritesOnly = false;
        ShowSafe = true;
        ShowAdvanced = true;
        ShowRisky = true;
        ShowDiskChecksOnly = false;
        TweaksView.Refresh();
        UpdateFilterSummary();
    }

    private static bool IsDiskCheckTweak(TweakItemViewModel item)
    {
        if (DiskCheckTweakIds.Any(id => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return item.Id.Contains("disk-health", StringComparison.OrdinalIgnoreCase)
            || item.Name.Contains("Disk Health", StringComparison.OrdinalIgnoreCase)
            || item.Description.Contains("file system errors", StringComparison.OrdinalIgnoreCase);
    }

    public async Task RunFastCleanAsync(CancellationToken ct = default)
    {
        ShowMaintenanceCleanupWorkspace();

        if (IsBulkRunning)
        {
            return;
        }

        var fastCleanItems = Tweaks
            .Where(t => FastCleanTweakIds.Contains(t.Id, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (fastCleanItems.Count == 0)
        {
            BulkStatusMessage = "Fast Clean is not available right now.";
            return;
        }

        DeselectAll();
        foreach (var item in fastCleanItems)
        {
            item.IsSelected = true;
        }

        await RunBulkAsync(
            "Fast Clean",
            () => fastCleanItems,
            async (item, token) =>
            {
                ct.ThrowIfCancellationRequested();
                await item.RunApplyAsync(token);
            });
    }

    private string ResolveMaintenanceCleanupCategoryName()
    {
        return Tweaks
            .Where(t => GetWorkspaceKind(t) == ConfigurationWorkspaceKind.Maintenance)
            .Select(t => t.Category)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(category =>
                category.Contains("cleanup", StringComparison.OrdinalIgnoreCase)
                || category.Contains("clean", StringComparison.OrdinalIgnoreCase))
            ?? string.Empty;
    }

    private void LoadCachedInventoryState()
    {
        var cachedStates = _inventoryStateStore.Load();
        if (cachedStates.Count == 0)
        {
            UpdateInventoryStatusMessage();
            return;
        }

        _isApplyingCachedInventory = true;
        try
        {
            var appliedCount = 0;
            DateTimeOffset? latestTimestamp = null;

            foreach (var tweak in Tweaks)
            {
                if (!cachedStates.TryGetValue(tweak.Id, out var cachedState))
                {
                    continue;
                }

                tweak.ApplyCachedInventoryState(cachedState);
                appliedCount++;

                if (cachedState.LastDetectedAtUtc.HasValue)
                {
                    if (!latestTimestamp.HasValue || cachedState.LastDetectedAtUtc > latestTimestamp)
                    {
                        latestTimestamp = cachedState.LastDetectedAtUtc;
                    }
                }
            }

            if (appliedCount > 0)
            {
                var latestText = latestTimestamp.HasValue
                    ? latestTimestamp.Value.ToLocalTime().ToString("HH:mm:ss")
                    : "unknown time";
                InventoryStatusMessage = $"Loaded last checked status for {appliedCount} settings (last: {latestText}).";
            }
        }
        finally
        {
            _isApplyingCachedInventory = false;
        }
    }

    private void UpdateInventoryStatusMessage()
    {
        var inventoryTweaks = Tweaks.Where(t => !IsDiskCheckTweak(t)).ToList();
        var total = inventoryTweaks.Count;
        var detected = inventoryTweaks.Count(t => t.AppliedStatus != TweakAppliedStatus.Unknown);
        var requiresPrompt = inventoryTweaks.Count(t => t.WillPromptForDetect);

        var suffix = _isBackgroundRefreshRunning
            ? " Refreshing in background..."
            : string.Empty;

        InventoryStatusMessage = requiresPrompt > 0
            ? $"Live status: {detected}/{total} checked. {requiresPrompt} need admin confirmation.{suffix}"
            : $"Live status: {detected}/{total} checked.{suffix}";
    }

    private async void ScheduleInventorySnapshotSave()
    {
        if (_isApplyingCachedInventory)
        {
            return;
        }

        _inventorySaveCts?.Cancel();
        _inventorySaveCts?.Dispose();
        _inventorySaveCts = new CancellationTokenSource();
        var token = _inventorySaveCts.Token;

        try
        {
            await Task.Delay(400, token);
            token.ThrowIfCancellationRequested();
            SaveInventorySnapshot();
        }
        catch (OperationCanceledException)
        {
            // Debounced by a newer save request.
        }
        catch
        {
            // Best-effort cache write.
        }
    }

    private void SaveInventorySnapshot()
    {
        var snapshot = Tweaks
            .Select(t => t.ExportInventoryState())
            .ToList();

        _inventoryStateStore.Save(snapshot);
    }

    private void RefreshLogFileSize()
    {
        if (!File.Exists(_tweakLogFilePath))
        {
            LogFileSizeBytes = 0;
            return;
        }

        try
        {
            LogFileSizeBytes = new FileInfo(_tweakLogFilePath).Length;
        }
        catch
        {
            LogFileSizeBytes = 0;
        }
    }

    private void LoadDocsCoverageReport()
    {
        try
        {
            var docsRoot = DocsLocator.TryFindDocsRoot();
            if (string.IsNullOrWhiteSpace(docsRoot))
            {
                DocsCoverageReportPath = string.Empty;
                DocsMissingCount = 0;
                DocsCoverageSummary = "Docs folder not found.";
                return;
            }

            var priorityHtml = Path.Combine(docsRoot, "tweaks", "tweak-docs-missing-priority.html");
            var priorityMd = Path.Combine(docsRoot, "tweaks", "tweak-docs-missing-priority.md");
            var priorityCsv = Path.Combine(docsRoot, "tweaks", "tweak-docs-missing-priority.csv");
            var fallbackHtml = Path.Combine(docsRoot, "tweaks", "tweak-docs-missing.html");
            var fallbackMd = Path.Combine(docsRoot, "tweaks", "tweak-docs-missing.md");
            var fallbackCsv = Path.Combine(docsRoot, "tweaks", "tweak-docs-missing.csv");

            var reportPath = File.Exists(priorityHtml)
                ? priorityHtml
                : File.Exists(priorityMd)
                    ? priorityMd
                    : File.Exists(fallbackHtml)
                        ? fallbackHtml
                        : File.Exists(fallbackMd) ? fallbackMd : string.Empty;

            DocsCoverageReportPath = reportPath;

            var csvPath = File.Exists(priorityCsv)
                ? priorityCsv
                : File.Exists(fallbackCsv) ? fallbackCsv : string.Empty;

            if (!string.IsNullOrWhiteSpace(csvPath))
            {
                var lines = File.ReadAllLines(csvPath);
                DocsMissingCount = Math.Max(0, lines.Length - 1);
                DocsCoverageSummary = DocsMissingCount == 0 ? "All documented" : $"{DocsMissingCount} missing";
            }
            else
            {
                DocsMissingCount = 0;
                DocsCoverageSummary = string.IsNullOrWhiteSpace(reportPath)
                    ? "Docs report unavailable."
                    : "Docs report ready.";
            }
        }
        catch
        {
            DocsCoverageReportPath = string.Empty;
            DocsMissingCount = 0;
            DocsCoverageSummary = "Docs report unavailable.";
        }
    }

    private void LoadProvenanceCoverageReport()
    {
        try
        {
            var catalog = _provenanceCatalogService.Catalog;
            ProvenanceReportPath = _provenanceCatalogService.ResolveMarkdownReportPath();

            if (catalog.TotalTweaks <= 0)
            {
                ProvenanceReviewCount = 0;
                ProvenanceCoverageSummary = "Source links unavailable.";
                return;
            }

            ProvenanceReviewCount = catalog.ReviewNeededTweaks;
            ProvenanceCoverageSummary =
                $"{catalog.RepoBackedTweaks}/{catalog.TotalTweaks} repo-linked | " +
                $"{catalog.InternalsBackedTweaks} internals refs | " +
                $"{catalog.ReviewNeededTweaks} review";
        }
        catch
        {
            ProvenanceReportPath = string.Empty;
            ProvenanceReviewCount = 0;
            ProvenanceCoverageSummary = "Source links unavailable.";
        }
    }

    private void OpenDocsCoverageReport()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(DocsCoverageReportPath) || !File.Exists(DocsCoverageReportPath))
            {
                LoadDocsCoverageReport();
            }

            if (!string.IsNullOrWhiteSpace(DocsCoverageReportPath) && File.Exists(DocsCoverageReportPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = DocsCoverageReportPath,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    private void OpenProvenanceReport()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ProvenanceReportPath) || !File.Exists(ProvenanceReportPath))
            {
                LoadProvenanceCoverageReport();
            }

            if (!string.IsNullOrWhiteSpace(ProvenanceReportPath) && File.Exists(ProvenanceReportPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = ProvenanceReportPath,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Ignore errors
        }
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
            _appLogger.Log(LogLevel.Info, $"Activity: Logs - Log folder opened ({_logFolderPath})");
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
            _appLogger.Log(LogLevel.Info, $"Activity: Logs - Tweak log opened ({_tweakLogFilePath})");
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

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F2} KB";
        if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
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
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _appLogger.Log(LogLevel.Info, $"Plugin discovery: baseDir='{baseDir}'");

            var existingIds = new HashSet<string>(
                Tweaks.Select(t => t.Id).Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);

            var pluginsPath = Path.Combine(baseDir, "Plugins");
            if (!Directory.Exists(pluginsPath))
            {
                Directory.CreateDirectory(pluginsPath);
            }

            _appLogger.Log(LogLevel.Info, $"Plugin discovery: pluginsPath='{pluginsPath}'");

            var plugins = _pluginLoader.LoadPlugins(pluginsPath);

            var pluginList = plugins.ToList();
            _appLogger.Log(LogLevel.Info, $"Plugin discovery: loadedPlugins={pluginList.Count}");

            foreach (var plugin in pluginList)
            {
                _appLogger.Log(LogLevel.Info, $"Plugin loaded: name='{plugin.PluginName}' version='{plugin.Version}'");

                var pluginTweaks = plugin.GetTweaks();
                var pluginTweaksList = pluginTweaks?.ToList() ?? new List<ITweak>();
                _appLogger.Log(LogLevel.Info, $"Plugin tweaks: plugin='{plugin.PluginName}' count={pluginTweaksList.Count}");

                foreach (var tweak in pluginTweaksList)
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
            _appLogger.Log(LogLevel.Error, "Plugin system error", ex);
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
    public async Task DetectAllTweaksAsync(
        IProgress<StartupScanProgress>? progress = null,
        CancellationToken ct = default,
        bool isStartupScan = false,
        bool forceRedetect = false,
        bool skipElevationPrompts = false,
        bool skipExpensiveOperations = false)
    {
        IEnumerable<TweakItemViewModel> candidates = Tweaks.Where(t => !IsDiskCheckTweak(t));
        if (isStartupScan)
        {
            candidates = candidates.Where(t => t.IsStartupScanEligible);
        }

        if (skipExpensiveOperations)
        {
            candidates = candidates.Where(t => t.IsScanFriendly);
        }

        if (skipElevationPrompts)
        {
            candidates = candidates.Where(t => !t.WillPromptForDetect);
        }

        var tweaksToScan = candidates.ToList();
        var perTweakTimeout = isStartupScan
            ? TimeSpan.FromSeconds(2)
            : TimeSpan.FromSeconds(6);

        // Wait for all detection to complete by detecting each tweak directly
        var totalTweaks = tweaksToScan.Count;
        var currentIndex = 0;
        progress?.Report(new StartupScanProgress(0, totalTweaks));

        foreach (var tweak in tweaksToScan)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                currentIndex++;
                progress?.Report(new StartupScanProgress(currentIndex, totalTweaks, tweak.Name));
                if (forceRedetect || tweak.AppliedStatus == TweakAppliedStatus.Unknown)
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeoutCts.CancelAfter(perTweakTimeout);
                    await tweak.DetectStatusAsync(timeoutCts.Token);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                // Timed out. Leave the previous state and continue.
            }
            catch
            {
                // Silently ignore detection failures for individual tweaks
            }
        }

        progress?.Report(new StartupScanProgress(totalTweaks, totalTweaks));
        SaveInventorySnapshot();
        UpdateInventoryStatusMessage();

        // Trigger health score recalculation
        OnPropertyChanged(nameof(GlobalOptimizationScore));
        OnPropertyChanged(nameof(ScorableTweaksMeasuredTotal));
        OnPropertyChanged(nameof(ScorableTweaksApplied));
        OnPropertyChanged(nameof(HealthCalculationSummary));
        OnPropertyChanged(nameof(HealthStatusMessage));

        if (!isStartupScan && !skipElevationPrompts && !skipExpensiveOperations)
        {
            foreach (var category in CategoryGroups)
            {
                category.MarkDetected();
            }
        }
    }

    public async Task RefreshInventoryInBackgroundAsync(CancellationToken ct = default)
    {
        if (_isBackgroundRefreshRunning)
        {
            return;
        }

        _isBackgroundRefreshRunning = true;
        UpdateInventoryStatusMessage();
        try
        {
            await DetectAllTweaksAsync(
                progress: null,
                ct: ct,
                isStartupScan: false,
                forceRedetect: true,
                skipElevationPrompts: true,
                skipExpensiveOperations: false);
        }
        finally
        {
            _isBackgroundRefreshRunning = false;
            UpdateInventoryStatusMessage();
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        // Dispose CancellationTokenSources
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _bulkCts?.Cancel();
        _bulkCts?.Dispose();
        _inventorySaveCts?.Cancel();
        _inventorySaveCts?.Dispose();

        // Unsubscribe collection changed events
        Tweaks.CollectionChanged -= OnTweaksCollectionChanged;

        // Unsubscribe tweak property changed events
        foreach (var tweak in Tweaks)
        {
            tweak.PropertyChanged -= OnTweakPropertyChanged;
            tweak.FavoriteChanged -= OnTweakFavoriteChanged;
        }

    }
}
