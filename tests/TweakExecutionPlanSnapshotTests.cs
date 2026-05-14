using System;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.App.Services;
using RegProbe.App.ViewModels;
using RegProbe.Core;
using RegProbe.Engine;
using RegProbe.Infrastructure;

namespace RegProbe.Tests;

public sealed class TweakExecutionPlanSnapshotTests
{
    [Fact]
    public void Create_ForRegistryBackedTweak_IncludesRegistryAndRollbackSummary()
    {
        var viewModel = CreateViewModel("system.plan-test", requiresElevation: false);
        viewModel.RegistryPath = @"HKLM\Software\RegProbe\Feature";
        viewModel.CurrentValue = "Enabled";
        viewModel.TargetValue = "Disabled";

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
            RollbackStatus = new TweakRollbackGateStatus
            {
                RollbackDeclared = true,
                RollbackExecuted = true,
                RollbackVerified = true,
                RollbackVerificationMethod = "vm-safety-bench"
            }
        });

        var snapshot = TweakExecutionPlanSnapshot.Create(viewModel);

        Assert.Equal("Apply review (7 checks) • Rollback ready", snapshot.CollapsedSummary);
        Assert.Contains(@"HKLM\Software\RegProbe\Feature", snapshot.ExportText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Rollback story: verified", snapshot.ExportText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_ForElevatedTweak_CallsOutAdministratorApproval()
    {
        var viewModel = CreateViewModel("system.elevated-plan-test", requiresElevation: true);

        var snapshot = TweakExecutionPlanSnapshot.Create(viewModel);

        Assert.Contains("administrator approval", snapshot.ExportText, StringComparison.OrdinalIgnoreCase);
    }

    private static TweakItemViewModel CreateViewModel(string id, bool requiresElevation)
    {
        var pipeline = new TweakExecutionPipeline(new RecordingLogger());
        var tweak = new PlanTestTweak(id, requiresElevation);
        return new TweakItemViewModel(tweak, pipeline, isElevated: false);
    }

    private sealed class RecordingLogger : IAppLogger
    {
        public void Log(LogLevel level, string message, Exception? exception = null)
        {
        }
    }

    private sealed class PlanTestTweak(string id, bool requiresElevation) : ITweak
    {
        public string Id { get; } = id;
        public string Name => "Plan Test";
        public string Description => "Plan test";
        public TweakRiskLevel Risk => TweakRiskLevel.Advanced;
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
