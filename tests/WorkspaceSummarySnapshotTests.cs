using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.App.ViewModels;
using RegProbe.Core;
using RegProbe.Engine;
using RegProbe.Infrastructure;

namespace RegProbe.Tests;

public sealed class WorkspaceSummarySnapshotTests
{
    [Fact]
    public void Create_MixedVisibleTweaks_ComputesTrustBarCounts()
    {
        var tweaks = new[]
        {
            CreateViewModel("system.pending-ready", TweakAppliedStatus.NotApplied, rollbackState: "ready"),
            CreateViewModel("system.unknown-partial", TweakAppliedStatus.Unknown, rollbackState: "partial", requiresElevation: true),
            CreateViewModel("system.applied-missing", TweakAppliedStatus.Applied, rollbackState: "missing")
        };

        var snapshot = WorkspaceSummarySnapshot.Create(tweaks, isElevated: false);

        Assert.Equal("1", snapshot.Pending.ValueText);
        Assert.Equal("attention", snapshot.Pending.State);
        Assert.Equal("1/3", snapshot.Rollback.ValueText);
        Assert.Equal("attention", snapshot.Rollback.State);
        Assert.Equal("1", snapshot.Elevation.ValueText);
        Assert.Equal("attention", snapshot.Elevation.State);
        Assert.Equal("2/3", snapshot.Verification.ValueText);
        Assert.Equal("attention", snapshot.Verification.State);
        Assert.Contains("still need admin", snapshot.Verification.DetailText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_AllReadyVisibleTweaks_ReturnsOkStates()
    {
        var tweaks = new[]
        {
            CreateViewModel("system.applied-ready", TweakAppliedStatus.Applied, rollbackState: "ready", requiresElevation: true),
            CreateViewModel("system.applied-ready-two", TweakAppliedStatus.Applied, rollbackState: "ready")
        };

        var snapshot = WorkspaceSummarySnapshot.Create(tweaks, isElevated: true);

        Assert.Equal("0", snapshot.Pending.ValueText);
        Assert.Equal("ok", snapshot.Pending.State);
        Assert.Equal("2/2", snapshot.Rollback.ValueText);
        Assert.Equal("ok", snapshot.Rollback.State);
        Assert.Equal("1", snapshot.Elevation.ValueText);
        Assert.Equal("ok", snapshot.Elevation.State);
        Assert.Equal("2/2", snapshot.Verification.ValueText);
        Assert.Equal("ok", snapshot.Verification.State);
    }

    private static TweakItemViewModel CreateViewModel(
        string id,
        TweakAppliedStatus appliedStatus,
        string rollbackState,
        bool requiresElevation = false)
    {
        var pipeline = new TweakExecutionPipeline(new RecordingLogger());
        var tweak = new SummaryTestTweak(id, requiresElevation);
        var viewModel = new TweakItemViewModel(tweak, pipeline, isElevated: false);

        viewModel.ApplyCachedInventoryState(new TweakInventoryState
        {
            Id = id,
            AppliedStatus = appliedStatus.ToString(),
            CurrentValue = appliedStatus == TweakAppliedStatus.Applied ? "Enabled" : "Disabled",
            TargetValue = "Enabled",
            LastDetectedAtUtc = DateTimeOffset.UtcNow
        });

        ApplyRollbackState(viewModel, rollbackState);
        return viewModel;
    }

    private static void ApplyRollbackState(TweakItemViewModel viewModel, string rollbackState)
    {
        if (string.Equals(rollbackState, "missing", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var rollbackStatus = new TweakRollbackGateStatus
        {
            RollbackDeclared = true,
            RollbackExecuted = string.Equals(rollbackState, "ready", StringComparison.OrdinalIgnoreCase),
            RollbackVerified = string.Equals(rollbackState, "ready", StringComparison.OrdinalIgnoreCase),
            RollbackVerificationMethod = string.Equals(rollbackState, "ready", StringComparison.OrdinalIgnoreCase)
                ? "vm-safety-bench"
                : string.Empty
        };

        viewModel.ApplyResearchPromotionGate(new TweakPromotionGateEntry
        {
            CandidateId = viewModel.Id,
            RecordId = viewModel.Id,
            TweakId = viewModel.Id,
            TweakOrigin = "research-derived",
            PromotionState = "promoted",
            RecordPromotionAllowed = true,
            TweakIngestAllowed = true,
            ApplyAllowed = true,
            AppMappingStatus = "matches-research",
            NextMissingLayer = "none",
            DebugOverrideAllowed = false,
            RollbackStatus = rollbackStatus
        });
    }

    private sealed class RecordingLogger : IAppLogger
    {
        public void Log(LogLevel level, string message, Exception? exception = null)
        {
        }
    }

    private sealed class SummaryTestTweak(string id, bool requiresElevation) : ITweak
    {
        public string Id { get; } = id;
        public string Name => "Summary";
        public string Description => "Summary test";
        public TweakRiskLevel Risk => TweakRiskLevel.Safe;
        public bool RequiresElevation { get; } = requiresElevation;

        public Task<TweakResult> DetectAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Detected, "Detected", DateTimeOffset.UtcNow));

        public Task<TweakResult> ApplyAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Applied, "Applied", DateTimeOffset.UtcNow));

        public Task<TweakResult> VerifyAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Verified, "Verified", DateTimeOffset.UtcNow));

        public Task<TweakResult> RollbackAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.RolledBack, "Rolled back", DateTimeOffset.UtcNow));
    }
}
