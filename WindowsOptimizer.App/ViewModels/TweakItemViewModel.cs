using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine;

namespace WindowsOptimizer.App.ViewModels;

public sealed class TweakItemViewModel : ViewModelBase
{
    private readonly ITweak _tweak;
    private readonly TweakExecutionPipeline _pipeline;
    private readonly RelayCommand _previewCommand;
    private readonly RelayCommand _applyCommand;
    private readonly RelayCommand _verifyCommand;
    private readonly RelayCommand _rollbackCommand;
    private readonly RelayCommand _cancelCommand;
    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private string _statusMessage = "Idle";
    private string _lastUpdatedText = "Last update: -";

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

        _previewCommand = new RelayCommand(_ => _ = RunPipelineAsync(true), _ => !IsRunning);
        _applyCommand = new RelayCommand(_ => _ = RunPipelineAsync(false), _ => !IsRunning);
        _verifyCommand = new RelayCommand(_ => _ = RunSingleStepAsync(TweakAction.Verify), _ => !IsRunning);
        _rollbackCommand = new RelayCommand(_ => _ = RunSingleStepAsync(TweakAction.Rollback), _ => !IsRunning);
        _cancelCommand = new RelayCommand(_ => CancelRun(), _ => IsRunning);
    }

    public string Name => _tweak.Name;

    public string Description => _tweak.Description;

    public TweakRiskLevel Risk => _tweak.Risk;

    public ObservableCollection<TweakStepStatusViewModel> Steps { get; }

    public ICommand PreviewCommand => _previewCommand;

    public ICommand ApplyCommand => _applyCommand;

    public ICommand VerifyCommand => _verifyCommand;

    public ICommand RollbackCommand => _rollbackCommand;

    public ICommand CancelCommand => _cancelCommand;

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                _previewCommand.RaiseCanExecuteChanged();
                _applyCommand.RaiseCanExecuteChanged();
                _verifyCommand.RaiseCanExecuteChanged();
                _rollbackCommand.RaiseCanExecuteChanged();
                _cancelCommand.RaiseCanExecuteChanged();
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

    private async Task RunPipelineAsync(bool dryRun)
    {
        if (IsRunning)
        {
            return;
        }

        StartCancellation();
        IsRunning = true;
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
            StatusMessage = report.Succeeded ? "Run completed." : "Run completed with errors.";
            LastUpdatedText = $"Last update: {report.CompletedAt.ToLocalTime():HH:mm:ss}";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Run cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Run failed: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
            ClearCancellation();
        }
    }

    private async Task RunSingleStepAsync(TweakAction action)
    {
        if (IsRunning)
        {
            return;
        }

        StartCancellation();
        IsRunning = true;
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

            var result = await _pipeline.ExecuteStepAsync(_tweak, action, updateProgress, _cts?.Token ?? CancellationToken.None);
            step?.ApplyResult(result.Result.Status, result.Result.Message, result.Result.Timestamp);
            StatusMessage = $"{action} {result.Result.Status}.";
            LastUpdatedText = $"Last update: {result.Result.Timestamp.ToLocalTime():HH:mm:ss}";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = $"{action} cancelled.";
        }
        catch (Exception ex)
        {
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
}
