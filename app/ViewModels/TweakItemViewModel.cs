using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Services;
using RegProbe.Engine;
using RegProbe.Engine.Tweaks;
using RegProbe.Engine.Tweaks.Commands;
using RegProbe.Engine.Tweaks.Commands.Cleanup;
using RegProbe.Infrastructure;
using RegProbe.App.Services;

namespace RegProbe.App.ViewModels;

public sealed class TweakItemViewModel : ViewModelBase
{
    private const int MaxBatchDetailLines = 200;
    private const int MaxDisplayMessageLength = 1024;
    private static readonly TweakInsightFormatter InsightFormatter = new();

    private readonly ITweak _tweak;
    private readonly TweakExecutionPipeline _pipeline;
    private readonly bool _isElevated;
    private readonly RelayCommand _detectCommand;
    private readonly RelayCommand _previewCommand;
    private readonly RelayCommand _applyCommand;
    private readonly RelayCommand _verifyCommand;
    private readonly RelayCommand _rollbackCommand;
    private readonly RelayCommand _restoreDefaultCommand;
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
    private bool _isSyncingChoiceOption;
    private bool _hasNohutoEvidence;
    private bool _hasWindowsInternalsContext;
    private bool _needsSourceReview;
    private string _provenanceSummary = string.Empty;
    private string _evidenceClassId = "D";
    private string _evidenceClassLabel = "Class D";
    private string _evidenceClassTitle = "Key Known, Value Semantics Unknown";
    private string _evidenceClassDescription = "The key exists, but value semantics are not trusted enough yet for an app-ready surface.";
    private string _evidenceClassActionState = "research-gated";
    private string _evidenceClassGatingReason = "Evidence pending";
    private bool _isEvidenceClassActionable;
    private bool _showInApp = true;
    private string _tweakOrigin = "legacy-curated";
    private string _promotionState = "promoted";
    private string _promotionGatingReason = string.Empty;
    private bool _isPromotionActionable = true;
    private bool _debugOverrideAllowed;
    private string _validatedSemanticsSummary = string.Empty;
    private string _validatedSemanticsSource = string.Empty;
    private string _runtimeProofSummary = string.Empty;
    private string _runtimeProofSource = string.Empty;
    private string _upstreamLineageSummary = string.Empty;
    private string _upstreamLineageSource = string.Empty;
    private bool _restoreStoryKnown;
    private bool _isEvidenceArchived;
    private bool _hasSemanticsEvidenceFlag;
    private bool _needsVmValidationFlag;
    private bool _hasRuntimeEvidenceFlag;
    private bool _hasLineageEvidenceFlag;
    private bool _rollbackDeclared;
    private bool _rollbackExecuted;
    private bool _rollbackVerified;
    private string _rollbackVerificationMethod = string.Empty;
    private string _rollbackFailureReason = string.Empty;
    private readonly RelayCommand _toggleFavoriteCommand;
    private readonly ObservableCollection<string> _batchDetails = new();
    private string _batchDetailsTitle = "Details";
    private string _batchSummaryLine = string.Empty;
    private TweakChoiceOption? _selectedChoiceOption;

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
        ReferenceLinks.CollectionChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(HasReferenceLinks));
            OnPropertyChanged(nameof(UserReferenceLinks));
            OnPropertyChanged(nameof(HasUserReferenceLinks));
            RaiseVerdictSnapshotChanged();
        };
        SubOptions = new ObservableCollection<TweakSubOption>();
        ChoiceOptions = new ObservableCollection<TweakChoiceOption>();
        ChoiceOptions.CollectionChanged += (_, __) => OnPropertyChanged(nameof(HasChoiceOptions));

        ResetSteps();

        _detectCommand = new RelayCommand(_ => _ = RunSingleStepAsync(TweakAction.Detect, CancellationToken.None), _ => CanInspect());
        _previewCommand = new RelayCommand(_ => _ = RunAsync(true, CancellationToken.None), _ => CanInspect());
        _applyCommand = new RelayCommand(_ => _ = RunAsync(false, CancellationToken.None), _ => CanMutate());
        _verifyCommand = new RelayCommand(_ => _ = RunSingleStepAsync(TweakAction.Verify, CancellationToken.None), _ => CanInspect());
        _rollbackCommand = new RelayCommand(_ => _ = RunSingleStepAsync(TweakAction.Rollback, CancellationToken.None), _ => CanMutate());
        _restoreDefaultCommand = new RelayCommand(_ => _ = RestoreDefaultAsync(), _ => CanRestoreDefault());
        _cancelCommand = new RelayCommand(_ => CancelRun(), _ => CanCancel());
        _copyIdCommand = new RelayCommand(_ => CopyId());
        _toggleCommand = new RelayCommand(_ => _ = ToggleAsync(), _ => CanToggle());
        _customActionCommand = new RelayCommand(_ => _ = RunCustomActionAsync(), _ => CanMutate());
        _copyRegistryPathCommand = new RelayCommand(_ => CopyRegistryPath(), _ => !string.IsNullOrEmpty(RegistryPath));
        _openReferenceLinkCommand = new RelayCommand(OpenReferenceLink, parameter => parameter is string url && !string.IsNullOrWhiteSpace(url));
        _toggleFavoriteCommand = new RelayCommand(_ => ToggleFavorite());

        _impactAreaLabel = TweakCategoryPresentation.DetermineImpactAreaLabel(_tweak);
        _batchDetails.CollectionChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(HasBatchDetails));
            RaiseInsightPropertiesChanged();
        };

        TryPopulateTechnicalInfo();
    }

    public string Name => _tweak.Name;

    public string Id => _tweak.Id;

    public string Description => _tweak.Description;

    /// <summary>
    /// Rich tooltip explaining implications of this tweak.
    /// </summary>
    public string HelpTooltip => TweakItemPresentationFormatter.BuildHelpTooltip(Description, Implications);

    /// <summary>
    /// Implications of enabling/disabling this tweak.
    /// </summary>
    public string Implications => TweakItemPresentationFormatter.BuildImplications(Risk, Category, RequiresElevation);

    public TweakRiskLevel Risk => _tweak.Risk;

    public bool IsSafeRisk => Risk == TweakRiskLevel.Safe;

    public bool IsAdvancedRisk => Risk == TweakRiskLevel.Advanced;

    public bool IsRisky => Risk == TweakRiskLevel.Risky;

    public string RiskBadgeText => TweakSurfacePresentation.BuildRiskBadgeText(Risk);

    public string RepairsRiskHint => TweakSurfacePresentation.BuildRepairsRiskHint(Risk);

    public bool HasRepairsRiskHint => !string.IsNullOrWhiteSpace(RepairsRiskHint);

    public bool RequiresElevation => _tweak.RequiresElevation;

    public bool IsElevated => _isElevated;

    public bool WillPromptForElevation => RequiresElevation && !IsElevated;

    public bool WillPromptForDetect =>
        RequiresElevation
        && !IsElevated
        && _tweak is not RegistryValueTweak
        && _tweak is not RegistryValueBatchTweak
        && _tweak is not RegistryValueSetTweak
        && _tweak is not RegistryValuePresetBatchTweak;

    public bool IsScanFriendly =>
        _tweak is not CommandTweak
        && _tweak is not FileCleanupTweak;

    public bool IsStartupScanEligible =>
        IsScanFriendly
        && !WillPromptForDetect;

    public string ElevationBadgeText => TweakSurfacePresentation.ElevationBadgeText;

    public string ElevationTooltip => TweakSurfacePresentation.BuildElevationTooltip(IsElevated);

    public string ElevationWarningText => TweakSurfacePresentation.BuildElevationWarningText(WillPromptForElevation);

    public string Category => TweakCategoryPresentation.ExtractCategory(Id);

    public string CategoryIcon => TweakCategoryPresentation.GetCategoryIcon(Category);

    public string StatusTooltip => TweakStatusPresentation.BuildTooltip(
        AppliedStatus,
        ShouldShowMixedStatus,
        RequiresAdminScan);

    public string ActionsHelpTooltip => TweakSurfacePresentation.ActionsHelpTooltip;

    public ObservableCollection<TweakStepStatusViewModel> Steps { get; }

    public ICommand DetectCommand => _detectCommand;

    public ICommand PreviewCommand => _previewCommand;

    public ICommand ApplyCommand => _applyCommand;

    public ICommand VerifyCommand => _verifyCommand;

    public ICommand RollbackCommand => _rollbackCommand;

    public ICommand RestoreDefaultCommand => _restoreDefaultCommand;

    public ICommand CancelCommand => _cancelCommand;

    public ICommand CopyIdCommand => _copyIdCommand;

    public ICommand ToggleCommand => _toggleCommand;

    public ICommand CustomActionCommand => _customActionCommand;

    public ICommand CopyRegistryPathCommand => _copyRegistryPathCommand;

    public ICommand OpenReferenceLinkCommand => _openReferenceLinkCommand;

    public TweakActionType ActionType
    {
        get => _actionType;
        set
        {
            if (SetProperty(ref _actionType, value))
            {
                RaiseInsightPropertiesChanged();
            }
        }
    }

    public string ActionButtonText
    {
        get => _actionButtonText;
        set
        {
            if (SetProperty(ref _actionButtonText, value))
            {
                OnPropertyChanged(nameof(RepairsActionButtonText));
            }
        }
    }

    public string RepairsActionButtonText => TweakSurfacePresentation.BuildRepairsActionButtonText(ActionButtonText);

    public string RegistryPath
    {
        get => _registryPath;
        set
        {
            if (SetProperty(ref _registryPath, value))
            {
                OnPropertyChanged(nameof(HasRegistryPath));
                OnPropertyChanged(nameof(HasDiff));
                OnPropertyChanged(nameof(ScopeFilterKey));
                OnPropertyChanged(nameof(ScopeDisplayText));
                RaiseInsightPropertiesChanged();
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

    public IEnumerable<ReferenceLink> UserReferenceLinks =>
        ReferenceLinks.Where(static link => link.Kind != ReferenceLinkKind.Source && link.Kind != ReferenceLinkKind.Catalog);

    public ObservableCollection<TweakSubOption> SubOptions { get; }

    public bool HasSubOptions => SubOptions.Any();

    public ObservableCollection<TweakChoiceOption> ChoiceOptions { get; }

    public bool HasChoiceOptions => ChoiceOptions.Any();

    public TweakChoiceOption? SelectedChoiceOption
    {
        get => _selectedChoiceOption;
        set
        {
            if (!SetProperty(ref _selectedChoiceOption, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SelectedChoiceDescription));

            if (_isSyncingChoiceOption || value is null || _tweak is not IChoiceTweak choiceTweak)
            {
                return;
            }

            choiceTweak.SelectedChoiceKey = value.Key;
            ApplyChoiceStateSnapshot(TweakChoiceStateCoordinator.BuildSelectionSnapshot(
                value,
                choiceTweak.MatchedChoiceKey,
                choiceTweak.MatchedChoiceLabel));
        }
    }

    public string SelectedChoiceDescription => SelectedChoiceOption?.Description ?? string.Empty;

    private TweakGuidance? Guidance => (_tweak as ITweakWithGuidance)?.Guidance;

    public string FriendlyDescription => TweakItemPresentationFormatter.BuildFriendlyDescription(
        Guidance?.CasualSummary,
        Description);

    public string ConfigurationFriendlyDescription => TweakItemPresentationFormatter.BuildConfigurationFriendlyDescription(FriendlyDescription);

    public string GuidanceWhenHelpful => Guidance?.WhenHelpful ?? string.Empty;

    public bool HasGuidanceWhenHelpful => TweakItemPresentationFormatter.HasText(GuidanceWhenHelpful);

    public string GuidanceTradeoffs => Guidance?.Tradeoffs ?? string.Empty;

    public bool HasGuidanceTradeoffs => TweakItemPresentationFormatter.HasText(GuidanceTradeoffs);

    public string DefaultVsPreviousSummary => TweakItemPresentationFormatter.BuildDefaultVsPreviousSummary(
        Guidance?.DefaultVsPrevious,
        HasDefaultChoice);

    public bool HasDefaultVsPreviousSummary => TweakItemPresentationFormatter.HasText(DefaultVsPreviousSummary);

    public string ProfessionalNotes => Guidance?.ProfessionalNotes ?? string.Empty;

    public bool HasProfessionalNotes => TweakItemPresentationFormatter.HasText(ProfessionalNotes);

    public bool HasExtendedGuidance => TweakItemPresentationFormatter.HasExtendedGuidance(
        HasGuidanceWhenHelpful,
        HasGuidanceTradeoffs,
        HasDefaultVsPreviousSummary,
        HasProfessionalNotes);

    public bool HasDefaultChoice =>
        _tweak is IChoiceTweak choiceTweak &&
        !string.IsNullOrWhiteSpace(choiceTweak.DefaultChoiceKey);

    public string RestoreDefaultButtonText => TweakItemPresentationFormatter.BuildRestoreDefaultButtonText(HasDefaultChoice);

    public string RestoreDefaultTooltip => TweakRollbackPresentation.BuildRestoreDefaultTooltip(
        IsMutationAllowed,
        PublicMutationGatingReason,
        _tweak is IChoiceTweak choiceTweak ? choiceTweak.DefaultChoiceLabel ?? string.Empty : string.Empty);

    public bool HasRegistryPath => !string.IsNullOrEmpty(RegistryPath);

    public bool HasCodeExample => !string.IsNullOrEmpty(CodeExample);

    public bool HasReferenceLinks => ReferenceLinks.Any();

    public bool HasUserReferenceLinks => UserReferenceLinks.Any();

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
                OnPropertyChanged(nameof(ConfigurationCompactInfoLine));
                OnPropertyChanged(nameof(CompactInfoTooltip));
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(StatusBorderBrush));
                OnPropertyChanged(nameof(StatusTextBrush));
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
                OnPropertyChanged(nameof(ConfigurationCompactInfoLine));
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
                OnPropertyChanged(nameof(ConfigurationInventoryFreshnessText));
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
                OnPropertyChanged(nameof(ConfigurationInventoryFreshnessText));
            }
        }
    }

    public bool HasDetectedState => LastDetectedAtUtc.HasValue;

    public string InventoryFreshnessText => TweakInventoryPresentation.BuildInventoryFreshnessText(
        LastDetectedAtUtc,
        IsStateFromCache);

    public string ConfigurationInventoryFreshnessText => TweakInventoryPresentation.BuildConfigurationInventoryFreshnessText(
        LastDetectedAtUtc,
        IsStateFromCache);

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
        ? $"Before: {BeforeState} -> After: {AfterState}"
        : "No changes detected";

    public string ImpactAreaLabel => _impactAreaLabel;

    public string ConfigurationImpactAreaText => TweakItemPresentationFormatter.BuildConfigurationImpactAreaText(ImpactAreaLabel);

    public string EffectSummary => TweakItemPresentationFormatter.BuildEffectSummary(
        Risk,
        Category,
        RequiresElevation,
        WillPromptForDetect);

    public string DetectedFromSummary => BuildInsightSnapshot().DetectedFrom;

    public string AffectsSummary => BuildInsightSnapshot().Affects;

    public string RestartAdvice => BuildInsightSnapshot().RestartAdvice;

    public string RelatedSettingsSummary => BuildInsightSnapshot().RelatedSettings;

    public bool HasCompactInfoLine => !string.IsNullOrWhiteSpace(ImpactAreaLabel);

    public string CompactInfoLine => TweakItemPresentationFormatter.BuildCompactInfoLine(
        HasCompactInfoLine,
        CurrentValue,
        TargetValue);

    public string ConfigurationCompactInfoLine => TweakItemPresentationFormatter.BuildConfigurationCompactInfoLine(
        CurrentValue,
        TargetValue);

    public string CompactInfoTooltip => TweakItemPresentationFormatter.BuildCompactInfoTooltip(
        ImpactAreaLabel,
        CompactInfoLine,
        HasBatchSummaryLine,
        BatchSummaryLine);

    public string RowMetaText => TweakItemPresentationFormatter.BuildRowMetaText(
        ImpactAreaLabel,
        CurrentValue,
        TargetValue,
        HasDetectedState,
        InventoryFreshnessText,
        LastUpdatedText);

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

    public string EvidenceClassId => _evidenceClassId;

    public string EvidenceClassLabel => _evidenceClassLabel;

    public string EvidenceClassTitle => _evidenceClassTitle;

    public string EvidenceClassDescription => _evidenceClassDescription;

    public string EvidenceClassBadgeText => TweakEvidenceClassPresentation.BuildBadgeText(EvidenceClassId);

    public bool IsEvidenceConfirmed => IsEvidenceClassActionable;

    public string EvidenceStateText => TweakEvidenceClassPresentation.BuildEvidenceStateText(IsEvidenceConfirmed);

    public string EvidenceClassActionState => _evidenceClassActionState;

    public string EvidenceClassTooltip => TweakEvidenceClassPresentation.BuildTooltip(EvidenceClassTitle, EvidenceClassDescription);

    public string EvidenceClassGatingReason => _evidenceClassGatingReason;

    public string PublicEvidenceClassGatingReason =>
        TweakVerdictPresentation.BuildPublicEvidenceClassGatingReason(EvidenceClassGatingReason);

    public bool IsEvidenceClassActionable => _isEvidenceClassActionable;

    public bool ShowInApp => _showInApp;

    public string TweakOrigin => _tweakOrigin;

    public string PromotionState => _promotionState;

    public string PromotionGatingReason => _promotionGatingReason;

    public bool IsResearchDerived => string.Equals(_tweakOrigin, "research-derived", StringComparison.OrdinalIgnoreCase);

    public bool IsPromotionActionable => _isPromotionActionable;

    public bool CanDebugOverridePromotionGate => ContributorMode.IsEnabled && _debugOverrideAllowed;

    public bool IsMutationAllowed => IsEvidenceClassActionable && (IsPromotionActionable || CanDebugOverridePromotionGate);

    public string PublicMutationGatingReason =>
        TweakVerdictPresentation.BuildPublicMutationGatingReason(
            IsEvidenceClassActionable,
            PublicEvidenceClassGatingReason,
            IsMutationAllowed,
            PromotionGatingReason);

    public bool IsResearchGated => TweakVerdictPresentation.IsResearchGated(ShowInApp, IsMutationAllowed);

    public bool HasEvidenceClass => TweakEvidenceClassPresentation.HasEvidenceClass(EvidenceClassId);

    public Brush EvidenceClassBrush => TweakEvidenceClassPresentation.GetBrush(EvidenceClassId);

    public Brush EvidenceClassBackgroundBrush => TweakEvidenceClassPresentation.GetBackgroundBrush(EvidenceClassId);

    public string VerdictState => TweakVerdictPresentation.BuildVerdictState(
        _isEvidenceArchived,
        _evidenceClassActionState,
        ShowInApp,
        IsMutationAllowed,
        IsResearchGated,
        IsPromotionActionable,
        IsEvidenceClassActionable);

    public string VerdictText => TweakVerdictPresentation.BuildVerdictText(VerdictState);

    public string CompactStateText => TweakVerdictPresentation.BuildCompactStateText(VerdictState);

    public string CompactStateTone => TweakVerdictPresentation.BuildCompactStateTone(VerdictState);

    public string ScopeFilterKey => TweakSurfacePresentation.BuildScopeFilterKey(RegistryPath, RequiresElevation);

    public string ScopeDisplayText => TweakSurfacePresentation.BuildScopeDisplayText(ScopeFilterKey);

    public string VerdictSummary => TweakVerdictPresentation.BuildVerdictSummary(
        VerdictState,
        _rollbackVerified,
        IsEvidenceClassActionable);

    public string DocsSnapshotState => TweakProofSnapshotPresentation.BuildDocsSnapshotState(
        ReferenceLinks.Any(link => link.Kind is ReferenceLinkKind.Docs or ReferenceLinkKind.Details),
        _hasSemanticsEvidenceFlag,
        HasValidatedSemantics,
        _validatedSemanticsSource);

    public string DocsSnapshotText => TweakProofSnapshotPresentation.BuildSnapshotText("Docs", DocsSnapshotState);

    public string RuntimeSnapshotState => TweakProofSnapshotPresentation.BuildRuntimeSnapshotState(
        _hasRuntimeEvidenceFlag,
        HasRuntimeProof,
        _needsVmValidationFlag,
        _hasSemanticsEvidenceFlag,
        HasValidatedSemantics);

    public string RuntimeSnapshotText => TweakProofSnapshotPresentation.BuildSnapshotText("Runtime", RuntimeSnapshotState);

    public string SourceSnapshotState => TweakProofSnapshotPresentation.BuildSourceSnapshotState(
        _hasLineageEvidenceFlag,
        HasUpstreamLineage,
        HasNohutoEvidence,
        HasWindowsInternalsContext,
        NeedsSourceReview,
        ProvenanceSummary);

    public string SourceSnapshotText => TweakProofSnapshotPresentation.BuildSnapshotText("Source", SourceSnapshotState);

    public string RollbackSnapshotState => TweakProofSnapshotPresentation.BuildRollbackSnapshotState(
        _rollbackVerified,
        _rollbackDeclared,
        _restoreStoryKnown,
        HasDefaultChoice,
        _rollbackFailureReason);

    public string RollbackSnapshotText => TweakProofSnapshotPresentation.BuildSnapshotText("Rollback", RollbackSnapshotState);

    public string RiskSnapshotText => TweakProofSnapshotPresentation.BuildRiskSnapshotText(Risk, IsMutationAllowed);

    public string RollbackStoryText => TweakRollbackPresentation.BuildRollbackStoryText(
        _rollbackVerified,
        _rollbackVerificationMethod,
        _rollbackFailureReason,
        _rollbackDeclared,
        _rollbackExecuted,
        _restoreStoryKnown,
        HasDefaultChoice);

    public string ConfigurationPrimaryActionTooltip =>
        TweakRollbackPresentation.BuildConfigurationPrimaryActionTooltip(IsMutationAllowed, PublicMutationGatingReason);

    public string ConfigurationRollbackActionTooltip =>
        TweakRollbackPresentation.BuildConfigurationRollbackActionTooltip(IsMutationAllowed, PublicMutationGatingReason);

    public string PrimaryActionTooltip =>
        TweakRollbackPresentation.BuildPrimaryActionTooltip(IsMutationAllowed, PublicMutationGatingReason);

    public string RollbackActionTooltip =>
        TweakRollbackPresentation.BuildRollbackActionTooltip(IsMutationAllowed, PublicMutationGatingReason);

    public string ResearchGateMessage => TweakVerdictPresentation.BuildResearchGateMessage(IsMutationAllowed);

    public bool HasResearchGateMessage => !string.IsNullOrWhiteSpace(ResearchGateMessage);

    public string ValidatedSemanticsSummary => _validatedSemanticsSummary;

    public string ValidatedSemanticsSource => _validatedSemanticsSource;

    public bool HasValidatedSemantics => !string.IsNullOrWhiteSpace(ValidatedSemanticsSummary);

    public string RuntimeProofSummary => _runtimeProofSummary;

    public string RuntimeProofSource => _runtimeProofSource;

    public bool HasRuntimeProof => !string.IsNullOrWhiteSpace(RuntimeProofSummary);

    public string UpstreamLineageSummary => _upstreamLineageSummary;

    public string UpstreamLineageSource => _upstreamLineageSource;

    public bool HasUpstreamLineage => !string.IsNullOrWhiteSpace(UpstreamLineageSummary);

    public bool HasEvidenceProofBoxes => HasValidatedSemantics || HasRuntimeProof || HasUpstreamLineage;

    public bool HasNohutoEvidence
    {
        get => _hasNohutoEvidence;
        set
        {
            if (SetProperty(ref _hasNohutoEvidence, value))
            {
                OnPropertyChanged(nameof(HasProvenance));
                OnPropertyChanged(nameof(ProvenanceStatusText));
                RaiseVerdictSnapshotChanged();
            }
        }
    }

    public bool HasWindowsInternalsContext
    {
        get => _hasWindowsInternalsContext;
        set
        {
            if (SetProperty(ref _hasWindowsInternalsContext, value))
            {
                OnPropertyChanged(nameof(HasProvenance));
                OnPropertyChanged(nameof(ProvenanceStatusText));
                RaiseVerdictSnapshotChanged();
            }
        }
    }

    public bool NeedsSourceReview
    {
        get => _needsSourceReview;
        set
        {
            if (SetProperty(ref _needsSourceReview, value))
            {
                OnPropertyChanged(nameof(HasProvenance));
                OnPropertyChanged(nameof(ProvenanceStatusText));
                RaiseVerdictSnapshotChanged();
            }
        }
    }

    public string ProvenanceSummary
    {
        get => _provenanceSummary;
        set
        {
            if (SetProperty(ref _provenanceSummary, value))
            {
                OnPropertyChanged(nameof(HasProvenance));
                RaiseVerdictSnapshotChanged();
            }
        }
    }

    public bool HasProvenance =>
        HasNohutoEvidence ||
        HasWindowsInternalsContext ||
        NeedsSourceReview ||
        !string.IsNullOrWhiteSpace(ProvenanceSummary);

    public string ProvenanceStatusText
    {
        get
        {
            if (HasNohutoEvidence && HasWindowsInternalsContext)
            {
                return "Dump source + Internals";
            }

            if (HasNohutoEvidence)
            {
                return "Dump source";
            }

            if (HasWindowsInternalsContext)
            {
                return "Internals";
            }

            return NeedsSourceReview ? "Needs review" : "No source links";
        }
    }

    public void ApplyEvidenceClassification(TweakEvidenceClassEntry? entry)
    {
        entry ??= TweakEvidenceClassEntry.CreateFallback(Id);

        _evidenceClassId = string.IsNullOrWhiteSpace(entry.EvidenceClass) ? "D" : entry.EvidenceClass;
        _evidenceClassLabel = string.IsNullOrWhiteSpace(entry.ClassLabel) ? "Class D" : entry.ClassLabel;
        _evidenceClassTitle = string.IsNullOrWhiteSpace(entry.ClassTitle) ? "Key Known, Value Semantics Unknown" : entry.ClassTitle;
        _evidenceClassDescription = string.IsNullOrWhiteSpace(entry.ClassDescription)
            ? "The key exists, but value semantics are not trusted enough yet for an app-ready surface."
            : entry.ClassDescription;
        _evidenceClassActionState = string.IsNullOrWhiteSpace(entry.ActionState) ? "research-gated" : entry.ActionState;
        _evidenceClassGatingReason = string.IsNullOrWhiteSpace(entry.GatingReason)
            ? "Evidence pending"
            : entry.GatingReason;
        _isEvidenceClassActionable = entry.IsActionable;
        _showInApp = entry.ShowInApp;
        _validatedSemanticsSummary = entry.ValidatedSemantics?.Summary?.Trim() ?? string.Empty;
        _validatedSemanticsSource = entry.ValidatedSemantics?.PrimarySourceText?.Trim() ?? string.Empty;
        _runtimeProofSummary = entry.RuntimeProof?.Summary?.Trim() ?? string.Empty;
        _runtimeProofSource = entry.RuntimeProof?.PrimarySourceText?.Trim() ?? string.Empty;
        _upstreamLineageSummary = entry.UpstreamLineage?.Summary?.Trim() ?? string.Empty;
        _upstreamLineageSource = entry.UpstreamLineage?.PrimarySourceText?.Trim() ?? string.Empty;
        _restoreStoryKnown = entry.RestoreStoryKnown;
        _isEvidenceArchived = entry.IsArchived;
        _hasSemanticsEvidenceFlag = entry.ValidatedSemantics?.HasSemanticsEvidence ?? false;
        _needsVmValidationFlag = entry.ValidatedSemantics?.NeedsVmValidation == true
            || entry.RuntimeProof?.NeedsVmValidation == true;
        _hasRuntimeEvidenceFlag = entry.RuntimeProof?.HasRuntimeEvidence ?? false;
        _hasLineageEvidenceFlag = entry.UpstreamLineage?.HasNohutoLineage ?? false
            || entry.UpstreamLineage?.HasValidationProof == true;

        RaiseEvidenceClassificationChanged();
    }

    public void ApplyResearchPromotionGate(TweakPromotionGateEntry? entry)
    {
        entry ??= TweakPromotionGateEntry.CreateFallback(Id);

        _tweakOrigin = string.IsNullOrWhiteSpace(entry.TweakOrigin) ? "legacy-curated" : entry.TweakOrigin;
        _promotionState = string.IsNullOrWhiteSpace(entry.PromotionState) ? "promoted" : entry.PromotionState;
        _isPromotionActionable =
            string.Equals(_tweakOrigin, "legacy-curated", StringComparison.OrdinalIgnoreCase)
            || string.Equals(_promotionState, "promoted", StringComparison.OrdinalIgnoreCase);
        _debugOverrideAllowed = entry.DebugOverrideAllowed;
        _promotionGatingReason = string.IsNullOrWhiteSpace(entry.GatingReason)
            ? "Promotion pending."
            : entry.GatingReason;
        _rollbackDeclared = entry.RollbackStatus?.RollbackDeclared ?? false;
        _rollbackExecuted = entry.RollbackStatus?.RollbackExecuted ?? false;
        _rollbackVerified = entry.RollbackStatus?.RollbackVerified ?? false;
        _rollbackVerificationMethod = entry.RollbackStatus?.RollbackVerificationMethod?.Trim() ?? string.Empty;
        _rollbackFailureReason = entry.RollbackStatus?.RollbackFailureReason?.Trim() ?? string.Empty;

        RaisePromotionGateChanged();
    }

    private void RaiseEvidenceClassificationChanged()
    {
        OnPropertyChanged(nameof(EvidenceClassId));
        OnPropertyChanged(nameof(EvidenceClassLabel));
        OnPropertyChanged(nameof(EvidenceClassTitle));
        OnPropertyChanged(nameof(EvidenceClassDescription));
        OnPropertyChanged(nameof(EvidenceClassBadgeText));
        OnPropertyChanged(nameof(IsEvidenceConfirmed));
        OnPropertyChanged(nameof(EvidenceStateText));
        OnPropertyChanged(nameof(EvidenceClassActionState));
        OnPropertyChanged(nameof(EvidenceClassTooltip));
        OnPropertyChanged(nameof(EvidenceClassGatingReason));
        OnPropertyChanged(nameof(IsEvidenceClassActionable));
        OnPropertyChanged(nameof(ShowInApp));
        OnPropertyChanged(nameof(IsMutationAllowed));
        OnPropertyChanged(nameof(IsResearchGated));
        OnPropertyChanged(nameof(HasEvidenceClass));
        OnPropertyChanged(nameof(EvidenceClassBrush));
        OnPropertyChanged(nameof(EvidenceClassBackgroundBrush));
        OnPropertyChanged(nameof(ConfigurationPrimaryActionTooltip));
        OnPropertyChanged(nameof(ConfigurationRollbackActionTooltip));
        OnPropertyChanged(nameof(PrimaryActionTooltip));
        OnPropertyChanged(nameof(RollbackActionTooltip));
        OnPropertyChanged(nameof(ResearchGateMessage));
        OnPropertyChanged(nameof(HasResearchGateMessage));
        OnPropertyChanged(nameof(ValidatedSemanticsSummary));
        OnPropertyChanged(nameof(ValidatedSemanticsSource));
        OnPropertyChanged(nameof(HasValidatedSemantics));
        OnPropertyChanged(nameof(RuntimeProofSummary));
        OnPropertyChanged(nameof(RuntimeProofSource));
        OnPropertyChanged(nameof(HasRuntimeProof));
        OnPropertyChanged(nameof(UpstreamLineageSummary));
        OnPropertyChanged(nameof(UpstreamLineageSource));
        OnPropertyChanged(nameof(HasUpstreamLineage));
        OnPropertyChanged(nameof(HasEvidenceProofBoxes));
        OnPropertyChanged(nameof(RestoreDefaultTooltip));
        OnPropertyChanged(nameof(PublicMutationGatingReason));
        RaiseVerdictSnapshotChanged();
        UpdateCommandStates();
    }

    private void RaisePromotionGateChanged()
    {
        OnPropertyChanged(nameof(TweakOrigin));
        OnPropertyChanged(nameof(PromotionState));
        OnPropertyChanged(nameof(PromotionGatingReason));
        OnPropertyChanged(nameof(IsResearchDerived));
        OnPropertyChanged(nameof(IsPromotionActionable));
        OnPropertyChanged(nameof(CanDebugOverridePromotionGate));
        OnPropertyChanged(nameof(IsMutationAllowed));
        OnPropertyChanged(nameof(PublicMutationGatingReason));
        OnPropertyChanged(nameof(IsResearchGated));
        OnPropertyChanged(nameof(ConfigurationPrimaryActionTooltip));
        OnPropertyChanged(nameof(ConfigurationRollbackActionTooltip));
        OnPropertyChanged(nameof(PrimaryActionTooltip));
        OnPropertyChanged(nameof(RollbackActionTooltip));
        OnPropertyChanged(nameof(ResearchGateMessage));
        OnPropertyChanged(nameof(HasResearchGateMessage));
        OnPropertyChanged(nameof(RestoreDefaultTooltip));
        RaiseVerdictSnapshotChanged();
        UpdateCommandStates();
    }

    private void RaiseVerdictSnapshotChanged()
    {
        OnPropertyChanged(nameof(VerdictState));
        OnPropertyChanged(nameof(VerdictText));
        OnPropertyChanged(nameof(CompactStateText));
        OnPropertyChanged(nameof(CompactStateTone));
        OnPropertyChanged(nameof(VerdictSummary));
        OnPropertyChanged(nameof(DocsSnapshotState));
        OnPropertyChanged(nameof(DocsSnapshotText));
        OnPropertyChanged(nameof(RuntimeSnapshotState));
        OnPropertyChanged(nameof(RuntimeSnapshotText));
        OnPropertyChanged(nameof(SourceSnapshotState));
        OnPropertyChanged(nameof(SourceSnapshotText));
        OnPropertyChanged(nameof(RollbackSnapshotState));
        OnPropertyChanged(nameof(RollbackSnapshotText));
        OnPropertyChanged(nameof(RiskSnapshotText));
        OnPropertyChanged(nameof(RollbackStoryText));
    }

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
        private set
        {
            if (SetProperty(ref _terminalOutput, value))
            {
                OnPropertyChanged(nameof(HasTerminalOutput));
            }
        }
    }

    public bool HasTerminalOutput => !string.IsNullOrWhiteSpace(_terminalOutput);

    public bool ShowTerminal
    {
        get => _showTerminal;
        set => SetProperty(ref _showTerminal, value);
    }

    private TweakInsightSnapshot BuildInsightSnapshot()
    {
        return InsightFormatter.Build(new TweakInsightInput
        {
            Id = Id,
            Category = Category,
            ImpactAreaLabel = ImpactAreaLabel,
            RegistryPath = RegistryPath,
            ActionType = ActionType,
            HasBatchDetails = HasBatchDetails
        });
    }

    private void RaiseInsightPropertiesChanged()
    {
        OnPropertyChanged(nameof(DetectedFromSummary));
        OnPropertyChanged(nameof(AffectsSummary));
        OnPropertyChanged(nameof(RestartAdvice));
        OnPropertyChanged(nameof(RelatedSettingsSummary));
    }

    private void AppendToTerminal(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        TerminalOutput += TweakExecutionLogFormatter.FormatTerminalLine(DateTime.Now, message);
    }

    private void ClearTerminal() => TerminalOutput = string.Empty;

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
                OnPropertyChanged(nameof(StatusBorderBrush));
                OnPropertyChanged(nameof(StatusTextBrush));
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

    public string StatusIcon => TweakStatusPresentation.BuildIcon(
        AppliedStatus,
        ShouldShowMixedStatus,
        RequiresAdminScan);

    public Brush StatusColor => TweakStatusPresentation.GetStatusBrush(
        AppliedStatus,
        ShouldShowMixedStatus,
        RequiresAdminScan);

    public Brush StatusBorderBrush => TweakStatusPresentation.GetBorderBrush(
        AppliedStatus,
        ShouldShowMixedStatus,
        RequiresAdminScan);

    public Brush StatusTextBrush => TweakStatusPresentation.GetTextBrush(
        AppliedStatus,
        ShouldShowMixedStatus,
        RequiresAdminScan);

    public Brush StatusBadgeBackground => TweakStatusPresentation.GetBadgeBackground(
        AppliedStatus,
        ShouldShowMixedStatus,
        RequiresAdminScan);

    public string StatusText => TweakStatusPresentation.BuildText(
        AppliedStatus,
        ShouldShowMixedStatus,
        RequiresAdminScan);

    private bool ShouldShowMixedStatus =>
        AppliedStatus is not TweakAppliedStatus.Error
        && !string.IsNullOrWhiteSpace(CurrentValue)
        && CurrentValue.Contains("Mixed", StringComparison.OrdinalIgnoreCase);

    private bool RequiresAdminScan =>
        AppliedStatus == TweakAppliedStatus.Unknown
        && WillPromptForDetect;

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

    public string FavoriteIcon => _isFavorite ? "*" : "+";

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

    public bool HasOutcome => TweakOutcomePresentation.HasOutcome(LastOutcome);

    public string OutcomeText => TweakOutcomePresentation.BuildOutcomeText(LastOutcome);

    public string OutcomeSummary => TweakOutcomePresentation.BuildOutcomeSummary(LastOutcome, LastActionText);

    public bool IsDetailsExpanded
    {
        get => _isDetailsExpanded;
        set
        {
            SetProperty(ref _isDetailsExpanded, value);
        }
    }

    public Task RunPreviewAsync(CancellationToken ct) => CanInspect() ? RunAsync(true, ct) : Task.CompletedTask;

    public Task RunApplyAsync(CancellationToken ct) => CanMutate() ? RunAsync(false, ct) : Task.CompletedTask;

    public Task RunDetectAsync(CancellationToken ct) => CanInspect() ? RunSingleStepAsync(TweakAction.Detect, ct) : Task.CompletedTask;

    public Task RunVerifyAsync(CancellationToken ct) => CanInspect() ? RunSingleStepAsync(TweakAction.Verify, ct) : Task.CompletedTask;

    public Task RunRollbackAsync(CancellationToken ct) => CanMutate() ? RunSingleStepAsync(TweakAction.Rollback, ct) : Task.CompletedTask;

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
        ShowTerminal = true;
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
            AppendToTerminal("Run cancelled.");
        }
        catch (Exception ex)
        {
            LogToFile($"RunAsync: '{Name}' FAILED with exception: {ex.Message}");
            LogToFile($"Stack: {ex.StackTrace}");
            LastOutcome = TweakRunOutcome.Failed;
            StatusMessage = $"Run failed: {ex.Message}";
            AppendToTerminal($"Run failed: {ex.Message}");
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
            SyncChoiceStateFromTweak(updateAppliedStatus: false);
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
            SyncChoiceStateFromTweak(updateAppliedStatus: true);
            return;
        }

        if (report.RolledBack)
        {
            AppliedStatus = TweakAppliedStatus.NotApplied;
            WasRolledBack = true;
            SyncChoiceStateFromTweak(updateAppliedStatus: true);
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

        SyncChoiceStateFromTweak(updateAppliedStatus: true);
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
        ShowTerminal = true;
        AppendToTerminal($"{action} started.");
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
            AppendToTerminal(TweakExecutionLogFormatter.FormatStepLogLine(action, result.Result.Status, result.Result.Message, MaxDisplayMessageLength));
            UpdateAfterSingleStep(action, result.Result);
            LastOutcome = MapOutcome(result.Result.Status);
            StatusMessage = TweakExecutionLogFormatter.CoalesceMessage(action, result.Result.Status, result.Result.Message, MaxDisplayMessageLength);
            LastUpdatedText = $"Last update: {result.Result.Timestamp.ToLocalTime():HH:mm:ss}";
        }
        catch (OperationCanceledException)
        {
            LastOutcome = TweakRunOutcome.Cancelled;
            StatusMessage = $"{action} cancelled.";
            AppendToTerminal($"{action} cancelled.");
        }
        catch (Exception ex)
        {
            LastOutcome = TweakRunOutcome.Failed;
            StatusMessage = $"{action} failed: {ex.Message}";
            AppendToTerminal($"{action} failed: {ex.Message}");
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

                SyncChoiceStateFromTweak(updateAppliedStatus: true);
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

                SyncChoiceStateFromTweak(updateAppliedStatus: result.Status != TweakStatus.Failed);
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

                SyncChoiceStateFromTweak(updateAppliedStatus: result.Status != TweakStatus.Failed);
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

                SyncChoiceStateFromTweak(updateAppliedStatus: result.Status != TweakStatus.Failed);
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

        AppendToTerminal(TweakExecutionLogFormatter.FormatStepLogLine(update.Action, update.Status, update.Message, MaxDisplayMessageLength));

        StatusMessage = TweakExecutionLogFormatter.CoalesceMessage(update.Action, update.Status, update.Message, MaxDisplayMessageLength);
        LastUpdatedText = $"Last update: {update.Timestamp.ToLocalTime():HH:mm:ss}";

        if (update.Action == TweakAction.Detect)
        {
            TryUpdateCurrentValueFromMessage(update.Message);
            SetDetectionTimestamp(update.Timestamp, fromCache: false);
            SyncChoiceStateFromTweak(updateAppliedStatus: false);
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
        if (TweakClipboardHelper.TrySetText(Id, out var error))
        {
            StatusMessage = "Tweak ID copied to clipboard.";
            return;
        }

        StatusMessage = $"Copy failed: {error}";
    }

    private bool CanInspect() => !IsRunning && !IsBulkLocked;

    private bool CanMutate() => !IsRunning && !IsBulkLocked && IsMutationAllowed;

    private bool CanCancel() => IsRunning && !IsBulkLocked;

    private void UpdateCommandStates()
    {
        _detectCommand.RaiseCanExecuteChanged();
        _previewCommand.RaiseCanExecuteChanged();
        _applyCommand.RaiseCanExecuteChanged();
        _verifyCommand.RaiseCanExecuteChanged();
        _rollbackCommand.RaiseCanExecuteChanged();
        _restoreDefaultCommand.RaiseCanExecuteChanged();
        _cancelCommand.RaiseCanExecuteChanged();
        _toggleCommand.RaiseCanExecuteChanged();
        _customActionCommand.RaiseCanExecuteChanged();
    }

    private bool CanToggle() => CanMutate() && AppliedStatus != TweakAppliedStatus.Unknown;

    private bool CanRestoreDefault()
    {
        return CanMutate()
            && _tweak is IChoiceTweak choiceTweak
            && !string.IsNullOrWhiteSpace(choiceTweak.DefaultChoiceKey);
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
        if (TweakClipboardHelper.TrySetText(RegistryPath, out var error))
        {
            StatusMessage = "Registry path copied to clipboard.";
            return;
        }

        StatusMessage = $"Copy failed: {error}";
    }

    private void OpenReferenceLink(object? parameter)
    {
        if (parameter is not string url || string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        LogToFile($"OpenReferenceLink: {url}");
        var result = TweakReferenceLinkNavigator.Open(url);
        StatusMessage = result.StatusMessage;
        if (!result.Success)
        {
            LogToFile($"OpenReferenceLink failed: {result.ErrorMessage ?? result.StatusMessage} ({url})");
        }
    }

    private void TryPopulateTechnicalInfo()
    {
        var snapshot = TweakTechnicalInfoBuilder.Build(_tweak, RegistryPath, TargetValue, CodeExample);
        ApplyTechnicalInfoSnapshot(snapshot);

        switch (_tweak)
        {
            case RegistryValuePresetBatchTweak presetBatchTweak:
                InitializeChoiceOptions(presetBatchTweak);
                SyncChoiceStateFromTweak(updateAppliedStatus: false);
                break;
            case IChoiceTweak choiceTweak:
                InitializeChoiceOptions(choiceTweak);
                SyncChoiceStateFromTweak(updateAppliedStatus: false);
                break;
        }
    }

    private void ApplyTechnicalInfoSnapshot(TweakTechnicalInfoSnapshot snapshot)
    {
        if (!string.IsNullOrWhiteSpace(snapshot.RegistryPath))
        {
            RegistryPath = snapshot.RegistryPath;
        }

        if (!string.IsNullOrWhiteSpace(snapshot.TargetValue))
        {
            TargetValue = snapshot.TargetValue;
        }

        if (!string.IsNullOrWhiteSpace(snapshot.CodeExample))
        {
            CodeExample = snapshot.CodeExample;
        }
    }

    private void InitializeChoiceOptions(IChoiceTweak choiceTweak)
    {
        _isSyncingChoiceOption = true;
        try
        {
            ChoiceOptions.Clear();
            foreach (var choice in TweakChoiceStateCoordinator.BuildOptions(choiceTweak))
            {
                ChoiceOptions.Add(choice);
            }

            _selectedChoiceOption = TweakChoiceStateCoordinator.ResolveSelectedOption(ChoiceOptions, choiceTweak.SelectedChoiceKey);
        }
        finally
        {
            _isSyncingChoiceOption = false;
        }

        OnPropertyChanged(nameof(HasChoiceOptions));
        OnPropertyChanged(nameof(SelectedChoiceOption));
        OnPropertyChanged(nameof(SelectedChoiceDescription));
    }

    private void SyncChoiceStateFromTweak(bool updateAppliedStatus)
    {
        if (_tweak is not IChoiceTweak choiceTweak)
        {
            return;
        }

        if (ChoiceOptions.Count == 0)
        {
            InitializeChoiceOptions(choiceTweak);
        }

        _isSyncingChoiceOption = true;
        try
        {
            _selectedChoiceOption = TweakChoiceStateCoordinator.ResolveSelectedOption(ChoiceOptions, choiceTweak.SelectedChoiceKey);
        }
        finally
        {
            _isSyncingChoiceOption = false;
        }

        OnPropertyChanged(nameof(SelectedChoiceOption));
        OnPropertyChanged(nameof(SelectedChoiceDescription));

        ApplyChoiceStateSnapshot(TweakChoiceStateCoordinator.BuildSyncSnapshot(
            choiceTweak,
            _selectedChoiceOption,
            HasDetectedState || AppliedStatus is TweakAppliedStatus.Applied or TweakAppliedStatus.NotApplied,
            updateAppliedStatus));
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

        if (_tweak is IChoiceTweak choiceTweak && ChoiceOptions.Count > 0)
        {
            var matchingOption = TweakChoiceStateCoordinator.ResolveOptionForTargetValue(ChoiceOptions, TargetValue);
            if (matchingOption is not null)
            {
                _isSyncingChoiceOption = true;
                try
                {
                    choiceTweak.SelectedChoiceKey = matchingOption.Key;
                    _selectedChoiceOption = matchingOption;
                }
                finally
                {
                    _isSyncingChoiceOption = false;
                }

                OnPropertyChanged(nameof(SelectedChoiceOption));
                OnPropertyChanged(nameof(SelectedChoiceDescription));
            }
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

            SyncChoiceStateFromTweak(updateAppliedStatus: true);
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
            SyncChoiceStateFromTweak(updateAppliedStatus: false);
            return;
        }

        TryUpdateBatchDetailsFromMessage(message);

        if (_tweak is IChoiceTweak)
        {
            SyncChoiceStateFromTweak(updateAppliedStatus: false);
            return;
        }

        if (TweakExecutionMessageParser.TryExtractCurrentValue(message, out var value))
        {
            CurrentValue = value;
        }
    }

    private void TryUpdateBatchDetailsFromMessage(string message)
    {
        if (!TweakExecutionMessageParser.TryParseBatchDetails(message, MaxBatchDetailLines, out var snapshot))
        {
            ClearBatchDetails();
            return;
        }

        BatchDetailsTitle = snapshot.Title;
        _batchDetails.Clear();
        foreach (var line in snapshot.Lines)
        {
            _batchDetails.Add(line);
        }

        BatchSummaryLine = snapshot.Summary;
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

    private async Task RestoreDefaultAsync()
    {
        if (_tweak is not IChoiceTweak choiceTweak || string.IsNullOrWhiteSpace(choiceTweak.DefaultChoiceKey))
        {
            return;
        }

        var defaultOption = TweakChoiceStateCoordinator.ResolveDefaultOption(choiceTweak, ChoiceOptions);
        if (defaultOption is null)
        {
            return;
        }

        SelectedChoiceOption = defaultOption;
        await RunApplyAsync(CancellationToken.None);
    }

    private void LogToFile(string message)
    {
        TweakFileLogger.Log(message);
    }

    private void ApplyChoiceStateSnapshot(TweakChoiceStateSnapshot snapshot)
    {
        if (!string.IsNullOrWhiteSpace(snapshot.TargetValue))
        {
            TargetValue = snapshot.TargetValue;
        }

        if (!string.IsNullOrWhiteSpace(snapshot.CurrentValue))
        {
            CurrentValue = snapshot.CurrentValue;
        }

        if (snapshot.AppliedStatus.HasValue)
        {
            AppliedStatus = snapshot.AppliedStatus.Value;
        }

        if (!string.IsNullOrWhiteSpace(snapshot.StatusMessage))
        {
            StatusMessage = snapshot.StatusMessage;
        }
    }

}
