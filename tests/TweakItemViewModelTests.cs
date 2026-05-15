using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.App.Services;
using RegProbe.App.ViewModels;
using RegProbe.Core;
using RegProbe.Core.Registry;
using RegProbe.Engine;
using RegProbe.Engine.Tweaks;
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
        Assert.False(viewModel.IsEndUserAppCardAllowed);
        Assert.True(viewModel.IsResearchGated);
        Assert.Equal("blocked", viewModel.PromotionState);
    }

    [Fact]
    public void WorkspaceFilter_HidesResearchHoldCardsFromNormalAppSurface()
    {
        var pipeline = new TweakExecutionPipeline(new RecordingLogger());
        var tweak = new TestTweak("system.hold-gate-test");
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
            PromotionState = "intentional-hold",
            PromotionBlockers = new List<string> { "manual-review" },
            RecordPromotionAllowed = false,
            TweakIngestAllowed = false,
            ApplyAllowed = false,
            AppMappingStatus = "not-mapped",
            NextMissingLayer = "intentional-hold",
            DebugOverrideAllowed = true,
        });
        var evaluator = new WorkspaceFilterEvaluator(new TweaksShellStateViewModel());

        Assert.False(viewModel.IsEndUserAppCardAllowed);
        Assert.False(evaluator.FilterTweak(viewModel));
        Assert.False(evaluator.CurrentWorkspaceContainsCategory(new[] { viewModel }, viewModel.Category));
    }

    [Fact]
    public void SourceSnapshot_TreatsCatalogOnlySummary_AsPartialNotReady()
    {
        var pipeline = new TweakExecutionPipeline(new RecordingLogger());
        var tweak = new TestTweak("system.catalog-only-source");
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
            UpstreamLineage = new TweakEvidenceProofBlock
            {
                Summary = PublicEvidenceLinkPolicy.NoLocalSourceMessage,
                HasNohutoLineage = false
            }
        });

        Assert.False(viewModel.HasUpstreamLineage);
        Assert.Equal("partial", viewModel.SourceSnapshotState);
        var sourceLane = Assert.Single(viewModel.ProofLanes, lane => lane.Key == "source");
        Assert.False(sourceLane.HasLinks);
        Assert.Contains("Catalog-only source context", sourceLane.Summary, StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public void ClaimBoundary_SeparatesKnownClaimFromRuntimeNonClaim()
    {
        var pipeline = new TweakExecutionPipeline(new RecordingLogger());
        var tweak = new TestTweak(
            "power.disable-network-power-saving.policy",
            "Network Power and Multimedia Responsiveness",
            "These settings affect TCP/IP task offloads and MMCSS CPU reservation.");
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
            RestoreStoryKnown = true,
            ValidatedSemantics = new TweakEvidenceProofBlock
            {
                Summary = "DisableTaskOffload and SystemResponsiveness are documented; NetworkThrottlingIndex is excluded.",
                HasSemanticsEvidence = true,
                HasValidationProof = true
            },
            RuntimeProof = new TweakEvidenceProofBlock
            {
                Summary = string.Empty,
                HasRuntimeEvidence = false
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
                RollbackVerified = true,
                RollbackVerificationMethod = "record-restore-story"
            }
        });

        Assert.True(viewModel.HasClaimBoundary);
        Assert.Contains("DisableTaskOffload", viewModel.WhatWeKnowSummary);
        Assert.Contains("NetworkThrottlingIndex is excluded", viewModel.WhatWeKnowSummary);
        Assert.Contains("No benchmark, ETW/WPR trace", viewModel.WhatWeDoNotClaimSummary);
    }

    [Fact]
    public void ClaimBoundary_MarksArchivedRecordsAsAuditTrailOnly()
    {
        var pipeline = new TweakExecutionPipeline(new RecordingLogger());
        var tweak = new TestTweak("power.disable-network-power-saving");
        var viewModel = new TweakItemViewModel(tweak, pipeline, isElevated: false);

        viewModel.ApplyEvidenceClassification(new TweakEvidenceClassEntry
        {
            RecordId = tweak.Id,
            TweakId = tweak.Id,
            EvidenceClass = "E",
            ClassLabel = "Class E",
            ClassTitle = "Archived / Audit Trail",
            ClassDescription = "Deprecated audit trail",
            ActionState = "archived",
            GatingReason = "Archived audit trail only.",
            IsActionable = false,
            ShowInApp = false,
            IsArchived = true
        });

        Assert.True(viewModel.HasClaimBoundary);
        Assert.Contains("Archived", viewModel.WhatWeDoNotClaimSummary);
        Assert.Contains("not a normal app-ready tweak", viewModel.WhatWeDoNotClaimSummary);
    }

    [Fact]
    public void CompactStateAndScope_ExposeCompactTweaksMetadata()
    {
        var pipeline = new TweakExecutionPipeline(new RecordingLogger());
        var tweak = new TestTweak("system.scope-test");
        var viewModel = new TweakItemViewModel(tweak, pipeline, isElevated: false)
        {
            RegistryPath = @"HKCU\Software\RegProbe\Test"
        };

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
            ShowInApp = true
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
            DebugOverrideAllowed = false
        });

        Assert.Equal("Verified", viewModel.CompactStateText);
        Assert.Equal("ok", viewModel.CompactStateTone);
        Assert.Equal("user", viewModel.ScopeFilterKey);
        Assert.Equal("User", viewModel.ScopeDisplayText);
    }

    [Fact]
    public void ApplyCachedInventoryState_DoesNotOverrideStaticTargetValue_ForNonChoiceTweaks()
    {
        var pipeline = new TweakExecutionPipeline(new RecordingLogger());
        var tweak = new RegistryValueTweak(
            "developer.ssh-agent-autostart",
            "SSH Agent Auto-start",
            "Test",
            TweakRiskLevel.Safe,
            Microsoft.Win32.RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            "SSH Agent",
            Microsoft.Win32.RegistryValueKind.String,
            @"C:\Windows\System32\OpenSSH\ssh-agent.exe",
            new InMemoryRegistryAccessor(),
            requiresElevation: false);
        var viewModel = new TweakItemViewModel(tweak, pipeline, isElevated: false);

        Assert.Equal(@"C:\Windows\System32\OpenSSH\ssh-agent.exe", viewModel.TargetValue);

        viewModel.ApplyCachedInventoryState(new TweakInventoryState
        {
            Id = tweak.Id,
            AppliedStatus = TweakAppliedStatus.NotApplied.ToString(),
            CurrentValue = "Not set",
            TargetValue = "evidence/files/external/c/System32/OpenSSH/ssh-agent.exe.md",
            LastDetectedAtUtc = DateTimeOffset.UtcNow
        });

        Assert.Equal(@"C:\Windows\System32\OpenSSH\ssh-agent.exe", viewModel.TargetValue);
    }

    private sealed class RecordingLogger : IAppLogger
    {
        public void Log(LogLevel level, string message, Exception? exception = null)
        {
        }
    }

    private sealed class TestTweak : ITweak
    {
        public TestTweak(string id, string name = "Test", string description = "Test")
        {
            Id = id;
            Name = name;
            Description = description;
        }

        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
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

    private sealed class InMemoryRegistryAccessor : IRegistryAccessor
    {
        private readonly Dictionary<RegistryValueReference, RegistryValueData> _values = new();

        public Task<RegistryValueReadResult> ReadValueAsync(RegistryValueReference reference, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(
                _values.TryGetValue(reference, out var value)
                    ? new RegistryValueReadResult(true, value)
                    : new RegistryValueReadResult(false, null));
        }

        public Task SetValueAsync(RegistryValueReference reference, RegistryValueData value, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            _values[reference] = value;
            return Task.CompletedTask;
        }

        public Task DeleteValueAsync(RegistryValueReference reference, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            _values.Remove(reference);
            return Task.CompletedTask;
        }
    }
}
