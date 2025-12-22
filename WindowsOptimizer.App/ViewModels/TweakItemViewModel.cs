using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine;

namespace WindowsOptimizer.App.ViewModels;

public sealed class TweakItemViewModel : ViewModelBase
{
    private readonly ITweak _tweak;
    private readonly TweakExecutionPipeline _pipeline;
    private readonly bool _isElevated;
    private readonly RelayCommand _detectCommand;
    private readonly RelayCommand _previewCommand;
    private readonly RelayCommand _applyCommand;
    private readonly RelayCommand _verifyCommand;
    private readonly RelayCommand _rollbackCommand;
    private readonly RelayCommand _cancelCommand;
    private readonly RelayCommand _copyIdCommand;
    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private bool _isBulkLocked;
    private string _statusMessage = "Idle";
    private string _lastUpdatedText = "Last update: -";
    private string _lastActionText = string.Empty;
    private TweakRunOutcome _lastOutcome = TweakRunOutcome.None;
    private bool _isDetailsExpanded = false;
    private TweakAppliedStatus _appliedStatus = TweakAppliedStatus.Unknown;
    private bool _isToggleEnabled = true;
    private readonly RelayCommand _toggleCommand;

    public TweakItemViewModel(ITweak tweak, TweakExecutionPipeline pipeline, bool isElevated)
    {
        _tweak = tweak ?? throw new ArgumentNullException(nameof(tweak));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _isElevated = isElevated;

        Steps = new ObservableCollection<TweakStepStatusViewModel>
        {
            new(TweakAction.Detect),
            new(TweakAction.Apply),
            new(TweakAction.Verify),
            new(TweakAction.Rollback)
        };

        ResetSteps();

        _detectCommand = new RelayCommand(_ => _ = RunSingleStepAsync(TweakAction.Detect, CancellationToken.None), _ => CanRun());
        _previewCommand = new RelayCommand(_ => _ = RunAsync(true, CancellationToken.None), _ => CanRun());
        _applyCommand = new RelayCommand(_ => _ = RunAsync(false, CancellationToken.None), _ => CanRun());
        _verifyCommand = new RelayCommand(_ => _ = RunSingleStepAsync(TweakAction.Verify, CancellationToken.None), _ => CanRun());
        _rollbackCommand = new RelayCommand(_ => _ = RunSingleStepAsync(TweakAction.Rollback, CancellationToken.None), _ => CanRun());
        _cancelCommand = new RelayCommand(_ => CancelRun(), _ => CanCancel());
        _copyIdCommand = new RelayCommand(_ => CopyId());
        _toggleCommand = new RelayCommand(_ => _ = ToggleAsync(), _ => CanToggle());
    }

    public string Name => _tweak.Name;

    public string Id => _tweak.Id;

    public string Description => _tweak.Description;

    public TweakRiskLevel Risk => _tweak.Risk;

    public bool RequiresElevation => _tweak.RequiresElevation;

    public bool IsElevated => _isElevated;

    public bool WillPromptForElevation => RequiresElevation && !IsElevated;

    public string ElevationBadgeText => "Admin required";

    public string ElevationTooltip => IsElevated
        ? "Requires administrator privileges."
        : "Requires administrator privileges. You'll be prompted to approve the action.";

    public string ElevationWarningText => WillPromptForElevation
        ? "Requires elevation. Approve the UAC prompt to continue."
        : string.Empty;

    public string Category => ExtractCategory(Id);

    public string CategoryIcon => GetCategoryIcon(Category);

    private static string ExtractCategory(string id)
    {
        var dotIndex = id.IndexOf('.');
        if (dotIndex <= 0) return "Other";
        var cat = id.Substring(0, dotIndex);
        return char.ToUpper(cat[0]) + cat.Substring(1).ToLowerInvariant();
    }

