using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.App.Services;
using RegProbe.App.ViewModels;
using RegProbe.Core;
using RegProbe.Engine;
using RegProbe.Infrastructure;

namespace RegProbe.Tests;

public sealed class TweakItemViewModelTests
{
    [Fact]
    public void Category_Uses_Known_Hyphenated_Prefix_When_Id_Has_No_Dots()
    {
        var pipeline = new TweakExecutionPipeline(new RecordingLogger());
        var tweak = new TestTweak("system-check-disk-health");

        var viewModel = new TweakItemViewModel(tweak, pipeline, isElevated: false);

        Assert.Equal("System", viewModel.Category);
    }

    [Fact]
    public void ChoiceTweaks_Expose_Default_Action_And_Guidance()
    {
        var pipeline = new TweakExecutionPipeline(new RecordingLogger());
        var tweak = new ChoiceTestTweak();

        var viewModel = new TweakItemViewModel(tweak, pipeline, isElevated: false);

        Assert.True(viewModel.HasChoiceOptions);
        Assert.True(viewModel.HasDefaultChoice);
        Assert.Equal("Privacy-friendly summary", viewModel.FriendlyDescription);
        Assert.Equal("Privacy", viewModel.SelectedChoiceOption?.Label);
        Assert.Contains("Restore Default", viewModel.RestoreDefaultButtonText);
    }

    [Fact]
    public async Task RunDetectAsync_SwitchesToRunDetails_AndWritesTerminalOutput()
    {
        var pipeline = new TweakExecutionPipeline(new RecordingLogger());
        var tweak = new TestTweak("misc.test");
        var viewModel = new TweakItemViewModel(tweak, pipeline, isElevated: false);

        await viewModel.RunDetectAsync(CancellationToken.None);

        Assert.True(viewModel.ShowTerminal);
        Assert.True(viewModel.HasTerminalOutput);
        Assert.Contains("Detect started.", viewModel.TerminalOutput);
    }

    [Fact]
    public async Task RunDetectAsync_Truncates_Very_Large_Batch_Details()
    {
        var pipeline = new TweakExecutionPipeline(new RecordingLogger());
        var tweak = new VerboseBatchDetectTweak();
        var viewModel = new TweakItemViewModel(tweak, pipeline, isElevated: false);

        await viewModel.RunDetectAsync(CancellationToken.None);

        Assert.True(viewModel.HasBatchDetails);
        Assert.Equal(200, viewModel.BatchDetails.Count);
        Assert.Contains("50 more hidden", viewModel.BatchSummaryLine);
        Assert.Equal("Mixed", viewModel.CurrentValue);
        Assert.DoesNotContain("Entries:", viewModel.StatusMessage);
        Assert.DoesNotContain("Value249", viewModel.StatusMessage);
    }

    [Fact]
    public void ResearchDerivedBlockedPromotionGate_DisablesMutation()
    {
        var pipeline = new TweakExecutionPipeline(new RecordingLogger());
        var tweak = new TestTweak("power.blocked-gate-test");
        var viewModel = new TweakItemViewModel(tweak, pipeline, isElevated: false);

        viewModel.ApplyEvidenceClassification(new TweakEvidenceClassEntry
        {
            RecordId = tweak.Id,
            TweakId = tweak.Id,
            EvidenceClass = "A",
            ClassLabel = "Class A",
            ClassTitle = "Ready",
            ClassDescription = "Ready for app surface",
            ActionState = "actionable",
            GatingReason = string.Empty,
            IsActionable = true,
            ShowInApp = true,
        });
        viewModel.ApplyResearchPromotionGate(new TweakPromotionGateEntry
        {
            CandidateId = tweak.Id,
            RecordId = tweak.Id,
            TweakId = tweak.Id,
            TweakOrigin = "research-derived",
            PromotionState = "blocked",
            PromotionBlockers = new List<string> { "runtime-trace" },
            RecordPromotionAllowed = false,
            TweakIngestAllowed = false,
            ApplyAllowed = false,
            AppMappingStatus = "not-mapped",
            NextMissingLayer = "runtime-trace",
            DebugOverrideAllowed = false,
        });

        Assert.True(viewModel.IsEvidenceClassActionable);
        Assert.False(viewModel.IsMutationAllowed);
        Assert.True(viewModel.IsResearchGated);
        Assert.Equal("blocked", viewModel.PromotionState);
    }

