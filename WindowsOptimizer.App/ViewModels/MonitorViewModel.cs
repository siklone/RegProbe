using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Infrastructure;

namespace WindowsOptimizer.App.ViewModels;

public sealed class MonitorViewModel : ViewModelBase
{
    private readonly RelayCommand _runPreviewCommand;
    private readonly RelayCommand _runApplyCommand;
    private readonly RelayCommand _cancelCommand;
    private readonly TweakExecutionPipeline _pipeline;
    private readonly DemoTweak _demoTweak = new();
    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private int _runCount;
    private bool _verifyAfterApply = true;
    private bool _rollbackOnFailure = true;
    private string _statusMessage = "Ready to run.";
    private string _runModeLabel = "Mode: Preview (DryRun)";
    private string _lastUpdatedText = "Last update: -";
    private string _lastDurationText = "Duration: -";
    private string _currentStepText = "Current step: -";

    public MonitorViewModel()
    {
        Steps = new ObservableCollection<TweakStepStatusViewModel>
        {
            new(TweakAction.Detect),
            new(TweakAction.Apply),
            new(TweakAction.Verify),
            new(TweakAction.Rollback)
        };

        ResetSteps();

        var paths = AppPaths.FromEnvironment();
        var logger = new FileAppLogger(paths);
        var logStore = new FileTweakLogStore(paths);
        _pipeline = new TweakExecutionPipeline(logger, logStore);

        _runPreviewCommand = new RelayCommand(_ => _ = RunPipelineAsync(true), _ => !IsRunning);
        _runApplyCommand = new RelayCommand(_ => _ = RunPipelineAsync(false), _ => !IsRunning);
        _cancelCommand = new RelayCommand(_ => CancelRun(), _ => IsRunning);
    }

    public string Title => "Monitor";

    public string CurrentTweakName => _demoTweak.Name;

    public ObservableCollection<TweakStepStatusViewModel> Steps { get; }

    public ICommand RunPreviewCommand => _runPreviewCommand;

    public ICommand RunApplyCommand => _runApplyCommand;

    public ICommand CancelCommand => _cancelCommand;

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                _runPreviewCommand.RaiseCanExecuteChanged();
                _runApplyCommand.RaiseCanExecuteChanged();
                _cancelCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public int RunCount
    {
        get => _runCount;
        private set => SetProperty(ref _runCount, value);
    }

    public bool VerifyAfterApply
    {
        get => _verifyAfterApply;
        set => SetProperty(ref _verifyAfterApply, value);
    }

    public bool RollbackOnFailure
    {
        get => _rollbackOnFailure;
        set => SetProperty(ref _rollbackOnFailure, value);
    }

    public string RunModeLabel
    {
        get => _runModeLabel;
        private set => SetProperty(ref _runModeLabel, value);
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

    public string CurrentStepText
    {
        get => _currentStepText;
        private set => SetProperty(ref _currentStepText, value);
    }

    public string StepProgressText => $"Steps: {GetCompletedStepCount()}/{Steps.Count}";

    private async Task RunPipelineAsync(bool dryRun)
    {
        if (IsRunning)
        {
            return;
        }

        IsRunning = true;
        StartCancellation();
        RunCount++;
        var startedAt = DateTimeOffset.UtcNow;
        RunModeLabel = dryRun ? "Mode: Preview (DryRun)" : "Mode: Apply";
        StatusMessage = dryRun ? "Preview run started." : "Apply run started.";
        ResetSteps();
        Steps.First().MarkInProgress();
        OnPropertyChanged(nameof(StepProgressText));
        CurrentStepText = "Current step: Detect";

        var progress = new Progress<TweakExecutionUpdate>(OnProgressUpdate);
        var options = new TweakExecutionOptions
        {
            DryRun = dryRun,
            VerifyAfterApply = VerifyAfterApply,
            RollbackOnFailure = RollbackOnFailure
        };

        try
        {
            var report = await _pipeline.ExecuteAsync(_demoTweak, options, progress, _cts?.Token ?? CancellationToken.None);
            ApplyReport(report);
            StatusMessage = report.Succeeded ? "Run completed." : "Run completed with errors.";
            LastUpdatedText = $"Last update: {report.CompletedAt.ToLocalTime():HH:mm:ss}";
            LastDurationText = $"Duration: {FormatDuration(report.CompletedAt - report.StartedAt)}";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Run cancelled.";
            LastDurationText = $"Duration: {FormatDuration(DateTimeOffset.UtcNow - startedAt)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Run failed: {ex.Message}";
            LastDurationText = $"Duration: {FormatDuration(DateTimeOffset.UtcNow - startedAt)}";
        }
        finally
        {
            IsRunning = false;
            ClearCancellation();
        }
    }

    private void OnProgressUpdate(TweakExecutionUpdate update)
    {
        var step = Steps.FirstOrDefault(item => item.Action == update.Action);
        step?.ApplyResult(update.Status, update.Message, update.Timestamp);
        OnPropertyChanged(nameof(StepProgressText));
        CurrentStepText = $"Current step: {update.Action} ({update.Status})";

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
        CurrentStepText = "Current step: -";
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

    private void StartCancellation()
    {
        ClearCancellation();
        _cts = new CancellationTokenSource();
    }

    private void ClearCancellation()
    {
        _cts?.Dispose();
        _cts = null;
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

    private sealed class DemoTweak : ITweak
    {
        private static readonly TimeSpan StepDelay = TimeSpan.FromMilliseconds(450);

        public string Id => "demo.monitor";
        public string Name => "Demo tweak pipeline";
        public string Description => "Simulated tweak run for monitoring UI.";
        public TweakRiskLevel Risk => TweakRiskLevel.Safe;

        public Task<TweakResult> DetectAsync(CancellationToken ct)
            => SimulateAsync(TweakStatus.Detected, "Configuration snapshot captured.", ct);

        public Task<TweakResult> ApplyAsync(CancellationToken ct)
            => SimulateAsync(TweakStatus.Applied, "Simulated tweak applied.", ct);

        public Task<TweakResult> VerifyAsync(CancellationToken ct)
            => SimulateAsync(TweakStatus.Verified, "Verification completed.", ct);

        public Task<TweakResult> RollbackAsync(CancellationToken ct)
            => SimulateAsync(TweakStatus.RolledBack, "Rollback completed.", ct);

        private static async Task<TweakResult> SimulateAsync(TweakStatus status, string message, CancellationToken ct)
        {
            await Task.Delay(StepDelay, ct);
            return new TweakResult(status, message, DateTimeOffset.UtcNow);
        }
    }
}
