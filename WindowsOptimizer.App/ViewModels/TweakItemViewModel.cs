using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using WindowsOptimizer.Engine.Tweaks.Commands;
using WindowsOptimizer.Engine.Tweaks.Commands.Cleanup;
using WindowsOptimizer.Infrastructure;
using WindowsOptimizer.App.Utilities;

namespace WindowsOptimizer.App.ViewModels;

public sealed class TweakItemViewModel : ViewModelBase
{
    private static readonly SolidColorBrush AppliedStatusBrush = CreateFrozenBrush("#A3BE8C");
    private static readonly SolidColorBrush NotAppliedStatusBrush = CreateFrozenBrush("#EBCB8B");
    private static readonly SolidColorBrush MixedStatusBrush = CreateFrozenBrush("#D08770");
    private static readonly SolidColorBrush ErrorStatusBrush = CreateFrozenBrush("#BF616A");
    private static readonly SolidColorBrush UnknownStatusBrush = CreateFrozenBrush("#88C0D0");

    private static readonly SolidColorBrush AppliedStatusBackgroundBrush = CreateFrozenBrush("#2AA3BE8C");
    private static readonly SolidColorBrush NotAppliedStatusBackgroundBrush = CreateFrozenBrush("#2AEBCB8B");
    private static readonly SolidColorBrush MixedStatusBackgroundBrush = CreateFrozenBrush("#2AD08770");
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
    private bool _wasRolledBack;
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
    private DateTimeOffset? _lastDetectedAtUtc;
    private bool _isStateFromCache;
    private bool _isRecommended;
    private string _recommendationReason = string.Empty;
    private double _recommendationConfidence;
    private bool _isSelected;
    private bool _isFavorite;
    private readonly RelayCommand _toggleFavoriteCommand;
    private readonly ObservableCollection<string> _batchDetails = new();
    private string _batchDetailsTitle = "Details";
    private string _batchSummaryLine = string.Empty;

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
        _batchDetails.CollectionChanged += (_, __) => OnPropertyChanged(nameof(HasBatchDetails));