    private static string GetCategoryIcon(string category) => category.ToLowerInvariant() switch
    {
        "system" => "⚙️",
        "security" => "🔒",
        "privacy" => "👁️",
        "network" => "🌐",
        "visibility" => "👀",
        "audio" => "🔊",
        "peripheral" => "🖱️",
        "power" => "⚡",
        "performance" => "🚀",
        "cleanup" => "🧹",
        "explorer" => "📁",
        "notifications" => "🔔",
        _ => "📦"
    };

    public ObservableCollection<TweakStepStatusViewModel> Steps { get; }

    public ICommand DetectCommand => _detectCommand;

    public ICommand PreviewCommand => _previewCommand;

    public ICommand ApplyCommand => _applyCommand;

    public ICommand VerifyCommand => _verifyCommand;

    public ICommand RollbackCommand => _rollbackCommand;

    public ICommand CancelCommand => _cancelCommand;

    public ICommand CopyIdCommand => _copyIdCommand;

    public ICommand ToggleCommand => _toggleCommand;

    // Simplified status for first-glance view
    public TweakAppliedStatus AppliedStatus
    {
        get => _appliedStatus;
        private set
        {
            if (SetProperty(ref _appliedStatus, value))
            {
                OnPropertyChanged(nameof(IsApplied));
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(StatusText));
                _toggleCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsApplied => AppliedStatus == TweakAppliedStatus.Applied;

    public string StatusIcon => AppliedStatus switch
    {
        TweakAppliedStatus.Applied => "✓",
        TweakAppliedStatus.NotApplied => "○",
        TweakAppliedStatus.Error => "✕",
        _ => "?"
    };

    public string StatusColor => AppliedStatus switch
    {
        TweakAppliedStatus.Applied => "#A3BE8C",
        TweakAppliedStatus.NotApplied => "#4C566A",
        TweakAppliedStatus.Error => "#BF616A",
        _ => "#EBCB8B"
    };

    public string StatusText => AppliedStatus switch
    {
        TweakAppliedStatus.Applied => "Applied",
        TweakAppliedStatus.NotApplied => "Not Applied",
        TweakAppliedStatus.Error => "Error",
        _ => "Unknown"
    };

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public bool IsBulkLocked
    {
        get => _isBulkLocked;
        set
        {
            if (SetProperty(ref _isBulkLocked, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string LastUpdatedText
    {
        get => _lastUpdatedText;
        private set => SetProperty(ref _lastUpdatedText, value);
    }

    public string LastActionText
    {
        get => _lastActionText;
        private set
        {
            if (SetProperty(ref _lastActionText, value))
            {
                OnPropertyChanged(nameof(OutcomeSummary));
            }
        }
    }

    public TweakRunOutcome LastOutcome
    {
        get => _lastOutcome;
        private set
        {
            if (SetProperty(ref _lastOutcome, value))
            {
                OnPropertyChanged(nameof(HasOutcome));
                OnPropertyChanged(nameof(OutcomeText));
                OnPropertyChanged(nameof(OutcomeSummary));
            }
        }
    }

    public bool HasOutcome => LastOutcome != TweakRunOutcome.None;

    public string OutcomeText => LastOutcome switch
    {
        TweakRunOutcome.InProgress => "Running",
        TweakRunOutcome.Success => "Success",
        TweakRunOutcome.Failed => "Failed",
        TweakRunOutcome.Cancelled => "Cancelled",
        TweakRunOutcome.Skipped => "Skipped",
        _ => "Idle"
    };

    public string OutcomeSummary => HasOutcome
        ? $"{LastActionText} - {OutcomeText}"
        : "No runs yet";

    public bool IsDetailsExpanded
    {
        get => _isDetailsExpanded;
        set => SetProperty(ref _isDetailsExpanded, value);
    }

    public Task RunPreviewAsync(CancellationToken ct) => RunAsync(true, ct);

    public Task RunApplyAsync(CancellationToken ct) => RunAsync(false, ct);

    public Task RunDetectAsync(CancellationToken ct) => RunSingleStepAsync(TweakAction.Detect, ct);

    public Task RunVerifyAsync(CancellationToken ct) => RunSingleStepAsync(TweakAction.Verify, ct);

    public Task RunRollbackAsync(CancellationToken ct) => RunSingleStepAsync(TweakAction.Rollback, ct);

    private async Task RunAsync(bool dryRun, CancellationToken ct)
    {
        if (IsRunning)
        {
            return;
        }

        StartCancellation(ct);
        var actionLabel = dryRun ? "Preview" : "Apply";

        IsRunning = true;
        LastActionText = actionLabel;
        LastOutcome = TweakRunOutcome.InProgress;
        StatusMessage = dryRun ? "Preview run started." : "Apply run started.";
        LastUpdatedText = "Last update: -";
        ResetSteps();
        Steps.First().MarkInProgress();

        var progress = new Progress<TweakExecutionUpdate>(OnProgressUpdate);
        var options = new TweakExecutionOptions
        {
            DryRun = dryRun,
            VerifyAfterApply = true,
            RollbackOnFailure = true
        };

        try
        {
            var report = await _pipeline.ExecuteAsync(_tweak, options, progress, _cts?.Token ?? CancellationToken.None);
            ApplyReport(report);
            LastOutcome = report.Succeeded ? TweakRunOutcome.Success : TweakRunOutcome.Failed;
            StatusMessage = report.Succeeded ? "Run completed." : "Run completed with errors.";
            LastUpdatedText = $"Last update: {report.CompletedAt.ToLocalTime():HH:mm:ss}";
        }
        catch (OperationCanceledException)
        {
            LastOutcome = TweakRunOutcome.Cancelled;
            StatusMessage = "Run cancelled.";
        }
        catch (Exception ex)
        {
            LastOutcome = TweakRunOutcome.Failed;
            StatusMessage = $"Run failed: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
            ClearCancellation();
        }
    }

    private async Task RunSingleStepAsync(TweakAction action, CancellationToken ct)
    {
        if (IsRunning)
        {
            return;
        }

        StartCancellation(ct);

        IsRunning = true;
        LastActionText = action.ToString();
        LastOutcome = TweakRunOutcome.InProgress;
        StatusMessage = $"{action} started.";
        var step = Steps.FirstOrDefault(item => item.Action == action);
        step?.MarkInProgress();

        try
        {
            var updateProgress = new Progress<TweakExecutionUpdate>(update =>
            {
                if (update.Action == action)
                {
                    step?.ApplyResult(update.Status, update.Message, update.Timestamp);
                }
            });

            var result = await _pipeline.ExecuteStepAsync(_tweak, action, updateProgress, _cts?.Token ?? ct);
            step?.ApplyResult(result.Result.Status, result.Result.Message, result.Result.Timestamp);
            LastOutcome = MapOutcome(result.Result.Status);
            StatusMessage = $"{action} {result.Result.Status}.";
            LastUpdatedText = $"Last update: {result.Result.Timestamp.ToLocalTime():HH:mm:ss}";
        }
        catch (OperationCanceledException)
        {
            LastOutcome = TweakRunOutcome.Cancelled;
            StatusMessage = $"{action} cancelled.";
        }
        catch (Exception ex)
        {
            LastOutcome = TweakRunOutcome.Failed;
            StatusMessage = $"{action} failed: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
            ClearCancellation();
        }
    }

    private void CancelRun()
    {
        if (!IsRunning || _cts is null)
        {
            return;
        }

        _cts.Cancel();
        StatusMessage = "Cancellation requested.";
    }

    private void StartCancellation(CancellationToken ct)
    {
        ClearCancellation();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    }

    private void ClearCancellation()
    {
        _cts?.Dispose();
        _cts = null;
    }

    private static TweakRunOutcome MapOutcome(TweakStatus status)
    {
        return status switch
        {
            TweakStatus.Failed => TweakRunOutcome.Failed,
            TweakStatus.Skipped => TweakRunOutcome.Skipped,
            TweakStatus.NotApplicable => TweakRunOutcome.Skipped,
            _ => TweakRunOutcome.Success
        };
    }

    private void OnProgressUpdate(TweakExecutionUpdate update)
    {
        var step = Steps.FirstOrDefault(item => item.Action == update.Action);
        step?.ApplyResult(update.Status, update.Message, update.Timestamp);

        StatusMessage = $"{update.Action}: {update.Status}";
        LastUpdatedText = $"Last update: {update.Timestamp.ToLocalTime():HH:mm:ss}";

        var nextStep = GetNextStep(update.Action);
        if (nextStep is not null && nextStep.State == TweakStepState.Pending)
        {
            nextStep.MarkInProgress();
        }
    }

    private void ApplyReport(TweakExecutionReport report)
    {
        foreach (var step in Steps)
        {
            var reportStep = report.Steps.FirstOrDefault(item => item.Action == step.Action);
            if (reportStep is null)
            {
                step.MarkNotRequired("Step not executed.");
                continue;
            }

            step.ApplyResult(reportStep.Result.Status, reportStep.Result.Message, reportStep.Result.Timestamp);
        }
    }

    private void ResetSteps()
    {
        foreach (var step in Steps)
        {
            step.Reset();
        }
    }

    private TweakStepStatusViewModel? GetNextStep(TweakAction action)
    {
        for (var i = 0; i < Steps.Count - 1; i++)
        {
            if (Steps[i].Action == action)
            {
                return Steps[i + 1];
            }
        }

        return null;
    }

    private void CopyId()
    {
        try
        {
            Clipboard.SetText(Id);
            StatusMessage = "Tweak ID copied to clipboard.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Copy failed: {ex.Message}";
        }
    }

    private bool CanRun()
    {
        return !IsRunning && !IsBulkLocked;
    }

    private bool CanCancel()
    {
        return IsRunning && !IsBulkLocked;
    }

    private void UpdateCommandStates()
    {
        _detectCommand.RaiseCanExecuteChanged();
        _previewCommand.RaiseCanExecuteChanged();
        _applyCommand.RaiseCanExecuteChanged();
        _verifyCommand.RaiseCanExecuteChanged();
        _rollbackCommand.RaiseCanExecuteChanged();
        _cancelCommand.RaiseCanExecuteChanged();
        _toggleCommand.RaiseCanExecuteChanged();
    }

    private bool CanToggle()
    {
        return !IsRunning && !IsBulkLocked && AppliedStatus != TweakAppliedStatus.Unknown;
    }

    /// <summary>
    /// Toggle the tweak: Apply if not applied, Rollback if applied
    /// </summary>
    private async Task ToggleAsync()
    {
        if (!CanToggle()) return;

        if (AppliedStatus == TweakAppliedStatus.Applied)
        {
            await RunRollbackAsync(CancellationToken.None);
            await DetectStatusAsync();
        }
        else
        {
            await RunApplyAsync(CancellationToken.None);
            await DetectStatusAsync();
        }
    }

    /// <summary>
    /// Detect if tweak is currently applied
    /// </summary>
    public async Task DetectStatusAsync()
    {
        if (IsRunning) return;

        try
        {
            var result = await _pipeline.ExecuteStepAsync(_tweak, TweakAction.Detect, null, CancellationToken.None);
            
            // Interpret detect result to determine applied status
            if (result.Result.Status == TweakStatus.Detected)
            {
                AppliedStatus = TweakAppliedStatus.Applied;
            }
            else if (result.Result.Status == TweakStatus.NotDetected)
            {
                AppliedStatus = TweakAppliedStatus.NotApplied;
            }
            else if (result.Result.Status == TweakStatus.Failed)
            {
                AppliedStatus = TweakAppliedStatus.Error;
            }
            else
            {
                AppliedStatus = TweakAppliedStatus.NotApplied;
            }
        }
        catch
        {
            AppliedStatus = TweakAppliedStatus.Unknown;
        }
    }
}

/// <summary>
/// Simplified status for first-glance view
/// </summary>
public enum TweakAppliedStatus
{
    Unknown,
    Applied,
    NotApplied,
    Error
}