    [Fact]
    public void ApplyAllowedVerdict_UsesProofAndRollbackSnapshots()
    {
        var pipeline = new TweakExecutionPipeline(new RecordingLogger());
        var tweak = new TestTweak("system.apply-allowed-test");
        var viewModel = new TweakItemViewModel(tweak, pipeline, isElevated: false);

        viewModel.ReferenceLinks.Add(new ReferenceLink(
            "User guide",
            "https://example.com/docs",
            kind: ReferenceLinkKind.Docs));
        viewModel.HasNohutoEvidence = true;

        viewModel.ApplyEvidenceClassification(new TweakEvidenceClassEntry
        {
            RecordId = tweak.Id,
            TweakId = tweak.Id,
            EvidenceClass = "A",
            ClassLabel = "Class A",
            ClassTitle = "Ready",
            ClassDescription = "Ready for app surface",
            ActionState = "actionable",
            GatingReason = string.Empty,
            IsActionable = true,
            ShowInApp = true,
            RestoreStoryKnown = true,
            ValidatedSemantics = new TweakEvidenceProofBlock
            {
                Summary = "Docs-backed semantics",
                HasSemanticsEvidence = true,
                HasValidationProof = true,
                PrimarySourceText = "Primary docs"
            },
            RuntimeProof = new TweakEvidenceProofBlock
            {
                Summary = "VM runtime confirmed",
                HasRuntimeEvidence = true,
                HasValidationProof = true
            },
            UpstreamLineage = new TweakEvidenceProofBlock
            {
                Summary = "Source lineage present",
                HasNohutoLineage = true,
                HasValidationProof = true
            }
        });

        viewModel.ApplyResearchPromotionGate(new TweakPromotionGateEntry
        {
            CandidateId = tweak.Id,
            RecordId = tweak.Id,
            TweakId = tweak.Id,
            TweakOrigin = "research-derived",
            PromotionState = "promoted",
            RecordPromotionAllowed = true,
            TweakIngestAllowed = true,
            ApplyAllowed = true,
            AppMappingStatus = "matches-research",
            NextMissingLayer = "none",
            DebugOverrideAllowed = false,
            RollbackStatus = new TweakRollbackGateStatus
            {
                RollbackDeclared = true,
                RollbackExecuted = true,
                RollbackVerified = true,
                RollbackVerificationMethod = "vm-safety-bench"
            }
        });

        Assert.Equal("Apply allowed", viewModel.VerdictText);
        Assert.Equal("allowed", viewModel.VerdictState);
        Assert.Equal("ready", viewModel.DocsSnapshotState);
        Assert.Equal("ready", viewModel.RuntimeSnapshotState);
        Assert.Equal("ready", viewModel.SourceSnapshotState);
        Assert.Equal("ready", viewModel.RollbackSnapshotState);
        Assert.Contains("Verified", viewModel.RollbackStoryText);
    }

    [Fact]
    public void AddingDocsReference_RefreshesDocsSnapshot()
    {
        var pipeline = new TweakExecutionPipeline(new RecordingLogger());
        var tweak = new TestTweak("system.docs-snapshot-test");
        var viewModel = new TweakItemViewModel(tweak, pipeline, isElevated: false);

        Assert.Equal("missing", viewModel.DocsSnapshotState);

        viewModel.ReferenceLinks.Add(new ReferenceLink(
            "How it works",
            "https://example.com/details",
            kind: ReferenceLinkKind.Details));

        Assert.Equal("ready", viewModel.DocsSnapshotState);
        Assert.Equal("Docs ready", viewModel.DocsSnapshotText);
    }

    [Fact]
    public void BlockedVerdict_KeepsRuntimeAndRollbackInPendingState()
    {
        var pipeline = new TweakExecutionPipeline(new RecordingLogger());
        var tweak = new TestTweak("system.blocked-verdict-test");
        var viewModel = new TweakItemViewModel(tweak, pipeline, isElevated: false);

        viewModel.ApplyEvidenceClassification(new TweakEvidenceClassEntry
        {
            RecordId = tweak.Id,
            TweakId = tweak.Id,
            EvidenceClass = "B",
            ClassLabel = "Class B",
            ClassTitle = "Control surface known",
            ClassDescription = "Needs runtime proof",
            ActionState = "actionable",
            GatingReason = string.Empty,
            IsActionable = true,
            ShowInApp = true,
            ValidatedSemantics = new TweakEvidenceProofBlock
            {
                Summary = "Semantics inferred",
                HasSemanticsEvidence = true,
                NeedsVmValidation = true
            }
        });

        viewModel.ApplyResearchPromotionGate(new TweakPromotionGateEntry
        {
            CandidateId = tweak.Id,
            RecordId = tweak.Id,
            TweakId = tweak.Id,
            TweakOrigin = "research-derived",
            PromotionState = "blocked",
            PromotionBlockers = new List<string> { "runtime-trace" },
            RecordPromotionAllowed = false,
            TweakIngestAllowed = false,
            ApplyAllowed = false,
            AppMappingStatus = "not-mapped",
            NextMissingLayer = "runtime-trace",
            DebugOverrideAllowed = false
        });

        Assert.Equal("Blocked", viewModel.VerdictText);
        Assert.Equal("blocked", viewModel.VerdictState);
        Assert.Equal("partial", viewModel.RuntimeSnapshotState);
        Assert.Equal("missing", viewModel.RollbackSnapshotState);
        Assert.Contains("evidence-first", viewModel.RiskSnapshotText);
    }