        TryPopulateTechnicalInfo();
    }

    public string Name => _tweak.Name;

    public string Id => _tweak.Id;

    public string Description => _tweak.Description;

    /// <summary>
    /// Rich tooltip explaining implications of this tweak.
    /// </summary>
    public string HelpTooltip => GenerateHelpTooltip();

    /// <summary>
    /// Implications of enabling/disabling this tweak.
    /// </summary>
    public string Implications => GenerateImplications();

    public TweakRiskLevel Risk => _tweak.Risk;

    public bool RequiresElevation => _tweak.RequiresElevation;

    public bool IsElevated => _isElevated;

    public bool WillPromptForElevation => RequiresElevation && !IsElevated;

    public bool WillPromptForDetect =>
        RequiresElevation
        && !IsElevated
        && _tweak is not RegistryValueTweak
        && _tweak is not RegistryValueBatchTweak
        && _tweak is not RegistryValueSetTweak;

    public bool IsScanFriendly =>
        _tweak is not CommandTweak
        && _tweak is not FileCleanupTweak;

    public bool IsStartupScanEligible =>
        IsScanFriendly
        && !WillPromptForDetect;

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
        _ when ShouldShowMixedStatus => "Mixed. Some sub-items match the desired configuration.",
        TweakAppliedStatus.Applied => "Applied. Current state matches the desired configuration.",
        TweakAppliedStatus.NotApplied => "Not applied. Detected state differs from the desired configuration.",
        TweakAppliedStatus.Error => "Error. Open Execution Log for details.",
        _ when RequiresAdminScan => "Unknown. Run an admin detect to read current state.",
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
        if (string.IsNullOrWhiteSpace(id))
        {
            return Utilities.StringPool.Intern("Other");
        }

        // Plugins follow: plugin.<pluginId>.<tweakId>...
        // Group plugin tweaks by pluginId in the UI (e.g. DevTools).
        if (id.StartsWith("plugin.", StringComparison.OrdinalIgnoreCase))
        {
            var parts = id.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 2)
            {
                return Utilities.StringPool.GetCategory(parts[1]);
            }
        }

        var dotIndex = id.IndexOf('.');
        if (dotIndex <= 0)
        {
            return Utilities.StringPool.Intern("Other");
        }

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
        "devtools" => "🧰",
        _ => "📦"
    };

    private static string DetermineImpactAreaLabel(ITweak tweak)
    {
        var area = tweak switch
        {
            RegistryValueTweak or RegistryValueBatchTweak or RegistryValueSetTweak => "Registry",
            ServiceStartModeBatchTweak => "Service",
            ScheduledTaskBatchTweak => "Task",
            SettingsToggleTweak => "Settings",
            FileCleanupTweak or FileRenameTweak => "File",
            CommandTweak => "Command",
            CompositeTweak => "Composite",
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
                OnPropertyChanged(nameof(CompactInfoTooltip));
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(StatusBadgeBackground));
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusTooltip));
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
                OnPropertyChanged(nameof(CompactInfoTooltip));
            }
        }
    }

    public DateTimeOffset? LastDetectedAtUtc
    {
        get => _lastDetectedAtUtc;
        private set
        {
            if (SetProperty(ref _lastDetectedAtUtc, value))
            {
                OnPropertyChanged(nameof(HasDetectedState));
                OnPropertyChanged(nameof(InventoryFreshnessText));
            }
        }
    }

    public bool IsStateFromCache
    {
        get => _isStateFromCache;
        private set
        {
            if (SetProperty(ref _isStateFromCache, value))
            {
                OnPropertyChanged(nameof(InventoryFreshnessText));
            }
        }
    }

    public bool HasDetectedState => LastDetectedAtUtc.HasValue;

    public string InventoryFreshnessText
    {
        get
        {
            if (!LastDetectedAtUtc.HasValue)
            {
                return "Not scanned yet";
            }

            var elapsed = DateTimeOffset.UtcNow - LastDetectedAtUtc.Value;
            if (elapsed < TimeSpan.Zero)
            {
                elapsed = TimeSpan.Zero;
            }

            string ageText;
            if (elapsed.TotalMinutes < 1)
            {
                ageText = $"{Math.Max(1, (int)elapsed.TotalSeconds)}s ago";
            }
            else if (elapsed.TotalHours < 1)
            {
                ageText = $"{(int)elapsed.TotalMinutes}m ago";
            }
            else if (elapsed.TotalDays < 1)
            {
                ageText = $"{(int)elapsed.TotalHours}h ago";
            }
            else
            {
                ageText = $"{(int)elapsed.TotalDays}d ago";
            }

            var source = IsStateFromCache ? "Cached" : "Live";
            return $"{source} {ageText}";
        }
    }

    /// <summary>
    /// Before state for snapshot comparison (same as CurrentValue).
    /// </summary>
    public string BeforeState => CurrentValue;

    /// <summary>
    /// After state for snapshot comparison (same as TargetValue).
    /// </summary>
    public string AfterState => TargetValue;

    /// <summary>
    /// Whether there's a meaningful state change to show.
    /// </summary>
    public bool HasStateChange => 
        !string.IsNullOrWhiteSpace(CurrentValue) && 
        !string.IsNullOrWhiteSpace(TargetValue) &&
        CurrentValue != "Unknown" &&
        !CurrentValue.Equals(TargetValue, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Formatted comparison text for UI display.
    /// </summary>
    public string ComparisonText => HasStateChange 
        ? $"Before: {BeforeState} → After: {AfterState}"
        : "No changes detected";

    public string ImpactAreaLabel => _impactAreaLabel;

    public bool HasCompactInfoLine => !string.IsNullOrWhiteSpace(ImpactAreaLabel);

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
            return $"{current} → {target}";
        }
    }

    public string CompactInfoTooltip
    {
        get
        {
            var baseText = $"{ImpactAreaLabel}: {CompactInfoLine}";
            if (HasBatchSummaryLine)
            {
                return $"{baseText}\n{BatchSummaryLine}";
            }

            return baseText;
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

    public ObservableCollection<string> BatchDetails => _batchDetails;

    public string BatchDetailsTitle
    {
        get => _batchDetailsTitle;
        private set => SetProperty(ref _batchDetailsTitle, value);
    }

    public bool HasBatchDetails => _batchDetails.Count > 0;

    public string BatchSummaryLine
    {
        get => _batchSummaryLine;
        private set
        {
            if (SetProperty(ref _batchSummaryLine, value))
            {
                OnPropertyChanged(nameof(HasBatchSummaryLine));
                OnPropertyChanged(nameof(CompactInfoTooltip));
            }
        }
    }

    public bool HasBatchSummaryLine => !string.IsNullOrWhiteSpace(_batchSummaryLine);

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

    private static string FormatStatusMessage(TweakAction action, TweakStatus status)
    {
        if (action == TweakAction.Detect && status == TweakStatus.Detected)
        {
            return "Current state captured.";
        }

        return status.ToString();
    }

    private static string CoalesceMessage(TweakAction action, TweakStatus status, string message)
    {
        return string.IsNullOrWhiteSpace(message) ? FormatStatusMessage(action, status) : message;
    }

    private static string FormatStepLogLine(TweakAction action, TweakStatus status, string message)
    {
        var details = CoalesceMessage(action, status, message);
        if (action == TweakAction.Detect &&
            details.StartsWith("Detected ", StringComparison.OrdinalIgnoreCase))
        {
            details = $"Found {details["Detected ".Length..]}";
        }
        return $"> {action}: {details}";
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
                OnPropertyChanged(nameof(StatusTooltip));
                _toggleCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsApplied => AppliedStatus == TweakAppliedStatus.Applied;

    public bool WasRolledBack
    {
        get => _wasRolledBack;
        private set => SetProperty(ref _wasRolledBack, value);
    }

    public string StatusIcon => AppliedStatus switch
    {
        _ when ShouldShowMixedStatus => "M",
        TweakAppliedStatus.Applied => "+",
        TweakAppliedStatus.NotApplied => "o",
        TweakAppliedStatus.Error => "x",
        _ when RequiresAdminScan => "!",
        _ => "?"
    };

    public Brush StatusColor => AppliedStatus switch
    {
        _ when ShouldShowMixedStatus => MixedStatusBrush,
        TweakAppliedStatus.Applied => AppliedStatusBrush,
        TweakAppliedStatus.NotApplied => NotAppliedStatusBrush,
        TweakAppliedStatus.Error => ErrorStatusBrush,
        _ when RequiresAdminScan => NotAppliedStatusBrush,
        _ => UnknownStatusBrush
    };

    public Brush StatusBadgeBackground => AppliedStatus switch
    {
        _ when ShouldShowMixedStatus => MixedStatusBackgroundBrush,
        TweakAppliedStatus.Applied => AppliedStatusBackgroundBrush,
        TweakAppliedStatus.NotApplied => NotAppliedStatusBackgroundBrush,
        TweakAppliedStatus.Error => ErrorStatusBackgroundBrush,
        _ when RequiresAdminScan => NotAppliedStatusBackgroundBrush,
        _ => UnknownStatusBackgroundBrush
    };

    public string StatusText => AppliedStatus switch
    {
        _ when ShouldShowMixedStatus => "Mixed",
        TweakAppliedStatus.Applied => "Applied",
        TweakAppliedStatus.NotApplied => "Not Applied",
        TweakAppliedStatus.Error => "Error",
        _ when RequiresAdminScan => "Needs Admin",
        _ => "Unknown"
    };

    private bool ShouldShowMixedStatus =>
        AppliedStatus is not TweakAppliedStatus.Error
        && !string.IsNullOrWhiteSpace(CurrentValue)
        && CurrentValue.Contains("Mixed", StringComparison.OrdinalIgnoreCase);

    private bool RequiresAdminScan =>
        AppliedStatus == TweakAppliedStatus.Unknown
        && WillPromptForDetect;

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
        TweakRunOutcome.RolledBack => "Rolled Back",
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
            SetProperty(ref _isDetailsExpanded, value);
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
            LastOutcome = report.RolledBack
                ? TweakRunOutcome.RolledBack
                : report.Succeeded ? TweakRunOutcome.Success : TweakRunOutcome.Failed;
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
            WasRolledBack = true;
            return;
        }

        if (report.Verified || report.Applied)
        {
            AppliedStatus = TweakAppliedStatus.Applied;
            WasRolledBack = false;
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
            AppendToTerminal(FormatStepLogLine(action, result.Result.Status, result.Result.Message));
            UpdateAfterSingleStep(action, result.Result);
            LastOutcome = MapOutcome(result.Result.Status);
            StatusMessage = CoalesceMessage(action, result.Result.Status, result.Result.Message);
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
                SetDetectionTimestamp(result.Timestamp, fromCache: false);
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
                    WasRolledBack = false;
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
                    WasRolledBack = false;
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
                    WasRolledBack = true;
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
            TweakStatus.RolledBack => TweakRunOutcome.RolledBack,
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

        AppendToTerminal(FormatStepLogLine(update.Action, update.Status, update.Message));

        StatusMessage = CoalesceMessage(update.Action, update.Status, update.Message);
        LastUpdatedText = $"Last update: {update.Timestamp.ToLocalTime():HH:mm:ss}";

        if (update.Action == TweakAction.Detect)
        {
            TryUpdateCurrentValueFromMessage(update.Message);
            SetDetectionTimestamp(update.Timestamp, fromCache: false);
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
        if (TrySetClipboardText(Id, out var error))
        {
            StatusMessage = "Tweak ID copied to clipboard.";
            return;
        }

        StatusMessage = $"Copy failed: {error}";
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
        if (TrySetClipboardText(RegistryPath, out var error))
        {
            StatusMessage = "Registry path copied to clipboard.";
            return;
        }

        StatusMessage = $"Copy failed: {error}";
    }

    private static bool TrySetClipboardText(string text, out string? errorMessage)
    {
        const int ClipboardBusy = unchecked((int)0x800401D0);
        const int ClipboardCantEmpty = unchecked((int)0x800401D1);
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            errorMessage = "Nothing to copy.";
            return false;
        }

        for (var attempt = 0; attempt < 4; attempt++)
        {
            try
            {
                if (Application.Current?.Dispatcher?.CheckAccess() == true)
                {
                    Clipboard.SetText(text);
                }
                else if (Application.Current?.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke(() => Clipboard.SetText(text));
                }
                else
                {
                    Clipboard.SetText(text);
                }

                return true;
            }
            catch (COMException ex) when (ex.HResult == ClipboardBusy || ex.HResult == ClipboardCantEmpty)
            {
                Thread.Sleep(30 * (attempt + 1));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        errorMessage = "Clipboard is busy. Try again.";
        return false;
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
            if (TryOpenFileAnchor(url))
            {
                StatusMessage = "Opening catalog entry...";
                return;
            }
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

    private static bool TryOpenFileAnchor(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri)
            && (string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var (rawPath, anchor) = SplitAnchor(url);
        if (TryResolveLocalPath(rawPath, out var localPath))
        {
            return TryOpenLocalPath(localPath, anchor);
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out absoluteUri))
        {
            if (!absoluteUri.IsFile)
            {
                return false;
            }

            var absoluteAnchor = absoluteUri.Fragment;
            var trimmedAnchor = string.IsNullOrWhiteSpace(absoluteAnchor)
                ? anchor
                : absoluteAnchor.TrimStart('#');
            return TryOpenLocalPath(absoluteUri.LocalPath, trimmedAnchor);
        }

        var hashIndex = url.IndexOf('#', StringComparison.Ordinal);
        var path = hashIndex > 0 ? url.Substring(0, hashIndex) : url;
        var fallbackAnchor = hashIndex > 0 ? url.Substring(hashIndex + 1) : string.Empty;

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        return TryOpenLocalPath(path, fallbackAnchor);
    }

    private static bool TryOpenLocalPath(string localPath, string? anchor = null)
    {
        if (string.IsNullOrWhiteSpace(localPath) || !File.Exists(localPath))
        {
            return false;
        }

        var extension = Path.GetExtension(localPath);
        var allowAnchor = string.Equals(extension, ".html", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(extension, ".htm", StringComparison.OrdinalIgnoreCase);

        if (allowAnchor && !string.IsNullOrWhiteSpace(anchor))
        {
            var escapedAnchor = Uri.EscapeDataString(anchor);
            var fileUri = new Uri(localPath, UriKind.Absolute);
            var anchoredUri = new Uri(fileUri.AbsoluteUri + "#" + escapedAnchor, UriKind.Absolute);
            Process.Start(new ProcessStartInfo
            {
                FileName = anchoredUri.AbsoluteUri,
                UseShellExecute = true
            });
            return true;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = localPath,
            UseShellExecute = true
        });
        return true;
    }

    private static (string path, string? anchor) SplitAnchor(string url)
    {
        var hashIndex = url.IndexOf('#', StringComparison.Ordinal);
        if (hashIndex <= 0)
        {
            return (url, null);
        }

        return (url.Substring(0, hashIndex), url.Substring(hashIndex + 1));
    }

    private static bool TryResolveLocalPath(string path, out string localPath)
    {
        localPath = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (Path.IsPathRooted(path) && File.Exists(path))
        {
            localPath = path;
            return true;
        }

        var docsRoot = DocsLocator.TryFindDocsRoot();
        var repoRoot = string.IsNullOrWhiteSpace(docsRoot)
            ? string.Empty
            : Directory.GetParent(docsRoot)?.FullName ?? string.Empty;
        var normalized = path.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);

        if (!string.IsNullOrWhiteSpace(repoRoot))
        {
            var repoCandidate = Path.Combine(repoRoot, normalized);
            if (File.Exists(repoCandidate))
            {
                localPath = repoCandidate;
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(docsRoot))
        {
            var trimmed = normalized;
            if (trimmed.StartsWith("Docs", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(4).TrimStart(Path.DirectorySeparatorChar);
            }

            var docsCandidate = Path.Combine(docsRoot, trimmed);
            if (File.Exists(docsCandidate))
            {
                localPath = docsCandidate;
                return true;
            }
        }

        return false;
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

    public TweakInventoryState ExportInventoryState()
    {
        return new TweakInventoryState
        {
            Id = Id,
            AppliedStatus = AppliedStatus.ToString(),
            CurrentValue = CurrentValue,
            TargetValue = TargetValue,
            LastDetectedAtUtc = LastDetectedAtUtc,
            ImpactArea = ImpactAreaLabel
        };
    }

    public void ApplyCachedInventoryState(TweakInventoryState cachedState)
    {
        if (cachedState is null)
        {
            return;
        }

        if (!string.Equals(cachedState.Id, Id, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        AppliedStatus = ParseAppliedStatus(cachedState.AppliedStatus);

        if (!string.IsNullOrWhiteSpace(cachedState.CurrentValue))
        {
            CurrentValue = cachedState.CurrentValue;
        }

        if (!string.IsNullOrWhiteSpace(cachedState.TargetValue))
        {
            TargetValue = cachedState.TargetValue;
        }

        if (cachedState.LastDetectedAtUtc.HasValue)
        {
            SetDetectionTimestamp(cachedState.LastDetectedAtUtc.Value, fromCache: true);
            LastUpdatedText = $"Last update: {cachedState.LastDetectedAtUtc.Value.ToLocalTime():HH:mm:ss}";
        }
    }

    private static TweakAppliedStatus ParseAppliedStatus(string? statusText)
    {
        if (string.IsNullOrWhiteSpace(statusText))
        {
            return TweakAppliedStatus.Unknown;
        }

        return Enum.TryParse<TweakAppliedStatus>(statusText, ignoreCase: true, out var parsed)
            ? parsed
            : TweakAppliedStatus.Unknown;
    }

    private void SetDetectionTimestamp(DateTimeOffset timestamp, bool fromCache)
    {
        var normalized = timestamp == default ? DateTimeOffset.UtcNow : timestamp.ToUniversalTime();
        LastDetectedAtUtc = normalized;
        IsStateFromCache = fromCache;
    }

    /// <summary>
    /// Detect if tweak is currently applied
    /// </summary>
    public Task DetectStatusAsync()
    {
        return DetectStatusAsync(CancellationToken.None);
    }

    public async Task DetectStatusAsync(CancellationToken ct)
    {
        if (IsRunning)
        {
            return;
        }

        try
        {
            ct.ThrowIfCancellationRequested();
            var result = await _pipeline.ExecuteStepAsync(_tweak, TweakAction.Detect, null, ct);

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
            SetDetectionTimestamp(result.Result.Timestamp, fromCache: false);

            if (CurrentValue == "Unknown" && result.Result.Status is TweakStatus.Applied or TweakStatus.Verified)
            {
                CurrentValue = TargetValue;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
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
            ClearBatchDetails();
            return;
        }

        TryUpdateBatchDetailsFromMessage(message);

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

            var entriesIndex = trimmed.IndexOf("Entries", StringComparison.OrdinalIgnoreCase);
            if (entriesIndex >= 0)
            {
                trimmed = trimmed[..entriesIndex];
            }

            var valuesIndex = trimmed.IndexOf("Values", StringComparison.OrdinalIgnoreCase);
            if (valuesIndex >= 0)
            {
                trimmed = trimmed[..valuesIndex];
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

    private void TryUpdateBatchDetailsFromMessage(string message)
    {
        if (!TryExtractBatchDetails(message, out var title, out var lines))
        {
            ClearBatchDetails();
            return;
        }

        BatchDetailsTitle = title;
        _batchDetails.Clear();
        foreach (var line in lines)
        {
            _batchDetails.Add(line);
        }

        BatchSummaryLine = BuildBatchSummary(title, lines);
    }

    private void ClearBatchDetails()
    {
        if (_batchDetails.Count == 0)
        {
            BatchSummaryLine = string.Empty;
            return;
        }

        _batchDetails.Clear();
        BatchSummaryLine = string.Empty;
    }

    private static bool TryExtractBatchDetails(string message, out string title, out List<string> lines)
    {
        lines = new List<string>();
        title = string.Empty;

        var markers = new[]
        {
            ("Services:", "Services"),
            ("Tasks:", "Tasks"),
            ("Entries:", "Registry Values"),
            ("Values:", "Registry Values")
        };

        foreach (var (marker, markerTitle) in markers)
        {
            var index = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                continue;
            }

            var start = index + marker.Length;
            if (start >= message.Length)
            {
                continue;
            }

            var detailText = message[start..];
            var parsedLines = detailText
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .Select(line => line.StartsWith("-", StringComparison.Ordinal) ? line : $"- {line}")
                .ToList();

            if (parsedLines.Count == 0)
            {
                continue;
            }

            title = markerTitle;
            lines = parsedLines;
            return true;
        }

        return false;
    }

    private static string BuildBatchSummary(string title, IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
        {
            return string.Empty;
        }

        var matched = 0;
        var missing = 0;
        var mismatched = 0;
        var errors = 0;
        var unknown = 0;

        foreach (var line in lines)
        {
            var lower = line.ToLowerInvariant();
            if (lower.Contains("missing"))
            {
                missing++;
                continue;
            }

            if (lower.Contains("error"))
            {
                errors++;
                continue;
            }

            if (lower.Contains("unknown"))
            {
                unknown++;
                continue;
            }

            if (title.Equals("Tasks", StringComparison.OrdinalIgnoreCase))
            {
                if (lower.Contains("disabled"))
                {
                    matched++;
                }
                else if (lower.Contains("enabled"))
                {
                    mismatched++;
                }
                else
                {
                    unknown++;
                }

                continue;
            }

            if (TryEvaluateArrowMatch(line, out var isMatch))
            {
                if (isMatch)
                {
                    matched++;
                }
                else
                {
                    mismatched++;
                }
            }
            else
            {
                unknown++;
            }
        }

        var parts = new List<string>
        {
            $"{matched} matched",
            $"{missing} missing"
        };

        if (mismatched > 0)
        {
            parts.Add($"{mismatched} mismatched");
        }

        if (errors > 0)
        {
            parts.Add($"{errors} error{(errors == 1 ? string.Empty : "s")}");
        }

        if (unknown > 0)
        {
            parts.Add($"{unknown} unknown");
        }

        return string.Join(" / ", parts);
    }

    private static bool TryEvaluateArrowMatch(string line, out bool isMatch)
    {
        isMatch = false;

        var arrowIndex = line.IndexOf('→');
        var arrowLength = 1;
        if (arrowIndex < 0)
        {
            arrowIndex = line.IndexOf("->", StringComparison.Ordinal);
            arrowLength = 2;
        }

        if (arrowIndex < 0)
        {
            return false;
        }

        var colonIndex = line.IndexOf(':');
        var currentStart = colonIndex >= 0 ? colonIndex + 1 : 0;
        if (currentStart >= arrowIndex)
        {
            return false;
        }

        var current = line[currentStart..arrowIndex].Trim();
        var target = line[(arrowIndex + arrowLength)..].Trim();
        if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(target))
        {
            return false;
        }

        var currentValue = current.Split('(')[0].Trim();
        var targetValue = target.Split('(')[0].Trim();
        if (string.IsNullOrWhiteSpace(currentValue) || string.IsNullOrWhiteSpace(targetValue))
        {
            return false;
        }

        isMatch = currentValue.Equals(targetValue, StringComparison.OrdinalIgnoreCase)
            || currentValue.Contains(targetValue, StringComparison.OrdinalIgnoreCase);
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

    private void LogToFile(string message)
    {
        try
        {
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WindowsOptimizer", "Logs", "tweak-vm.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
        }
        catch
        {
            // Ignore logging failures
        }
    }

    /// <summary>
    /// Generates a rich tooltip for contextual help.
    /// </summary>
    private string GenerateHelpTooltip()
    {
        var tooltip = Description;
        var implications = GenerateImplications();
        
        if (!string.IsNullOrEmpty(implications))
        {
            tooltip += $"\n\n{implications}";
        }

        return tooltip;
    }

    /// <summary>
    /// Generates implications text based on tweak category and risk.
    /// </summary>
    private string GenerateImplications()
    {
        var implications = new List<string>();

        // Risk-based implications
        switch (Risk)
        {
            case TweakRiskLevel.Safe:
                implications.Add("✓ Safe to apply - minimal system impact");
                break;
            case TweakRiskLevel.Advanced:
                implications.Add("⚠ Advanced - may affect some features");
                break;
            case TweakRiskLevel.Risky:
                implications.Add("⚠️ Risky - could impact system stability");
                break;
        }

        // Category-based implications
        var category = Category.ToLowerInvariant();
        switch (category)
        {
            case "privacy":
                implications.Add("🔒 Affects: Privacy & data collection");
                break;
            case "performance":
                implications.Add("🚀 Affects: System responsiveness");
                break;
            case "security":
                implications.Add("🛡️ Affects: System security posture");
                break;
            case "network":
                implications.Add("🌐 Affects: Network connectivity");
                break;
            case "visibility":
                implications.Add("👀 Affects: UI elements & visual features");
                break;
        }

        // Elevation implications
        if (RequiresElevation)
        {
            implications.Add("🔑 Requires administrator privileges");
        }

        return string.Join("\n", implications);
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
    public ReferenceLink(string title, string url, string? tooltip = null, ReferenceLinkKind kind = ReferenceLinkKind.Other)
    {
        Title = title;
        Url = url;
        Tooltip = string.IsNullOrWhiteSpace(tooltip) ? url : tooltip;
        Kind = kind;
    }
    public string Title { get; }
    public string Url { get; }
    public string Tooltip { get; }
    public ReferenceLinkKind Kind { get; }
    public string Icon => Kind switch
    {
        ReferenceLinkKind.Catalog => "📚",
        ReferenceLinkKind.Details => "🧩",
        ReferenceLinkKind.Docs => "📘",
        ReferenceLinkKind.Source => "📄",
        _ => "🔗"
    };
}

public enum ReferenceLinkKind
{
    Catalog,
    Details,
    Docs,
    Source,
    Other
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

