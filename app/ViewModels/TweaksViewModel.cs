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
    private readonly RelayCommand _resetFiltersCommand;
    private readonly RelayCommand _clearCategorySelectionCommand;
    private readonly RelayCommand _filterAppliedCommand;
    private readonly RelayCommand _filterRolledBackCommand;
    private readonly RelayCommand _showSettingsWorkspaceCommand;
    private readonly RelayCommand _showMaintenanceWorkspaceCommand;
	private readonly RelayCommand _expandAllDetailsCommand;
	private readonly RelayCommand _collapseAllDetailsCommand;
	private readonly bool _isElevated;
	private readonly string _elevatedHostExecutablePath;
	private readonly bool _isElevatedHostAvailable;
    private string _bulkStatusMessage = "Bulk actions are idle.";
    private bool _isBulkRunning;
    private int _bulkProgressCurrent;
    private int _bulkProgressTotal;
    private int _selectedCount;
    private readonly TweaksShellStateViewModel _shellState = new();
    private readonly TweaksPresentationStateViewModel _presentationState = new();
    private readonly bool _showContributorEvidenceUi = ContributorMode.IsEnabled;
    private readonly IFavoritesStore _favoritesStore;
    private readonly TweakExecutionPipeline _pipeline;
    private readonly IRollbackStateStore _rollbackStore;
    private readonly WorkspaceBrowseCoordinator _browseCoordinator;
    private readonly WorkspaceCatalogCoordinator _catalogCoordinator;
    private readonly WorkspaceCollectionCoordinator _collectionCoordinator;
    private readonly ConfigurationWorkspaceCoordinator _configurationCoordinator;
    private readonly WorkspaceHealthCoordinator _healthCoordinator;
    private readonly WorkspaceInventoryCoordinator _inventoryCoordinator;
    private readonly WorkspaceOperationsCoordinator _operationsCoordinator;
    private readonly WorkspaceSupportCoordinator _supportCoordinator;
    private readonly WorkspaceActionCoordinator _workspaceActionCoordinator;
    private readonly IAppLogger _appLogger;
    private readonly IBusyService _busyService;

    /// <summary>
    /// DNS configuration panel ViewModel for Network category.
    /// </summary>
    public DnsConfigurationViewModel DnsConfiguration { get; } = new();
    public WinConfigCatalogPanelViewModel WinConfigCatalog { get; }

    public ConfigurationWorkspaceKind SelectedWorkspace
    {
        get => _shellState.SelectedWorkspace;
        set => _shellState.SelectedWorkspace = value;
    }

    public TweaksViewModel(
        IEnumerable<ITweakProvider>? providers,
        IBusyService busyService)
    {
        _busyService = busyService ?? throw new ArgumentNullException(nameof(busyService));
        _workspaceActionCoordinator = new WorkspaceActionCoordinator(_busyService);
        _shellState.PropertyChanged += OnShellStatePropertyChanged;
        _presentationState.PropertyChanged += OnPresentationStatePropertyChanged;
        _browseCoordinator = new WorkspaceBrowseCoordinator(_shellState, _presentationState, _showContributorEvidenceUi);
        _configurationCoordinator = new ConfigurationWorkspaceCoordinator(this);
        _supportCoordinator = new WorkspaceSupportCoordinator();
        _supportCoordinator.PropertyChanged += OnSupportCoordinatorPropertyChanged;
        _healthCoordinator = new WorkspaceHealthCoordinator();
        _healthCoordinator.PropertyChanged += OnHealthCoordinatorPropertyChanged;
        var paths = AppPaths.FromEnvironment();
        paths.EnsureDirectories();
        _appLogger = new FileAppLogger(paths);
        var logger = _appLogger;
        _logStore = new FileTweakLogStore(paths);
        var profileManager = new ProfileManager(paths);
        _operationsCoordinator = new WorkspaceOperationsCoordinator(
            _logStore,
            profileManager,
            _appLogger,
            paths.LogDirectory,
            paths.TweakLogFilePath);
        _operationsCoordinator.PropertyChanged += OnOperationsCoordinatorPropertyChanged;
        _rollbackStore = new RollbackStateStore(paths);
        _favoritesStore = new FavoritesStore(paths);
        _collectionCoordinator = new WorkspaceCollectionCoordinator(_favoritesStore, GetWorkspaceKind);
        _inventoryCoordinator = new WorkspaceInventoryCoordinator(new TweakInventoryStateStore(paths));
        _inventoryCoordinator.PropertyChanged += OnInventoryCoordinatorPropertyChanged;
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
        var elevatedRegistryAccessor = new ElevatedRegistryAccessor(elevatedHostClient);
        var localRegistryAccessor = new RoutingRegistryAccessor(machineLocalRegistryAccessor, elevatedRegistryAccessor);
        var hybridRegistryAccessor = new HybridRegistryAccessor(machineLocalRegistryAccessor, elevatedRegistryAccessor);
        IRegistryAccessor scanAwareElevatedRegistryAccessor = _isElevated ? elevatedRegistryAccessor : hybridRegistryAccessor;
        var elevatedServiceManager = new ElevatedServiceManager(elevatedHostClient);
        var elevatedTaskManager = new ElevatedScheduledTaskManager(elevatedHostClient);
        var elevatedFileSystemAccessor = new ElevatedFileSystemAccessor(elevatedHostClient);
        var elevatedCommandRunner = new ElevatedCommandRunner(elevatedHostClient);
        var systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var system32Path = Path.Combine(systemRoot, "System32");
        var mobsyncPath = Path.Combine(system32Path, "mobsync.exe");
        var mobsyncDisabledPath = mobsyncPath + ".disabled";
        var psrPath = Path.Combine(system32Path, "psr.exe");
        var psrDisabledPath = psrPath + ".disabled";
		var helpPanePath = Path.Combine(system32Path, "HelpPane.exe");
        var helpPaneDisabledPath = helpPanePath + ".disabled";
        _catalogCoordinator = new WorkspaceCatalogCoordinator(
            providers,
            localRegistryAccessor,
            scanAwareElevatedRegistryAccessor,
            elevatedServiceManager,
            elevatedTaskManager,
            elevatedFileSystemAccessor,
            elevatedCommandRunner,
            _pipeline,
            _isElevated,
            _appLogger);

        Tweaks = new ObservableCollection<TweakItemViewModel>();
        Tweaks.CollectionChanged += OnTweaksCollectionChanged;

        TweaksView = CollectionViewSource.GetDefaultView(Tweaks);
        TweaksView.Filter = FilterTweaks;
        TweaksView.SortDescriptions.Add(new SortDescription(nameof(TweakItemViewModel.Risk), ListSortDirection.Ascending));
        TweaksView.SortDescriptions.Add(new SortDescription(nameof(TweakItemViewModel.Name), ListSortDirection.Ascending));
        RepairsRowsView = CollectionViewSource.GetDefaultView(_collectionCoordinator.RepairsRows);
        RepairsRowsView.Filter = FilterRepairsRows;
        RepairsRowsView.SortDescriptions.Add(new SortDescription(nameof(RepairsItemViewModel.Risk), ListSortDirection.Ascending));
        RepairsRowsView.SortDescriptions.Add(new SortDescription(nameof(RepairsItemViewModel.Name), ListSortDirection.Ascending));

        _exportLogsCommand = new RelayCommand(_ => _ = _operationsCoordinator.ExportLogsAsync(), _ => !IsExporting);
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
        _resetFiltersCommand = new RelayCommand(_ => ResetFilters());
        _clearCategorySelectionCommand = new RelayCommand(_ => SelectedCategoryName = string.Empty);
        _filterAppliedCommand = new RelayCommand(_ => _configurationCoordinator.ShowAppliedOnly());
        _filterRolledBackCommand = new RelayCommand(_ => _configurationCoordinator.ShowRolledBackOnly());
        _showSettingsWorkspaceCommand = new RelayCommand(_ => _configurationCoordinator.ShowConfigurationWorkspace());
        _showMaintenanceWorkspaceCommand = new RelayCommand(_ => SelectedWorkspace = ConfigurationWorkspaceKind.Maintenance);
        _expandAllDetailsCommand = new RelayCommand(_ => SetDetailsExpanded(true));
        _collapseAllDetailsCommand = new RelayCommand(_ => SetDetailsExpanded(false));

        _catalogCoordinator.LoadInitialTweaks(Tweaks);
        _inventoryCoordinator.LoadCachedInventoryState(Tweaks);
        _supportCoordinator.ApplyCatalogs(Tweaks);
        WinConfigCatalog = new WinConfigCatalogPanelViewModel(paths, BuildWinConfigCategoryCoverageMap);
        UpdateFilterSummary();
        _supportCoordinator.Initialize();
        _healthCoordinator.Refresh(Tweaks);
        RefreshSummaryStats();
        _ = _operationsCoordinator.InitializePresetsAsync();

    }

    private IDictionary<string, int> BuildWinConfigCategoryCoverageMap()
    {
        return _catalogCoordinator.BuildWinConfigCategoryCoverageMap(Tweaks);
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

    public ICollectionView RepairsRowsView { get; }

    public ObservableCollection<CategoryGroupViewModel> CategoryGroups => _presentationState.CategoryGroups;

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

    public ICommand ResetFiltersCommand => _resetFiltersCommand;

    public ICommand ClearCategorySelectionCommand => _clearCategorySelectionCommand;

    public ICommand FilterAppliedCommand => _filterAppliedCommand;

    public ICommand FilterRolledBackCommand => _filterRolledBackCommand;

    public ICommand ShowSettingsWorkspaceCommand => _showSettingsWorkspaceCommand;

    public ICommand ShowMaintenanceWorkspaceCommand => _showMaintenanceWorkspaceCommand;

    public ICommand ExpandAllDetailsCommand => _expandAllDetailsCommand;

    public ICommand CollapseAllDetailsCommand => _collapseAllDetailsCommand;

    public int ScorableTweaksTotal => _healthCoordinator.ScorableTweaksTotal;

    public int TotalTweaksAvailable => _presentationState.TotalTweaksAvailable;

    public int SettingsWorkspaceCount => Tweaks.Count(t => t.ShowInApp && GetWorkspaceKind(t) == ConfigurationWorkspaceKind.Settings);

    public int MaintenanceWorkspaceCount => Tweaks.Count(t => t.ShowInApp && GetWorkspaceKind(t) == ConfigurationWorkspaceKind.Maintenance);

    public int CurrentWorkspaceItemCount => SelectedWorkspace == ConfigurationWorkspaceKind.Maintenance
        ? MaintenanceWorkspaceCount
        : SettingsWorkspaceCount;

    public bool IsSettingsWorkspaceSelected => _shellState.IsSettingsWorkspaceSelected;

    public bool IsMaintenanceWorkspaceSelected => _shellState.IsMaintenanceWorkspaceSelected;

    public string CurrentWorkspaceLabel => _shellState.CurrentWorkspaceLabel;

    public string CurrentWorkspaceDescription => _shellState.CurrentWorkspaceDescription;

    public string CurrentWorkspaceCountLabel => IsMaintenanceWorkspaceSelected
        ? $"{CurrentWorkspaceItemCount} repairs"
        : $"{CurrentWorkspaceItemCount} settings";

    public string WorkspaceCategoryHeader => _shellState.WorkspaceCategoryHeader;

    public string AllItemsLabel => _shellState.AllItemsLabel;

    public string SearchPlaceholder => _shellState.SearchPlaceholder;

    public string ToolbarSectionLabel => _shellState.ToolbarSectionLabel;

    public string ToolbarSectionHint => _shellState.ToolbarSectionHint;

    public string CurrentWorkspaceModeNote => _shellState.CurrentWorkspaceModeNote;

    public string WorkspaceStatusHint => _shellState.WorkspaceStatusHint;

    public string EmptyStateTitle => _shellState.EmptyStateTitle;

    public string EmptyStateDescription => _shellState.EmptyStateDescription;

    public string EmptyStateActionText => _shellState.EmptyStateActionText;

    public bool CanClearCategorySelection => _shellState.CanClearCategorySelection;

    public string ClearCategorySelectionText => _shellState.ClearCategorySelectionText;

    public string FilterSummaryLabel => _shellState.FilterSummaryLabel;

    public string InventorySummaryLabel => _shellState.InventorySummaryLabel;

    public string SelectedCategoryContext => _shellState.SelectedCategoryContext;

    public bool ShowDnsConfigurationPanel =>
        IsSettingsWorkspaceSelected &&
        string.Equals(SelectedCategoryName, "Network", StringComparison.OrdinalIgnoreCase);

    public int TweaksApplied => _presentationState.TweaksApplied;

    public int TweaksRolledBack => _presentationState.TweaksRolledBack;

    public long LogFileSizeBytes => _operationsCoordinator.LogFileSizeBytes;

    public string LogFileSizeFormatted => _operationsCoordinator.LogFileSizeFormatted;

    public int DocsMissingCount => _supportCoordinator.DocsMissingCount;

    public string DocsCoverageSummary => _supportCoordinator.DocsCoverageSummary;

    public string DocsCoverageReportPath => _supportCoordinator.DocsCoverageReportPath;

    public bool DocsCoverageOk => _supportCoordinator.DocsCoverageOk;

    public bool DocsCoverageWarn => _supportCoordinator.DocsCoverageWarn;

    public bool DocsCoverageCritical => _supportCoordinator.DocsCoverageCritical;

    public int ProvenanceReviewCount => _supportCoordinator.ProvenanceReviewCount;

    public string ProvenanceCoverageSummary => _supportCoordinator.ProvenanceCoverageSummary;

    public string ProvenanceReportPath => _supportCoordinator.ProvenanceReportPath;

    public int ScorableTweaksMeasuredTotal => _healthCoordinator.ScorableTweaksMeasuredTotal;

    public int ScorableTweaksApplied => _healthCoordinator.ScorableTweaksApplied;

    public int GlobalOptimizationScore => _healthCoordinator.GlobalOptimizationScore;

    public string HealthCalculationSummary => _healthCoordinator.HealthCalculationSummary;

    public string HealthStatusMessage => _healthCoordinator.HealthStatusMessage;

    public string ExportStatusMessage => _operationsCoordinator.ExportStatusMessage;

    public string BulkStatusMessage
    {
        get => _bulkStatusMessage;
        private set => SetProperty(ref _bulkStatusMessage, value);
    }

    public string InventoryStatusMessage => _inventoryCoordinator.InventoryStatusMessage;

    public bool IsExporting => _operationsCoordinator.IsExporting;

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
        get => _shellState.IsFlatView;
        set => _shellState.IsFlatView = value;
    }

    public string SearchText
    {
        get => _shellState.SearchText;
        set => _shellState.SearchText = value;
    }

    public string StatusFilter
    {
        get => _shellState.StatusFilter;
        set => _shellState.StatusFilter = value;
    }

    public string StatusFilterLabel => _shellState.StatusFilterLabel;

    public bool HasStatusFilter => _shellState.HasStatusFilter;

    private void TriggerSearchUpdate()
    {
        _browseCoordinator.TriggerSearchUpdate(() => RefreshFilteredViews());
    }

    public bool ShowSafe
    {
        get => _shellState.ShowSafe;
        set => _shellState.ShowSafe = value;
    }

    public bool ShowAdvanced
    {
        get => _shellState.ShowAdvanced;
        set => _shellState.ShowAdvanced = value;
    }

    public bool ShowRisky
    {
        get => _shellState.ShowRisky;
        set => _shellState.ShowRisky = value;
    }

    public bool ShowFavoritesOnly
    {
        get => _shellState.ShowFavoritesOnly;
        set => _shellState.ShowFavoritesOnly = value;
    }

    public bool ShowContributorEvidenceUi => _showContributorEvidenceUi;

    public bool ShowClassA
    {
        get => _shellState.ShowClassA;
        set => _shellState.ShowClassA = value;
    }

    public bool ShowClassB
    {
        get => _shellState.ShowClassB;
        set => _shellState.ShowClassB = value;
    }

    public bool ShowClassC
    {
        get => _shellState.ShowClassC;
        set => _shellState.ShowClassC = value;
    }

    public bool ShowClassD
    {
        get => _shellState.ShowClassD;
        set => _shellState.ShowClassD = value;
    }

    public string SelectedCategoryName
    {
        get => _shellState.SelectedCategoryName;
        set => _shellState.SelectedCategoryName = value;
    }

    public bool IsAllCategoriesSelected => _shellState.IsAllCategoriesSelected;

    public string SelectedCategoryLabel => _shellState.SelectedCategoryLabel;

    public int FavoritesCount => Tweaks.Count(t => t.IsFavorite);

    public string FilterSummary => _presentationState.FilterSummary;

    public bool HasVisibleTweaks => _presentationState.HasVisibleTweaks;

    private bool CanRunBulkInspectable(Func<List<TweakItemViewModel>> getTweaks)
    {
        return _workspaceActionCoordinator.CanRunInspectable(IsBulkRunning, Tweaks, getTweaks);
    }

    private bool CanRunBulkMutating(Func<List<TweakItemViewModel>> getTweaks)
    {
        return _workspaceActionCoordinator.CanRunMutating(IsBulkRunning, Tweaks, getTweaks);
    }

    private async Task RunBulkAsync(
        string label,
        Func<List<TweakItemViewModel>> getTweaks,
        Func<TweakItemViewModel, CancellationToken, Task> runner)
    {
        await _workspaceActionCoordinator.RunBulkAsync(
            label,
            getTweaks,
            runner,
            () => IsBulkRunning,
            value => IsBulkRunning = value,
            value => BulkProgressCurrent = value,
            value => BulkProgressTotal = value,
            value => BulkStatusMessage = value,
            () => OnPropertyChanged(nameof(BulkProgressText)),
            RefreshSummaryStats);
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
        _workspaceActionCoordinator.SelectAll(TweaksView.Cast<TweakItemViewModel>());
    }

    private void DeselectAll()
    {
        _workspaceActionCoordinator.DeselectAll(Tweaks);
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
            _inventoryCoordinator.ScheduleSnapshotSave(Tweaks);
            if (HasStatusFilter)
            {
                RefreshFilteredViews();
            }
        }

    }

    private void OnTweakFavoriteChanged(TweakItemViewModel tweak, bool isFavorite)
    {
        _collectionCoordinator.HandleFavoriteChanged(tweak, isFavorite);

        OnPropertyChanged(nameof(FavoritesCount));

        // Refresh view if showing favorites only
        if (ShowFavoritesOnly)
        {
            RefreshFilteredViews();
        }
    }

    private void OnTweaksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _collectionCoordinator.HandleCollectionChanged(Tweaks, e, OnTweakPropertyChanged, OnTweakFavoriteChanged);

        UpdateSelectionCount();
        RaiseHealthMetricsChanged();
        RaiseWorkspaceMetricsChanged();
        RefreshSummaryStats();
    }

    private void RaiseHealthMetricsChanged()
    {
        _healthCoordinator.Refresh(Tweaks);
    }

    private void UpdateSelectionCount()
    {
        SelectedCount = _workspaceActionCoordinator.CountSelected(Tweaks);
    }

    private void CancelBulk()
    {
        _workspaceActionCoordinator.CancelBulk(IsBulkRunning, value => BulkStatusMessage = value);
    }

    private bool FilterTweaks(object obj)
    {
        if (obj is not TweakItemViewModel item)
        {
            return false;
        }

        return _browseCoordinator.FilterTweak(item);
    }

    private bool FilterRepairsRows(object obj)
    {
        if (obj is not RepairsItemViewModel item)
        {
            return false;
        }

        return _browseCoordinator.FilterRepair(item);
    }

    private void UpdateFilterSummary(bool rebuildCategoryGroups = true)
    {
        _browseCoordinator.RefreshPresentation(
            Tweaks,
            CurrentWorkspaceItemCount,
            TweaksView.Cast<object>().Count(),
            rebuildCategoryGroups,
            () => SelectedCategoryName = string.Empty);
        _previewAllCommand.RaiseCanExecuteChanged();
        _applyAllCommand.RaiseCanExecuteChanged();
        _verifyAllCommand.RaiseCanExecuteChanged();
        _rollbackAllCommand.RaiseCanExecuteChanged();
    }

    private ConfigurationWorkspaceKind GetWorkspaceKind(TweakItemViewModel tweak)
        => _browseCoordinator.GetWorkspaceKind(tweak);

    private bool CurrentWorkspaceContainsCategory(string categoryName)
        => _browseCoordinator.CurrentWorkspaceContainsCategory(Tweaks, categoryName);

    private void OnShellStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName))
        {
            return;
        }

        OnPropertyChanged(e.PropertyName);

        switch (e.PropertyName)
        {
            case nameof(TweaksShellStateViewModel.SearchText):
                TriggerSearchUpdate();
                break;
            case nameof(TweaksShellStateViewModel.StatusFilter):
            case nameof(TweaksShellStateViewModel.ShowSafe):
            case nameof(TweaksShellStateViewModel.ShowAdvanced):
            case nameof(TweaksShellStateViewModel.ShowRisky):
            case nameof(TweaksShellStateViewModel.ShowFavoritesOnly):
            case nameof(TweaksShellStateViewModel.ShowClassA):
            case nameof(TweaksShellStateViewModel.ShowClassB):
            case nameof(TweaksShellStateViewModel.ShowClassC):
            case nameof(TweaksShellStateViewModel.ShowClassD):
            case nameof(TweaksShellStateViewModel.IsFlatView):
                TweaksView.Refresh();
                RepairsRowsView.Refresh();
                UpdateFilterSummary();
                break;
            case nameof(TweaksShellStateViewModel.SelectedCategoryName):
                OnPropertyChanged(nameof(ShowDnsConfigurationPanel));
                RefreshFilteredViews(rebuildCategoryGroups: false);
                break;
            case nameof(TweaksShellStateViewModel.SelectedWorkspace):
                if (!string.IsNullOrWhiteSpace(SelectedCategoryName) && !CurrentWorkspaceContainsCategory(SelectedCategoryName))
                {
                    SelectedCategoryName = string.Empty;
                }

                RaiseWorkspaceMetricsChanged();
                OnPropertyChanged(nameof(ShowDnsConfigurationPanel));
                RefreshFilteredViews();
                break;
        }
    }

    private void OnPresentationStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName))
        {
            return;
        }

        OnPropertyChanged(e.PropertyName);
    }

    private void OnSupportCoordinatorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName))
        {
            return;
        }

        OnPropertyChanged(e.PropertyName);
    }

    private void OnHealthCoordinatorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName))
        {
            return;
        }

        OnPropertyChanged(e.PropertyName);
    }

    private void OnOperationsCoordinatorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName))
        {
            return;
        }

        OnPropertyChanged(e.PropertyName);

        if (e.PropertyName == nameof(WorkspaceOperationsCoordinator.IsExporting))
        {
            _exportLogsCommand.RaiseCanExecuteChanged();
        }
    }

    private void OnInventoryCoordinatorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName))
        {
            return;
        }

        OnPropertyChanged(e.PropertyName);
    }

    private void RaiseWorkspaceMetricsChanged()
    {
        OnPropertyChanged(nameof(SettingsWorkspaceCount));
        OnPropertyChanged(nameof(MaintenanceWorkspaceCount));
        OnPropertyChanged(nameof(CurrentWorkspaceItemCount));
        OnPropertyChanged(nameof(CurrentWorkspaceCountLabel));
        OnPropertyChanged(nameof(ShowDnsConfigurationPanel));
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
        _shellState.ResetFilters();
    }

    private void RefreshFilteredViews(bool rebuildCategoryGroups = true)
    {
        TweaksView.Refresh();
        RepairsRowsView.Refresh();
        UpdateFilterSummary(rebuildCategoryGroups);
    }

    private void RefreshSummaryStats()
    {
        _presentationState.SetInventoryCounts(
            Tweaks.Count(t => t.ShowInApp),
            Tweaks.Count(t => t.ShowInApp && t.IsApplied),
            Tweaks.Count(t => t.ShowInApp && t.WasRolledBack));
        RaiseWorkspaceMetricsChanged();
        RefreshLogFileSize();
        _inventoryCoordinator.UpdateStatusMessage(Tweaks);
    }

    internal void FocusMaintenanceWorkspace(string categoryName)
    {
        SelectedWorkspace = ConfigurationWorkspaceKind.Maintenance;
        SelectedCategoryName = categoryName;
        StatusFilter = string.Empty;
        SearchText = string.Empty;
        ShowFavoritesOnly = false;
        ShowSafe = true;
        ShowAdvanced = true;
        ShowRisky = true;
        RefreshFilteredViews();
    }

    internal void SetBulkStatusFromRepairs(string message)
    {
        BulkStatusMessage = message;
    }

    internal void ClearSelectionFromRepairs()
    {
        DeselectAll();
    }

    internal ConfigurationWorkspaceKind GetWorkspaceKindForRepairs(TweakItemViewModel tweak)
    {
        return GetWorkspaceKind(tweak);
    }

    internal Task RunRepairsBatchAsync(
        string label,
        Func<List<TweakItemViewModel>> getTweaks,
        Func<TweakItemViewModel, CancellationToken, Task> runner)
    {
        return RunBulkAsync(label, getTweaks, runner);
    }

    private void RefreshLogFileSize()
    {
        _operationsCoordinator.RefreshLogFileSize();
    }

    private void SetDetailsExpanded(bool isExpanded)
    {
        foreach (var item in Tweaks)
        {
            item.IsDetailsExpanded = isExpanded;
        }
    }

    private void SetBulkLock(bool isLocked)
    {
        foreach (var item in Tweaks)
        {
            item.IsBulkLocked = isLocked;
        }
    }

    public async Task DetectAllTweaksAsync(
        CancellationToken ct = default,
        bool forceRedetect = false,
        bool skipElevationPrompts = false,
        bool skipExpensiveOperations = false)
    {
        IEnumerable<TweakItemViewModel> candidates = Tweaks;

        if (skipExpensiveOperations)
        {
            candidates = candidates.Where(t => t.IsScanFriendly);
        }

        if (skipElevationPrompts)
        {
            candidates = candidates.Where(t => !t.WillPromptForDetect);
        }

        var tweaksToScan = candidates.ToList();
        var perTweakTimeout = TimeSpan.FromSeconds(6);

        foreach (var tweak in tweaksToScan)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
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
        _inventoryCoordinator.SaveSnapshot(Tweaks);
        _inventoryCoordinator.UpdateStatusMessage(Tweaks);

        _healthCoordinator.Refresh(Tweaks);

        if (!skipElevationPrompts && !skipExpensiveOperations)
        {
            foreach (var category in CategoryGroups)
            {
                category.MarkDetected();
            }
        }
    }

    public async Task RefreshInventoryInBackgroundAsync(CancellationToken ct = default)
    {
        await _inventoryCoordinator.RunBackgroundRefreshAsync(
            Tweaks,
            async token => await DetectAllTweaksAsync(
                ct: token,
                forceRedetect: true,
                skipElevationPrompts: true,
                skipExpensiveOperations: false),
            ct);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        // Dispose CancellationTokenSources
        _browseCoordinator.Dispose();
        _collectionCoordinator.Dispose(OnTweakPropertyChanged, OnTweakFavoriteChanged);
        _workspaceActionCoordinator.Dispose();
        _inventoryCoordinator.Dispose();

        // Unsubscribe collection changed events
        _shellState.PropertyChanged -= OnShellStatePropertyChanged;
        _presentationState.PropertyChanged -= OnPresentationStatePropertyChanged;
        _healthCoordinator.PropertyChanged -= OnHealthCoordinatorPropertyChanged;
        _inventoryCoordinator.PropertyChanged -= OnInventoryCoordinatorPropertyChanged;
        _supportCoordinator.PropertyChanged -= OnSupportCoordinatorPropertyChanged;
        _operationsCoordinator.PropertyChanged -= OnOperationsCoordinatorPropertyChanged;
        Tweaks.CollectionChanged -= OnTweaksCollectionChanged;

    }
}
