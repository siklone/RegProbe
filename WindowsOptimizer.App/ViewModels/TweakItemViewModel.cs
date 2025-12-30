using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Tweaks;

namespace WindowsOptimizer.App.ViewModels;

public sealed class TweakItemViewModel : ViewModelBase
{
    private static readonly SolidColorBrush AppliedStatusBrush = CreateFrozenBrush("#A3BE8C");
    private static readonly SolidColorBrush NotAppliedStatusBrush = CreateFrozenBrush("#EBCB8B");
    private static readonly SolidColorBrush ErrorStatusBrush = CreateFrozenBrush("#BF616A");
    private static readonly SolidColorBrush UnknownStatusBrush = CreateFrozenBrush("#88C0D0");

    private static readonly SolidColorBrush AppliedStatusBackgroundBrush = CreateFrozenBrush("#2AA3BE8C");
    private static readonly SolidColorBrush NotAppliedStatusBackgroundBrush = CreateFrozenBrush("#2AEBCB8B");
    private static readonly SolidColorBrush ErrorStatusBackgroundBrush = CreateFrozenBrush("#2ABF616A");
    private static readonly SolidColorBrush UnknownStatusBackgroundBrush = CreateFrozenBrush("#2A88C0D0");

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
    private TweakActionType _actionType = TweakActionType.Toggle;
    private string _actionButtonText = "Apply";
    private string _registryPath = string.Empty;
    private string _codeExample = string.Empty;
    private readonly RelayCommand _toggleCommand;
    private readonly RelayCommand _customActionCommand;
    private readonly RelayCommand _copyRegistryPathCommand;
    private readonly RelayCommand _openReferenceLinkCommand;
    private string _terminalOutput = string.Empty;
    private bool _showTerminal = false;
    private PriorityCalculatorViewModel? _priorityCalculator;
    private bool _isHighlighted = false;
    private string _currentValue = "Unknown";
    private string _targetValue = "Optimized";
    private readonly string _impactAreaLabel;
    private ObservableCollection<double> _sparklinePoints = new();
    private bool _isRecommended;
    private string _recommendationReason = string.Empty;
    private double _recommendationConfidence;
    private bool _isSelected;
    private bool _isFavorite;
    private readonly RelayCommand _toggleFavoriteCommand;

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

        ReferenceLinks = new ObservableCollection<ReferenceLink>();
        SubOptions = new ObservableCollection<TweakSubOption>();

        ResetSteps();

        _detectCommand = new RelayCommand(_ => _ = RunSingleStepAsync(TweakAction.Detect, CancellationToken.None), _ => CanRun());
        _previewCommand = new RelayCommand(_ => _ = RunAsync(true, CancellationToken.None), _ => CanRun());
        _applyCommand = new RelayCommand(_ => _ = RunAsync(false, CancellationToken.None), _ => CanRun());
        _verifyCommand = new RelayCommand(_ => _ = RunSingleStepAsync(TweakAction.Verify, CancellationToken.None), _ => CanRun());
        _rollbackCommand = new RelayCommand(_ => _ = RunSingleStepAsync(TweakAction.Rollback, CancellationToken.None), _ => CanRun());
        _cancelCommand = new RelayCommand(_ => CancelRun(), _ => CanCancel());
        _copyIdCommand = new RelayCommand(_ => CopyId());
        _toggleCommand = new RelayCommand(_ => _ = ToggleAsync(), _ => CanToggle());
        _customActionCommand = new RelayCommand(_ => _ = RunCustomActionAsync(), _ => CanRun());
        _copyRegistryPathCommand = new RelayCommand(_ => CopyRegistryPath(), _ => !string.IsNullOrEmpty(RegistryPath));
        _openReferenceLinkCommand = new RelayCommand(OpenReferenceLink, parameter => parameter is string url && !string.IsNullOrWhiteSpace(url));
        _toggleFavoriteCommand = new RelayCommand(_ => ToggleFavorite());

        _impactAreaLabel = DetermineImpactAreaLabel(_tweak);