    private sealed class RecordingLogger : IAppLogger
    {
        public void Log(LogLevel level, string message, Exception? exception = null)
        {
        }
    }

    private sealed class TestTweak : ITweak
    {
        public TestTweak(string id)
        {
            Id = id;
        }

        public string Id { get; }
        public string Name => "Test";
        public string Description => "Test";
        public TweakRiskLevel Risk => TweakRiskLevel.Safe;
        public bool RequiresElevation => false;

        public Task<TweakResult> DetectAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Detected, "Detected", DateTimeOffset.UtcNow));

        public Task<TweakResult> ApplyAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Applied, "Applied", DateTimeOffset.UtcNow));

        public Task<TweakResult> VerifyAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Verified, "Verified", DateTimeOffset.UtcNow));

        public Task<TweakResult> RollbackAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.RolledBack, "Rolled back", DateTimeOffset.UtcNow));
    }

    private sealed class ChoiceTestTweak : IChoiceTweak, ITweakWithGuidance
    {
        public string Id => "misc.choice-test";
        public string Name => "Choice";
        public string Description => "Choice test";
        public TweakRiskLevel Risk => TweakRiskLevel.Safe;
        public bool RequiresElevation => false;

        public IReadOnlyList<TweakChoiceDefinition> Choices { get; } =
        [
            new("default", "Default", "Default description"),
            new("privacy", "Privacy", "Privacy description")
        ];

        public string SelectedChoiceKey { get; set; } = "privacy";
        public string SelectedChoiceLabel => "Privacy";
        public string SelectedChoiceDescription => "Privacy description";
        public string? MatchedChoiceKey => "privacy";
        public string? MatchedChoiceLabel => "Privacy";
        public string? DefaultChoiceKey => "default";
        public string? DefaultChoiceLabel => "Default";

        public TweakGuidance Guidance => new()
        {
            CasualSummary = "Privacy-friendly summary",
            WhenHelpful = "Helpful",
            Tradeoffs = "Tradeoffs",
            DefaultVsPrevious = "Default vs previous",
            ProfessionalNotes = "Notes"
        };

        public Task<TweakResult> DetectAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Applied, "Applied", DateTimeOffset.UtcNow));

        public Task<TweakResult> ApplyAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Applied, "Applied", DateTimeOffset.UtcNow));

        public Task<TweakResult> VerifyAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Verified, "Verified", DateTimeOffset.UtcNow));

        public Task<TweakResult> RollbackAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.RolledBack, "Rolled back", DateTimeOffset.UtcNow));
    }

    private sealed class VerboseBatchDetectTweak : ITweak
    {
        public string Id => "power.verbose-batch-test";
        public string Name => "Verbose batch";
        public string Description => "Verbose batch";
        public TweakRiskLevel Risk => TweakRiskLevel.Safe;
        public bool RequiresElevation => false;

        public Task<TweakResult> DetectAsync(CancellationToken ct)
        {
            var lines = new List<string> { "Detected. Current state: Mixed." , "Entries:" };
            for (var index = 0; index < 250; index++)
            {
                lines.Add($"Value{index:D3}: 0 -> 1");
            }

            return Task.FromResult(new TweakResult(
                TweakStatus.Detected,
                string.Join(Environment.NewLine, lines),
                DateTimeOffset.UtcNow));
        }

        public Task<TweakResult> ApplyAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Applied, "Applied", DateTimeOffset.UtcNow));

        public Task<TweakResult> VerifyAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Verified, "Verified", DateTimeOffset.UtcNow));

        public Task<TweakResult> RollbackAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.RolledBack, "Rolled back", DateTimeOffset.UtcNow));
    }
}
