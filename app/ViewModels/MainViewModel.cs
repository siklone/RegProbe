using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using RegProbe.App.Services;
using RegProbe.App.Utilities;

namespace RegProbe.App.ViewModels;

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly MainCompositionCoordinator _compositionCoordinator;
    private readonly MainRecoveryCoordinator _recoveryCoordinator;
    private readonly MainShellCoordinator _shellCoordinator;
    private readonly TweaksViewModel _workspaceViewModel;
    private readonly string _hostContextLabel;
    private readonly ObservableCollection<ShellStatusStripToken> _statusStripTokens = [];

    public MainViewModel()
    {
        LogToFile("========== APPLICATION STARTED ==========");
        _compositionCoordinator = new MainCompositionCoordinator(LogToFile);
        _workspaceViewModel = _compositionCoordinator.WorkspaceViewModel;
        _recoveryCoordinator = _compositionCoordinator.RecoveryCoordinator;
        _recoveryCoordinator.PropertyChanged += OnRecoveryCoordinatorPropertyChanged;
        _workspaceViewModel.PropertyChanged += OnWorkspaceViewModelPropertyChanged;

        _shellCoordinator = _compositionCoordinator.ShellCoordinator;
        _shellCoordinator.PropertyChanged += OnShellCoordinatorPropertyChanged;
        _hostContextLabel = BuildHostContextLabel(OsDetectionResolver.Resolve(includeWmiCrossCheck: false));
        RefreshStatusStripTokens();
        _compositionCoordinator.Initialize();
    }

    public IBusyService BusyService => _compositionCoordinator.BusyService;

    public string AppVersionLabel => AppInfo.VersionLabel;

    public string AppCopyrightLabel => AppInfo.CopyrightLabel;

    public ICommand RecoverPendingRollbacksCommand => _recoveryCoordinator.RecoverPendingRollbacksCommand;

    public ICommand DismissPendingRollbacksCommand => _recoveryCoordinator.DismissPendingRollbacksCommand;

    public RelayCommand ShowRepairsCommand => _shellCoordinator.ShowRepairsCommand;

    public RelayCommand ShowConfigurationCommand => _shellCoordinator.ShowConfigurationCommand;

    public RelayCommand ShowAboutCommand => _shellCoordinator.ShowAboutCommand;

    public RelayCommand FocusSearchCommand => _shellCoordinator.FocusSearchCommand;

    public RelayCommand ClearFiltersCommand => _shellCoordinator.ClearFiltersCommand;

    public ViewModelBase? CurrentViewModel => _shellCoordinator.CurrentViewModel;

    public bool IsConfigurationViewActive => _shellCoordinator.IsConfigurationViewActive;

    public bool IsRepairsViewActive => _shellCoordinator.IsRepairsViewActive;

    public bool IsAboutViewActive => _shellCoordinator.IsAboutViewActive;

    public string HostContextLabel => _hostContextLabel;

    public bool CanUseStagingEnvironment => false;

    public bool CanFocusSearch => !IsAboutViewActive;

    public string FocusSearchLabel => CanFocusSearch ? "Search" : "Search unavailable";

    public string StagingEnvironmentTooltip => "Staging is not backed in this build.";

    public ObservableCollection<ShellStatusStripToken> StatusStripTokens => _statusStripTokens;

    public bool HasPendingRollbacks => _recoveryCoordinator.HasPendingRollbacks;

    public int PendingRollbackCount => _recoveryCoordinator.PendingRollbackCount;

    public string PendingRollbackMessage => _recoveryCoordinator.PendingRollbackMessage;

    public bool IsRecovering => _recoveryCoordinator.IsRecovering;

    public void Dispose()
    {
        _recoveryCoordinator.PropertyChanged -= OnRecoveryCoordinatorPropertyChanged;
        _workspaceViewModel.PropertyChanged -= OnWorkspaceViewModelPropertyChanged;
        _shellCoordinator.PropertyChanged -= OnShellCoordinatorPropertyChanged;
        _compositionCoordinator.Dispose();
    }

    private void OnRecoveryCoordinatorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName))
        {
            return;
        }

        OnPropertyChanged(e.PropertyName);
        RefreshStatusStripTokens();
    }

    private void OnShellCoordinatorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName))
        {
            return;
        }

        OnPropertyChanged(e.PropertyName);
        OnPropertyChanged(nameof(CanFocusSearch));
        OnPropertyChanged(nameof(FocusSearchLabel));
        RefreshStatusStripTokens();
    }

    private void OnWorkspaceViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName))
        {
            return;
        }

        RefreshStatusStripTokens();
    }

    private void RefreshStatusStripTokens()
    {
        _statusStripTokens.Clear();

        if (IsConfigurationViewActive)
        {
            _statusStripTokens.Add(new ShellStatusStripToken(_workspaceViewModel.WorkspaceVerificationStripText, ResolveTone(_workspaceViewModel.WorkspaceVerificationSummaryState)));
            _statusStripTokens.Add(new ShellStatusStripToken(_workspaceViewModel.WorkspaceRiskStripText, ResolveRiskTone(_workspaceViewModel.WorkspaceRiskStripText)));
            _statusStripTokens.Add(new ShellStatusStripToken(_workspaceViewModel.WorkspacePendingStripText, ResolveTone(_workspaceViewModel.WorkspacePendingSummaryState)));
            _statusStripTokens.Add(new ShellStatusStripToken(_workspaceViewModel.WorkspaceRollbackStripText, ResolveTone(_workspaceViewModel.WorkspaceRollbackSummaryState)));
            return;
        }

        if (IsRepairsViewActive)
        {
            _statusStripTokens.Add(new ShellStatusStripToken("Recovery", "neutral"));
            _statusStripTokens.Add(new ShellStatusStripToken($"{_workspaceViewModel.MaintenanceWorkspaceCount} actions", "neutral"));
            _statusStripTokens.Add(new ShellStatusStripToken(_workspaceViewModel.InventoryStatusMessage, "info"));
            _statusStripTokens.Add(new ShellStatusStripToken(_workspaceViewModel.IsBulkRunning ? "Running now" : "Ready", _workspaceViewModel.IsBulkRunning ? "info" : "ok"));
            return;
        }

        _statusStripTokens.Add(new ShellStatusStripToken("Diagnostics", "neutral"));
        _statusStripTokens.Add(new ShellStatusStripToken(AppVersionLabel, "neutral"));
        _statusStripTokens.Add(new ShellStatusStripToken(HostContextLabel, "info"));
        _statusStripTokens.Add(new ShellStatusStripToken("Logs available", "ok"));
    }

    private static string ResolveTone(string state) => state switch
    {
        "ok" => "ok",
        "attention" => "info",
        "warning" => "warning",
        _ => "neutral"
    };

    private static string ResolveRiskTone(string riskText) => riskText switch
    {
        "Low risk" => "ok",
        "Managed risk" => "info",
        _ => "warning"
    };

    private static string BuildHostContextLabel(OsDetectionResult result)
    {
        if (result is null)
        {
            return "Windows host";
        }

        return $"{result.NormalizedName} • build {result.BuildNumber}";
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