        TryPopulateTechnicalInfo();
    }

    public string Name => _tweak.Name;

    public string Id => _tweak.Id;

    public string Description => _tweak.Description;

    public TweakRiskLevel Risk => _tweak.Risk;

    public bool RequiresElevation => _tweak.RequiresElevation;

    public bool IsElevated => _isElevated;

    public bool WillPromptForElevation => RequiresElevation && !IsElevated;

    public string ElevationBadgeText => "Admin";

    public string ElevationTooltip => IsElevated
        ? "Admin required. Runs via ElevatedHost."
        : "Admin required. You'll get a UAC prompt and it runs via ElevatedHost.";

    public string ElevationWarningText => WillPromptForElevation
        ? "Requires elevation. Approve the UAC prompt to continue."
        : string.Empty;

    public string Category => ExtractCategory(Id);

    public string CategoryIcon => GetCategoryIcon(Category);

    public string StatusTooltip => AppliedStatus switch
    {
        TweakAppliedStatus.Applied => "Applied. Current state matches the desired configuration.",
        TweakAppliedStatus.NotApplied => "Not applied. Detected state differs from the desired configuration.",
        TweakAppliedStatus.Error => "Error. Open Execution Log for details.",
        _ => "Unknown. Click Detect to read current state."
    };

    public string ActionsHelpTooltip =>
        "Detect: Reads current state (no changes)\n" +
        "Preview: Dry run (no changes)\n" +
        "Apply: Detect -> Apply -> Verify (Rollback on failure)\n" +
        "Verify: Confirms current state matches desired\n" +
        "Rollback: Restores value captured by Detect (same app session)";

    private static string ExtractCategory(string id)
    {
        var dotIndex = id.IndexOf('.');
        if (dotIndex <= 0) return Utilities.StringPool.Intern("Other");
        var cat = id.Substring(0, dotIndex);
        return Utilities.StringPool.GetCategory(cat);
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

    private static string DetermineImpactAreaLabel(ITweak tweak)
    {
        var area = tweak switch
        {
            RegistryValueTweak or RegistryValueBatchTweak or RegistryValueSetTweak => "Registry",
            ServiceStartModeBatchTweak => "Service",
            ScheduledTaskBatchTweak => "Task",
            _ => "Other"
        };
        return Utilities.StringPool.GetImpactArea(area);
    }

    public ObservableCollection<TweakStepStatusViewModel> Steps { get; }

    public ICommand DetectCommand => _detectCommand;

    public ICommand PreviewCommand => _previewCommand;

    public ICommand ApplyCommand => _applyCommand;

    public ICommand VerifyCommand => _verifyCommand;

    public ICommand RollbackCommand => _rollbackCommand;

    public ICommand CancelCommand => _cancelCommand;

    public ICommand CopyIdCommand => _copyIdCommand;

    public ICommand ToggleCommand => _toggleCommand;

    public ICommand CustomActionCommand => _customActionCommand;

    public ICommand CopyRegistryPathCommand => _copyRegistryPathCommand;

    public ICommand OpenReferenceLinkCommand => _openReferenceLinkCommand;

    public TweakActionType ActionType
    {
        get => _actionType;
        set => SetProperty(ref _actionType, value);
    }

    public string ActionButtonText
    {
        get => _actionButtonText;
        set => SetProperty(ref _actionButtonText, value);
    }

    public string RegistryPath
    {
        get => _registryPath;
        set
        {
            if (SetProperty(ref _registryPath, value))
            {
                OnPropertyChanged(nameof(HasRegistryPath));
                OnPropertyChanged(nameof(HasDiff));
            }
        }
    }

    public string CodeExample
    {
        get => _codeExample;
        set
        {
            if (SetProperty(ref _codeExample, value))
            {
                OnPropertyChanged(nameof(HasCodeExample));
            }
        }
    }

    public ObservableCollection<ReferenceLink> ReferenceLinks { get; }

    public ObservableCollection<TweakSubOption> SubOptions { get; }

    public bool HasSubOptions => SubOptions.Any();

    public bool HasRegistryPath => !string.IsNullOrEmpty(RegistryPath);

    public bool HasCodeExample => !string.IsNullOrEmpty(CodeExample);

    public bool HasReferenceLinks => ReferenceLinks.Any();

    public PriorityCalculatorViewModel? PriorityCalculator
    {
        get => _priorityCalculator;
        set { if (SetProperty(ref _priorityCalculator, value)) OnPropertyChanged(nameof(HasPriorityCalculator)); }
    }

    public bool HasPriorityCalculator => PriorityCalculator != null;

    public bool IsHighlighted
    {
        get => _isHighlighted;
        set => SetProperty(ref _isHighlighted, value);
    }

    public string CurrentValue
    {
        get => _currentValue;
        set
        {
            if (SetProperty(ref _currentValue, value))
            {
                OnPropertyChanged(nameof(HasDiff));
                OnPropertyChanged(nameof(CompactInfoLine));
            }
        }
    }

    public string TargetValue
    {
        get => _targetValue;
        set
        {
            if (SetProperty(ref _targetValue, value))
            {
                OnPropertyChanged(nameof(CompactInfoLine));
            }
        }
    }

    public string ImpactAreaLabel => _impactAreaLabel;

    public bool HasCompactInfoLine => ImpactAreaLabel is "Registry" or "Service" or "Task";

    public string CompactInfoLine
    {
        get
        {
            if (!HasCompactInfoLine)
            {
                return string.Empty;
            }

            var current = string.IsNullOrWhiteSpace(CurrentValue) ? "Unknown" : CurrentValue;
            var target = string.IsNullOrWhiteSpace(TargetValue) ? "Optimized" : TargetValue;
            return $"{ImpactAreaLabel}: {current} → {target}";
        }
    }

    public bool IsRecommended
    {
        get => _isRecommended;
        set => SetProperty(ref _isRecommended, value);
    }

    public string RecommendationReason
    {
        get => _recommendationReason;
        set => SetProperty(ref _recommendationReason, value);
    }

    public double RecommendationConfidence
    {
        get => _recommendationConfidence;
        set => SetProperty(ref _recommendationConfidence, value);
    }

    public bool HasDiff => !string.IsNullOrEmpty(RegistryPath) && CurrentValue != "Unknown";

    public ObservableCollection<double> SparklinePoints
    {
        get => _sparklinePoints;
        set => SetProperty(ref _sparklinePoints, value);
    }

    public void UpdateMetric(double value)
    {
        _sparklinePoints.Add(value);
        if (_sparklinePoints.Count > 20)
        {
            _sparklinePoints.RemoveAt(0);
        }
    }

    private void GenerateMockSparkline()
    {
        if (_sparklinePoints.Any()) return;
        var r = new Random(Id.GetHashCode());
        for (int i = 0; i < 15; i++)
        {
            _sparklinePoints.Add(r.Next(20, 80));
        }
    }

    public string TerminalOutput
    {
        get => _terminalOutput;
        private set => SetProperty(ref _terminalOutput, value);
    }

    public bool ShowTerminal
    {
        get => _showTerminal;
        set => SetProperty(ref _showTerminal, value);
    }

    private void AppendToTerminal(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        TerminalOutput += $"[{timestamp}] {message}\n";
    }

    private void ClearTerminal()
    {
        TerminalOutput = string.Empty;
    }

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
                OnPropertyChanged(nameof(StatusBadgeBackground));
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

    public Brush StatusColor => AppliedStatus switch
    {
        TweakAppliedStatus.Applied => AppliedStatusBrush,
        TweakAppliedStatus.NotApplied => NotAppliedStatusBrush,
        TweakAppliedStatus.Error => ErrorStatusBrush,
        _ => UnknownStatusBrush
    };

    public Brush StatusBadgeBackground => AppliedStatus switch
    {
        TweakAppliedStatus.Applied => AppliedStatusBackgroundBrush,
        TweakAppliedStatus.NotApplied => NotAppliedStatusBackgroundBrush,
        TweakAppliedStatus.Error => ErrorStatusBackgroundBrush,
        _ => UnknownStatusBackgroundBrush
    };

    public string StatusText => AppliedStatus switch
    {
        TweakAppliedStatus.Applied => "Applied",
        TweakAppliedStatus.NotApplied => "Not Applied",
        TweakAppliedStatus.Error => "Error",
        _ => "Unknown"
    };

    private static SolidColorBrush CreateFrozenBrush(string hex)
    {
        var color = (Color)ColorConverter.ConvertFromString(hex);
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

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

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        set
        {
            if (SetProperty(ref _isFavorite, value))
            {
                OnPropertyChanged(nameof(FavoriteIcon));
                FavoriteChanged?.Invoke(this, value);
            }
        }
    }

    public string FavoriteIcon => _isFavorite ? "★" : "☆";

    public ICommand ToggleFavoriteCommand => _toggleFavoriteCommand;

    /// <summary>
    /// Event raised when favorite status changes. TweaksViewModel subscribes to persist changes.
    /// </summary>
    public event Action<TweakItemViewModel, bool>? FavoriteChanged;

    private void ToggleFavorite()
    {
        IsFavorite = !IsFavorite;
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
        set
        {
            if (SetProperty(ref _isDetailsExpanded, value) && value)
            {
                GenerateMockSparkline();
            }
        }
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

        LogToFile($"RunAsync START: Tweak '{Name}' (ID: {Id}), DryRun={dryRun}");
        StartCancellation(ct);
        var actionLabel = dryRun ? "Preview" : "Apply";

        IsRunning = true;
        LastActionText = actionLabel;
        LastOutcome = TweakRunOutcome.InProgress;
        StatusMessage = dryRun ? "Preview run started." : "Apply run started.";
        LastUpdatedText = "Last update: -";
        ClearTerminal();
        AppendToTerminal(dryRun ? "Starting Pre-Execution Check (Dry Run)..." : "Starting Execution Pipeline...");
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
            LogToFile($"RunAsync: Calling ExecuteAsync for '{Name}'");
            var report = await _pipeline.ExecuteAsync(_tweak, options, progress, _cts?.Token ?? CancellationToken.None);
            LogToFile($"RunAsync: ExecuteAsync COMPLETED for '{Name}', Succeeded={report.Succeeded}");
            ApplyReport(report);
            UpdateAfterRun(report);
            LastOutcome = report.Succeeded ? TweakRunOutcome.Success : TweakRunOutcome.Failed;
            StatusMessage = report.Succeeded ? "Run completed." : "Run completed with errors.";
            LastUpdatedText = $"Last update: {report.CompletedAt.ToLocalTime():HH:mm:ss}";
        }
        catch (OperationCanceledException)
        {
            LogToFile($"RunAsync: '{Name}' was CANCELLED");
            LastOutcome = TweakRunOutcome.Cancelled;
            StatusMessage = "Run cancelled.";
        }
        catch (Exception ex)
        {
            LogToFile($"RunAsync: '{Name}' FAILED with exception: {ex.Message}");
            LogToFile($"Stack: {ex.StackTrace}");
            LastOutcome = TweakRunOutcome.Failed;
            StatusMessage = $"Run failed: {ex.Message}";
        }
        finally
        {
            LogToFile($"RunAsync END: '{Name}' IsRunning=false");
            IsRunning = false;
            ClearCancellation();
        }
    }

    private void UpdateAfterRun(TweakExecutionReport report)
    {
        if (!report.Succeeded)
        {
            AppliedStatus = TweakAppliedStatus.Error;
            return;
        }

        if (report.DryRun)
        {
            var detect = report.Steps.FirstOrDefault(step => step.Action == TweakAction.Detect);
            AppliedStatus = detect?.Result.Status switch
            {
                TweakStatus.Applied or TweakStatus.Verified => TweakAppliedStatus.Applied,
                TweakStatus.Detected => TweakAppliedStatus.NotApplied,
                _ => AppliedStatus
            };
            return;
        }

        if (report.RolledBack)
        {
            AppliedStatus = TweakAppliedStatus.NotApplied;
            return;
        }

        if (report.Verified || report.Applied)
        {
            AppliedStatus = TweakAppliedStatus.Applied;
            if (report.Verified)
            {
                CurrentValue = TargetValue;
            }
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
            AppendToTerminal($"{action} Result: {result.Result.Status}. {result.Result.Message}");
            UpdateAfterSingleStep(action, result.Result);
            LastOutcome = MapOutcome(result.Result.Status);
            StatusMessage = string.IsNullOrWhiteSpace(result.Result.Message)
                ? result.Result.Status.ToString()
                : result.Result.Message;
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

    private void UpdateAfterSingleStep(TweakAction action, TweakResult result)
    {
        switch (action)
        {
            case TweakAction.Detect:
                AppliedStatus = result.Status switch
                {
                    TweakStatus.Applied or TweakStatus.Verified => TweakAppliedStatus.Applied,
                    TweakStatus.Detected => TweakAppliedStatus.NotApplied,
                    TweakStatus.NotApplicable => TweakAppliedStatus.NotApplied,
                    TweakStatus.Skipped => TweakAppliedStatus.NotApplied,
                    TweakStatus.Failed => TweakAppliedStatus.Error,
                    _ => AppliedStatus
                };

                TryUpdateCurrentValueFromMessage(result.Message);
                if (CurrentValue == "Unknown" && result.Status is TweakStatus.Applied or TweakStatus.Verified)
                {
                    CurrentValue = TargetValue;
                }

                break;
            case TweakAction.Apply:
                if (result.Status == TweakStatus.Applied)
                {
                    AppliedStatus = TweakAppliedStatus.Applied;
                    CurrentValue = TargetValue;
                }
                else if (result.Status == TweakStatus.Failed)
                {
                    AppliedStatus = TweakAppliedStatus.Error;
                }

                break;
            case TweakAction.Verify:
                if (result.Status == TweakStatus.Verified)
                {
                    AppliedStatus = TweakAppliedStatus.Applied;
                    CurrentValue = TargetValue;
                }
                else if (result.Status == TweakStatus.Failed)
                {
                    AppliedStatus = TweakAppliedStatus.NotApplied;
                }

                break;
            case TweakAction.Rollback:
                if (result.Status == TweakStatus.RolledBack)
                {
                    AppliedStatus = TweakAppliedStatus.NotApplied;
                }
                else if (result.Status == TweakStatus.Failed)
                {
                    AppliedStatus = TweakAppliedStatus.Error;
                }

                break;
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

        AppendToTerminal($"> {update.Action}: {update.Status} - {update.Message}");

        StatusMessage = string.IsNullOrWhiteSpace(update.Message)
            ? update.Status.ToString()
            : update.Message;
        LastUpdatedText = $"Last update: {update.Timestamp.ToLocalTime():HH:mm:ss}";

        if (update.Action == TweakAction.Detect)
        {
            TryUpdateCurrentValueFromMessage(update.Message);
        }

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

    private void CopyRegistryPath()
    {
        try
        {
            Clipboard.SetText(RegistryPath);
            StatusMessage = "Registry path copied to clipboard.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Copy failed: {ex.Message}";
        }
    }

    private void OpenReferenceLink(object? parameter)
    {
        if (parameter is not string url || string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            LogToFile($"OpenReferenceLink: {url}");
            var startInfo = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
                Verb = "open"
            };

            Process.Start(startInfo);
            StatusMessage = "Opening link...";
        }
        catch (Exception ex)
        {
            try
            {
                // Fallback: use explorer to open the URL (avoids shell association edge cases).
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = url,
                    UseShellExecute = true
                });
                StatusMessage = "Opening link...";
                return;
            }
            catch
            {
            }

            StatusMessage = $"Could not open link: {ex.Message}";
            LogToFile($"OpenReferenceLink failed: {ex.Message} ({url})");
        }
    }

    private void TryPopulateTechnicalInfo()
    {
        switch (_tweak)
        {
            case RegistryValueTweak registryValueTweak:
                if (string.IsNullOrWhiteSpace(RegistryPath))
                {
                    RegistryPath = FormatRegistryValuePath(registryValueTweak.Reference);
                }

                if (string.IsNullOrWhiteSpace(TargetValue) || TargetValue == "Optimized")
                {
                    TargetValue = FormatRegistryValueForDisplay(registryValueTweak.ValueKind, registryValueTweak.TargetValue);
                }

                if (string.IsNullOrWhiteSpace(CodeExample))
                {
                    CodeExample = BuildRegistryCommandPreview(
                        registryValueTweak.Reference,
                        registryValueTweak.ValueKind,
                        registryValueTweak.TargetValue);
                }

                break;
            case RegistryValueBatchTweak:
            case RegistryValueSetTweak:
                if (string.IsNullOrWhiteSpace(TargetValue) || TargetValue == "Optimized")
                {
                    TargetValue = "Multiple values";
                }

                break;
            case ServiceStartModeBatchTweak serviceStartModeBatchTweak:
                if (string.IsNullOrWhiteSpace(TargetValue) || TargetValue == "Optimized")
                {
                    TargetValue = FormatServiceStartModeForDisplay(serviceStartModeBatchTweak.TargetStartModeSummary);
                }

                break;
            case ScheduledTaskBatchTweak:
                if (string.IsNullOrWhiteSpace(TargetValue) || TargetValue == "Optimized")
                {
                    TargetValue = "Disabled";
                }

                break;
        }
    }

    private static string FormatServiceStartModeForDisplay(ServiceStartMode startMode)
    {
        return startMode == ServiceStartMode.Unknown
            ? "Mixed"
            : startMode.ToString();
    }

    private static string FormatRegistryValuePath(WindowsOptimizer.Core.Registry.RegistryValueReference reference)
    {
        var key = FormatRegistryKey(reference);
        return $"{key}\\{reference.ValueName}";
    }

    private static string FormatRegistryKey(WindowsOptimizer.Core.Registry.RegistryValueReference reference)
    {
        var keyPath = (reference.KeyPath ?? string.Empty).Trim().TrimStart('\\').TrimEnd('\\');
        if (keyPath.StartsWith("HKEY_", StringComparison.OrdinalIgnoreCase)
            || keyPath.StartsWith("HKLM\\", StringComparison.OrdinalIgnoreCase)
            || keyPath.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase)
            || keyPath.StartsWith("HKCR\\", StringComparison.OrdinalIgnoreCase)
            || keyPath.StartsWith("HKU\\", StringComparison.OrdinalIgnoreCase)
            || keyPath.StartsWith("HKCC\\", StringComparison.OrdinalIgnoreCase))
        {
            return keyPath;
        }

        var hive = reference.Hive switch
        {
            RegistryHive.LocalMachine => "HKLM",
            RegistryHive.CurrentUser => "HKCU",
            RegistryHive.ClassesRoot => "HKCR",
            RegistryHive.Users => "HKU",
            RegistryHive.CurrentConfig => "HKCC",
            _ => reference.Hive.ToString()
        };

        return string.IsNullOrEmpty(keyPath) ? hive : $"{hive}\\{keyPath}";
    }

    private static string BuildRegistryCommandPreview(
        WindowsOptimizer.Core.Registry.RegistryValueReference reference,
        RegistryValueKind valueKind,
        object targetValue)
    {
        var key = FormatRegistryKey(reference);
        var regType = valueKind switch
        {
            RegistryValueKind.String => "REG_SZ",
            RegistryValueKind.ExpandString => "REG_EXPAND_SZ",
            RegistryValueKind.MultiString => "REG_MULTI_SZ",
            RegistryValueKind.Binary => "REG_BINARY",
            RegistryValueKind.DWord => "REG_DWORD",
            RegistryValueKind.QWord => "REG_QWORD",
            _ => $"REG_{valueKind.ToString().ToUpperInvariant()}"
        };

        var viewFlag = reference.View switch
        {
            RegistryView.Registry32 => " /reg:32",
            RegistryView.Registry64 => " /reg:64",
            _ => string.Empty
        };

        var data = FormatRegistryValueForRegAdd(valueKind, targetValue);

        return string.Join(
            Environment.NewLine,
            $"reg add \"{key}\" /v \"{reference.ValueName}\" /t {regType} /d {data} /f{viewFlag}",
            $"reg query \"{key}\" /v \"{reference.ValueName}\"{viewFlag}");
    }

    private static string FormatRegistryValueForRegAdd(RegistryValueKind valueKind, object value)
    {
        switch (valueKind)
        {
            case RegistryValueKind.DWord:
            case RegistryValueKind.QWord:
                return Convert.ToInt64(value).ToString();
            case RegistryValueKind.MultiString:
                if (value is string[] strings)
                {
                    var combined = string.Join("\\0", strings);
                    return $"\"{combined}\\0\"";
                }

                return $"\"{value}\"";
            case RegistryValueKind.String:
            case RegistryValueKind.ExpandString:
                return $"\"{value}\"";
            case RegistryValueKind.Binary:
                if (value is byte[] bytes)
                {
                    var hex = BitConverter.ToString(bytes).Replace("-", string.Empty);
                    return hex;
                }

                return value.ToString() ?? string.Empty;
            default:
                return value.ToString() ?? string.Empty;
        }
    }

    private static string FormatRegistryValueForDisplay(RegistryValueKind valueKind, object value)
    {
        switch (valueKind)
        {
            case RegistryValueKind.DWord:
            case RegistryValueKind.QWord:
                try
                {
                    var number = Convert.ToInt64(value);
                    return $"{number} (0x{number:X})";
                }
                catch
                {
                    return value.ToString() ?? "Unknown";
                }
            case RegistryValueKind.MultiString:
                return value is string[] strings
                    ? string.Join("; ", strings)
                    : value.ToString() ?? "Unknown";
            case RegistryValueKind.Binary:
                return value is byte[] bytes
                    ? $"0x{BitConverter.ToString(bytes).Replace("-", string.Empty)}"
                    : value.ToString() ?? "Unknown";
            default:
                return value.ToString() ?? "Unknown";
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

            AppliedStatus = result.Result.Status switch
            {
                TweakStatus.Applied or TweakStatus.Verified => TweakAppliedStatus.Applied,
                TweakStatus.Detected => TweakAppliedStatus.NotApplied,
                TweakStatus.NotApplicable => TweakAppliedStatus.NotApplied,
                TweakStatus.Skipped => TweakAppliedStatus.NotApplied,
                TweakStatus.Failed => TweakAppliedStatus.Error,
                _ => TweakAppliedStatus.Unknown
            };

            TryUpdateCurrentValueFromMessage(result.Result.Message);

            if (CurrentValue == "Unknown" && result.Result.Status is TweakStatus.Applied or TweakStatus.Verified)
            {
                CurrentValue = TargetValue;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DetectStatusAsync failed for tweak '{Name}' (ID: {Id}): {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            AppliedStatus = TweakAppliedStatus.Unknown;
        }
    }

    private void TryUpdateCurrentValueFromMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (message.Contains("Value not set", StringComparison.OrdinalIgnoreCase))
        {
            CurrentValue = "Not set";
            return;
        }

        if (TryExtractAfterPrefix(message, "Current value is ", out var value))
        {
            CurrentValue = value.TrimEnd('.');
            return;
        }

        if (TryExtractAfterPrefix(message, "Current state:", out var state))
        {
            var trimmed = state.Trim();
            var newlineIndex = trimmed.IndexOfAny(new[] { '\r', '\n' });
            if (newlineIndex >= 0)
            {
                trimmed = trimmed[..newlineIndex];
            }

            var detailsIndex = trimmed.IndexOf("Details", StringComparison.OrdinalIgnoreCase);
            if (detailsIndex >= 0)
            {
                trimmed = trimmed[..detailsIndex];
            }

            var servicesIndex = trimmed.IndexOf("Services", StringComparison.OrdinalIgnoreCase);
            if (servicesIndex >= 0)
            {
                trimmed = trimmed[..servicesIndex];
            }

            var tasksIndex = trimmed.IndexOf("Tasks", StringComparison.OrdinalIgnoreCase);
            if (tasksIndex >= 0)
            {
                trimmed = trimmed[..tasksIndex];
            }

            var periodIndex = trimmed.IndexOf('.');
            if (periodIndex >= 0)
            {
                trimmed = trimmed[..periodIndex];
            }

            CurrentValue = trimmed.Trim();
        }
    }

    private static bool TryExtractAfterPrefix(string message, string prefix, out string value)
    {
        value = string.Empty;
        var index = message.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return false;
        }

        value = message[(index + prefix.Length)..];
        return true;
    }

    private async Task RunCustomActionAsync()
    {
        // For specific action types like Open, we might want different behavior
        if (ActionType == TweakActionType.Open)
        {
            // Placeholder: Typically this would trigger a specific property on ITweak or similar
            StatusMessage = $"Opening associated tool for {Name}...";
            return;
        }

        await RunApplyAsync(CancellationToken.None);
    }

    private static void LogToFile(string message)
    {
        try
        {
            var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WindowsOptimizer_Debug.log");
            var timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
            System.IO.File.AppendAllText(logPath, $"[{timestamp}] {message}\n");
        }
        catch
        {
            // Ignore logging errors
        }
    }
}

/// <summary>
/// Types of primary actions for a tweak
/// </summary>
public enum TweakActionType
{
    Toggle,
    Open,
    Import,
    Export,
    Clean,
    Remove,
    Custom
}

/// <summary>
/// A reference link for documentation or sources
/// </summary>
public sealed class ReferenceLink
{
    public ReferenceLink(string title, string url)
    {
        Title = title;
        Url = url;
    }
    public string Title { get; }
    public string Url { get; }
}

/// <summary>
/// A sub-option for fine-tuning a tweak
/// </summary>
public sealed class TweakSubOption : ViewModelBase
{
    private bool _isEnabled;
    private string _value = string.Empty;

    public TweakSubOption(string label, TweakSubOptionType type)
    {
        Label = label;
        Type = type;
    }

    public string Label { get; }
    public TweakSubOptionType Type { get; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}

public enum TweakSubOptionType
{
    Toggle,
    Numeric,
    Dropdown
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
