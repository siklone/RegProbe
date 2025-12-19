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
    private readonly RelayCommand _detectCommand;
    private readonly RelayCommand _previewCommand;
    private readonly RelayCommand _applyCommand;
    private readonly RelayCommand _verifyCommand;
    private readonly RelayCommand _rollbackCommand;
    private readonly RelayCommand _cancelCommand;
    private readonly RelayCommand _resetCommand;
    private readonly RelayCommand _copyIdCommand;
    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private bool _isBulkLocked;
    private string _statusMessage = "Idle";
    private string _lastUpdatedText = "Last update: -";
    private string _lastDurationText = "Duration: -";
    private string _lastActionText = string.Empty;
    private TweakRunOutcome _lastOutcome = TweakRunOutcome.None;
    private int _runCount;
    private bool _isDetailsExpanded = true;

    public TweakItemViewModel(ITweak tweak, TweakExecutionPipeline pipeline)
    {
        _tweak = tweak ?? throw new ArgumentNullException(nameof(tweak));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));

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
        _resetCommand = new RelayCommand(_ => ResetStatus(), _ => CanReset());
        _copyIdCommand = new RelayCommand(_ => CopyId());
    }

    public string Name => _tweak.Name;

    public string Id => _tweak.Id;

    public string Description => _tweak.Description;

    public TweakRiskLevel Risk => _tweak.Risk;

    public ObservableCollection<TweakStepStatusViewModel> Steps { get; }

    public ICommand DetectCommand => _detectCommand;

    public ICommand PreviewCommand => _previewCommand;

    public ICommand ApplyCommand => _applyCommand;

    public ICommand VerifyCommand => _verifyCommand;

    public ICommand RollbackCommand => _rollbackCommand;

    public ICommand CancelCommand => _cancelCommand;

    public ICommand ResetCommand => _resetCommand;

    public ICommand CopyIdCommand => _copyIdCommand;

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

    internal bool SuppressRiskConfirmation { get; set; }

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

    public string LastDurationText
    {
        get => _lastDurationText;
        private set => SetProperty(ref _lastDurationText, value);
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
                OnPropertyChanged(nameof(OutcomeSortRank));

                if (value == TweakRunOutcome.Failed)
                {
                    IsDetailsExpanded = true;
                }
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

    public int OutcomeSortRank => LastOutcome switch
    {
        TweakRunOutcome.Failed => 0,
        TweakRunOutcome.InProgress => 1,
        TweakRunOutcome.Cancelled => 2,
        TweakRunOutcome.Skipped => 3,
        TweakRunOutcome.Success => 4,
        _ => 5
    };

    public string OutcomeSummary => HasOutcome
        ? $"{LastActionText} - {OutcomeText}"
        : "No runs yet";

    public string StepProgressText => $"Steps: {GetCompletedStepCount()}/{Steps.Count}";

    public int RunCount
    {
        get => _runCount;
        private set => SetProperty(ref _runCount, value);
    }

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

        if (!dryRun && !ConfirmRiskyAction("apply"))
        {
            LastActionText = "Apply";
            LastOutcome = TweakRunOutcome.Cancelled;
            StatusMessage = "Apply cancelled.";
            LastUpdatedText = "Last update: -";
            return;
        }

        StartCancellation(ct);
        IsRunning = true;
        RunCount++;
        var startedAt = DateTimeOffset.UtcNow;
        var actionLabel = dryRun ? "Preview" : "Apply";
        LastActionText = actionLabel;
        LastOutcome = TweakRunOutcome.InProgress;
        StatusMessage = dryRun ? "Preview run started." : "Apply run started.";
        LastUpdatedText = "Last update: -";
        LastDurationText = "Duration: -";
        ResetSteps();
        Steps.First().MarkInProgress();
        OnPropertyChanged(nameof(StepProgressText));

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
            LastDurationText = $"Duration: {FormatDuration(report.CompletedAt - report.StartedAt)}";
        }
        catch (OperationCanceledException)
        {
            LastOutcome = TweakRunOutcome.Cancelled;
            StatusMessage = "Run cancelled.";
            LastDurationText = $"Duration: {FormatDuration(DateTimeOffset.UtcNow - startedAt)}";
        }
        catch (Exception ex)
        {
            LastOutcome = TweakRunOutcome.Failed;
            StatusMessage = $"Run failed: {ex.Message}";
            LastDurationText = $"Duration: {FormatDuration(DateTimeOffset.UtcNow - startedAt)}";
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

        if (action == TweakAction.Rollback && !ConfirmRiskyAction("rollback"))
        {
            LastActionText = action.ToString();
            LastOutcome = TweakRunOutcome.Cancelled;
            StatusMessage = "Rollback cancelled.";
            LastUpdatedText = "Last update: -";
            return;
        }

        StartCancellation(ct);
        IsRunning = true;
        RunCount++;
        var startedAt = DateTimeOffset.UtcNow;
        LastActionText = action.ToString();
        LastOutcome = TweakRunOutcome.InProgress;
        StatusMessage = $"{action} started.";
        var step = Steps.FirstOrDefault(item => item.Action == action);
        step?.MarkInProgress();
        OnPropertyChanged(nameof(StepProgressText));

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
            OnPropertyChanged(nameof(StepProgressText));
            LastOutcome = MapOutcome(result.Result.Status);
            StatusMessage = $"{action} {result.Result.Status}.";
            LastUpdatedText = $"Last update: {result.Result.Timestamp.ToLocalTime():HH:mm:ss}";
            LastDurationText = $"Duration: {FormatDuration(DateTimeOffset.UtcNow - startedAt)}";
        }
        catch (OperationCanceledException)
        {
            LastOutcome = TweakRunOutcome.Cancelled;
            StatusMessage = $"{action} cancelled.";
            LastDurationText = $"Duration: {FormatDuration(DateTimeOffset.UtcNow - startedAt)}";
        }
        catch (Exception ex)
        {
            LastOutcome = TweakRunOutcome.Failed;
            StatusMessage = $"{action} failed: {ex.Message}";
            LastDurationText = $"Duration: {FormatDuration(DateTimeOffset.UtcNow - startedAt)}";
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
        OnPropertyChanged(nameof(StepProgressText));

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
        OnPropertyChanged(nameof(StepProgressText));
    }

    private void ResetSteps()
    {
        foreach (var step in Steps)
        {
            step.Reset();
        }
        OnPropertyChanged(nameof(StepProgressText));
    }

    public void ResetStatus()
    {
        if (IsRunning)
        {
            return;
        }

        ResetSteps();
        LastActionText = string.Empty;
        LastOutcome = TweakRunOutcome.None;
        StatusMessage = "Idle";
        LastUpdatedText = "Last update: -";
        LastDurationText = "Duration: -";
        RunCount = 0;
        IsDetailsExpanded = false;
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

    private bool ConfirmRiskyAction(string actionLabel)
    {
        if (SuppressRiskConfirmation)
        {
            return true;
        }

        if (_tweak.Risk != TweakRiskLevel.Risky)
        {
            return true;
        }

        var result = MessageBox.Show(
            $"This tweak is marked Risky. Proceed with {actionLabel}?",
            "Confirm risky tweak",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        return result == MessageBoxResult.Yes;
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1)
        {
            return $"{duration.TotalMilliseconds:0} ms";
        }

        if (duration.TotalMinutes < 1)
        {
            return $"{duration.TotalSeconds:0.0} s";
        }

        return $"{duration.TotalMinutes:0.0} min";
    }

    private int GetCompletedStepCount()
    {
        return Steps.Count(step => step.State != TweakStepState.Pending && step.State != TweakStepState.InProgress);
    }

    private bool CanRun()
    {
        return !IsRunning && !IsBulkLocked;
    }

    private bool CanCancel()
    {
        return IsRunning && !IsBulkLocked;
    }

    private bool CanReset()
    {
        return !IsRunning && !IsBulkLocked;
    }

    private void UpdateCommandStates()
    {
        _detectCommand.RaiseCanExecuteChanged();
        _previewCommand.RaiseCanExecuteChanged();
        _applyCommand.RaiseCanExecuteChanged();
        _verifyCommand.RaiseCanExecuteChanged();
        _rollbackCommand.RaiseCanExecuteChanged();
        _cancelCommand.RaiseCanExecuteChanged();
        _resetCommand.RaiseCanExecuteChanged();
    }
}
