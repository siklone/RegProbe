using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.App.Diagnostics;
using RegProbe.App.ViewModels;

namespace RegProbe.App.Services;

internal static class StartupQaRunner
{
    public static async Task RunAsync(MainViewModel mainViewModel, StartupQaRequest request, Action<int>? shutdown = null)
    {
        ArgumentNullException.ThrowIfNull(mainViewModel);
        ArgumentNullException.ThrowIfNull(request);

        var startedAt = DateTimeOffset.UtcNow;
        QaRunReport report;

        try
        {
            mainViewModel.ShowConfigurationCommand.Execute(null);

            var workspace = mainViewModel.WorkspaceViewModel;
            var tweak = workspace.Tweaks.FirstOrDefault(item =>
                string.Equals(item.Id, request.TweakId, StringComparison.OrdinalIgnoreCase));

            if (tweak is null)
            {
                report = QaRunReport.CreateError(request.TweakId, $"Tweak '{request.TweakId}' was not found in the app catalog.", startedAt);
            }
            else
            {
                report = await ExecuteFlowAsync(tweak, request, startedAt);
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("Startup QA runner failed", ex);
            report = QaRunReport.CreateError(request.TweakId, ex.Message, startedAt);
        }

        await WriteReportAsync(report, request.OutputPath);
        AppDiagnostics.Log($"[QA] Wrote tweak QA report to {request.OutputPath}");

        if (request.ShutdownWhenDone)
        {
            shutdown?.Invoke(report.Success ? 0 : 1);
        }
    }

    private static async Task<QaRunReport> ExecuteFlowAsync(
        TweakItemViewModel tweak,
        StartupQaRequest request,
        DateTimeOffset startedAt)
    {
        var stages = new List<QaRunStageReport>();
        var card = QaRunCardSnapshot.Create(tweak);

        await tweak.RunDetectAsync(CancellationToken.None);
        stages.Add(QaRunStageReport.Create("detect-before", tweak));

        var gatedMutationOverrideUsed = request.AllowGatedMutation && !tweak.IsMutationAllowed;
        if (!tweak.IsMutationAllowed && !request.AllowGatedMutation)
        {
            return new QaRunReport(
                tweak.Id,
                tweak.Name,
                false,
                "mutation-blocked",
                "The app loaded the tweak, but the current evidence/promotion gate still blocks mutation.",
                request.RollbackAfterApply,
                request.AllowGatedMutation,
                gatedMutationOverrideUsed,
                card,
                stages,
                startedAt,
                DateTimeOffset.UtcNow);
        }

        await tweak.RunApplyForQaAsync(CancellationToken.None, request.AllowGatedMutation);
        stages.Add(QaRunStageReport.Create("apply", tweak));

        if (request.RollbackAfterApply)
        {
            await tweak.RunRollbackForQaAsync(CancellationToken.None, request.AllowGatedMutation);
            stages.Add(QaRunStageReport.Create("rollback", tweak));
        }

        await tweak.RunDetectAsync(CancellationToken.None);
        stages.Add(QaRunStageReport.Create("detect-after", tweak));

        var applyStage = stages.FirstOrDefault(stage => stage.Stage == "apply");
        var rollbackStage = stages.FirstOrDefault(stage => stage.Stage == "rollback");
        if (TryBuildTruthfulNotApplicableSummary(stages, request.RollbackAfterApply, out var notApplicableSummary))
        {
            return new QaRunReport(
                tweak.Id,
                tweak.Name,
                true,
                "not-applicable",
                notApplicableSummary,
                request.RollbackAfterApply,
                request.AllowGatedMutation,
                gatedMutationOverrideUsed,
                card,
                stages,
                startedAt,
                DateTimeOffset.UtcNow);
        }

        if (TryBuildAlreadyAppliedSummary(stages, request.RollbackAfterApply, out var alreadyAppliedSummary))
        {
            return new QaRunReport(
                tweak.Id,
                tweak.Name,
                true,
                "already-applied",
                alreadyAppliedSummary,
                request.RollbackAfterApply,
                request.AllowGatedMutation,
                gatedMutationOverrideUsed,
                card,
                stages,
                startedAt,
                DateTimeOffset.UtcNow);
        }

        var success = applyStage?.HasSuccessfulApplyStory == true
                      && (!request.RollbackAfterApply || rollbackStage?.HasSuccessfulRollbackStory == true);

        var summary = success
            ? gatedMutationOverrideUsed
                ? "QA-only gated mutation override used; apply/verify path completed and rollback restored the tweak."
                : "Apply/verify path completed and rollback restored the tweak."
            : "The tweak flow completed, but at least one apply or rollback checkpoint did not come back clean.";

        return new QaRunReport(
            tweak.Id,
            tweak.Name,
            success,
            success && gatedMutationOverrideUsed ? "ok-gated-override" : success ? "ok" : "check-failed",
            summary,
            request.RollbackAfterApply,
            request.AllowGatedMutation,
            gatedMutationOverrideUsed,
            card,
            stages,
            startedAt,
            DateTimeOffset.UtcNow);
    }

    internal static bool TryBuildTruthfulNotApplicableSummary(
        IReadOnlyList<QaRunStageReport> stages,
        bool rollbackRequested,
        out string summary)
    {
        summary = string.Empty;
        var detectBefore = stages.FirstOrDefault(stage => stage.Stage == "detect-before");
        var applyStage = stages.FirstOrDefault(stage => stage.Stage == "apply");
        var detectAfter = stages.FirstOrDefault(stage => stage.Stage == "detect-after");
        var rollbackStage = stages.FirstOrDefault(stage => stage.Stage == "rollback");

        if (detectBefore is null || applyStage is null || detectAfter is null)
        {
            return false;
        }

        if (!HasStepStatus(detectBefore, "Detect", "Not applicable")
            || !HasStepStatus(applyStage, "Apply", "Skipped")
            || !HasStepStatus(applyStage, "Verify", "Skipped")
            || !HasStepStatus(detectAfter, "Detect", "Not applicable"))
        {
            return false;
        }

        if (rollbackRequested)
        {
            if (rollbackStage is null || !HasStepStatus(rollbackStage, "Rollback", "Not applicable", "Skipped"))
            {
                return false;
            }
        }

        summary = string.IsNullOrWhiteSpace(detectBefore.StatusMessage)
            ? "The app loaded the tweak and correctly reported it as not applicable on this system."
            : detectBefore.StatusMessage;
        return true;
    }

    internal static bool TryBuildAlreadyAppliedSummary(
        IReadOnlyList<QaRunStageReport> stages,
        bool rollbackRequested,
        out string summary)
    {
        summary = string.Empty;
        var detectBefore = stages.FirstOrDefault(stage => stage.Stage == "detect-before");
        var applyStage = stages.FirstOrDefault(stage => stage.Stage == "apply");
        var detectAfter = stages.FirstOrDefault(stage => stage.Stage == "detect-after");
        var rollbackStage = stages.FirstOrDefault(stage => stage.Stage == "rollback");

        if (detectBefore is null || applyStage is null || detectAfter is null)
        {
            return false;
        }

        if (!HasStepStatus(detectBefore, "Detect", "Applied", "Verified")
            || !HasStepStatus(applyStage, "Apply", "Skipped")
            || !HasStepStatus(applyStage, "Verify", "Verified")
            || !HasStepStatus(applyStage, "Rollback", "Skipped")
            || !HasStepStatus(detectAfter, "Detect", "Applied", "Verified"))
        {
            return false;
        }

        if (rollbackRequested
            && rollbackStage is not null
            && !HasStepStatus(rollbackStage, "Rollback", "Not applicable", "Skipped", "Rolled back"))
        {
            return false;
        }

        summary = "The tweak already matched the desired state; the app verified it and skipped rollback because no mutation was performed.";
        return true;
    }

    private static bool HasStepStatus(QaRunStageReport stage, string action, params string[] statuses)
        => stage.Steps.Any(step =>
            string.Equals(step.Action, action, StringComparison.OrdinalIgnoreCase)
            && statuses.Any(status => string.Equals(step.StatusText, status, StringComparison.OrdinalIgnoreCase)));

    private static async Task WriteReportAsync(QaRunReport report, string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(
            outputPath,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }),
            CancellationToken.None);
    }

    private sealed record QaRunReport(
        string TweakId,
        string TweakName,
        bool Success,
        string Status,
        string Summary,
        bool RollbackRequested,
        bool GatedMutationOverrideRequested,
        bool GatedMutationOverrideUsed,
        QaRunCardSnapshot Card,
        IReadOnlyList<QaRunStageReport> Stages,
        DateTimeOffset StartedAtUtc,
        DateTimeOffset CompletedAtUtc)
    {
        public static QaRunReport CreateError(string tweakId, string message, DateTimeOffset startedAt)
            => new(
                tweakId,
                tweakId,
                false,
                "error",
                message,
                RollbackRequested: false,
                GatedMutationOverrideRequested: false,
                GatedMutationOverrideUsed: false,
                Card: QaRunCardSnapshot.CreateMissing(tweakId),
                Stages: Array.Empty<QaRunStageReport>(),
                startedAt,
                DateTimeOffset.UtcNow);
    }

    internal sealed record QaRunCardSnapshot(
        string TweakId,
        string Name,
        string Category,
        string Description,
        string FriendlyDescription,
        string RegistryPath,
        string EvidenceClass,
        string EvidenceClassTitle,
        string ResearchStatus,
        bool IsMutationAllowed,
        string RollbackSnapshotState,
        string RollbackStoryText,
        bool HasClaimBoundary,
        string WhatWeKnowSummary,
        string WhatWeDoNotClaimSummary,
        IReadOnlyList<QaRunProofLaneReport> ProofLanes,
        IReadOnlyList<QaRunReferenceLinkReport> ReferenceLinks)
    {
        public static QaRunCardSnapshot Create(TweakItemViewModel tweak)
            => new(
                tweak.Id,
                tweak.Name,
                tweak.Category,
                tweak.Description,
                tweak.FriendlyDescription,
                tweak.RegistryPath,
                tweak.EvidenceClassBadgeText,
                tweak.EvidenceClassTitle,
                tweak.ResearchStatusBadgeText,
                tweak.IsMutationAllowed,
                tweak.RollbackSnapshotState,
                tweak.RollbackStoryText,
                tweak.HasClaimBoundary,
                tweak.WhatWeKnowSummary,
                tweak.WhatWeDoNotClaimSummary,
                tweak.ProofLanes
                    .Select(lane => new QaRunProofLaneReport(
                        lane.Key,
                        lane.Label,
                        lane.State,
                        lane.Summary,
                        lane.PrimarySourceText))
                    .ToArray(),
                tweak.UserReferenceLinks
                    .Take(10)
                    .Select(link => new QaRunReferenceLinkReport(
                        link.Title,
                        link.Url,
                        link.Kind.ToString()))
                    .ToArray());

        public static QaRunCardSnapshot CreateMissing(string tweakId)
            => new(
                tweakId,
                tweakId,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                "missing",
                false,
                string.Empty,
                string.Empty,
                false,
                string.Empty,
                string.Empty,
                Array.Empty<QaRunProofLaneReport>(),
                Array.Empty<QaRunReferenceLinkReport>());
    }

    internal sealed record QaRunProofLaneReport(
        string Key,
        string Label,
        string State,
        string Summary,
        string Source);

    internal sealed record QaRunReferenceLinkReport(
        string Title,
        string Url,
        string Kind);

    internal sealed record QaRunStageReport(
        string Stage,
        string AppliedStatus,
        string StatusMessage,
        string OutcomeSummary,
        string CurrentValue,
        string TargetValue,
        bool WasRolledBack,
        bool IsMutationAllowed,
        bool HasSuccessfulApplyStory,
        bool HasSuccessfulRollbackStory,
        IReadOnlyList<QaRunStepReport> Steps)
    {
        public static QaRunStageReport Create(string stage, TweakItemViewModel tweak)
        {
            var steps = tweak.Steps
                .Select(step => new QaRunStepReport(step.ActionLabel, step.StatusText, step.Message, step.TimestampText))
                .ToArray();

            var applyStep = steps.FirstOrDefault(step => string.Equals(step.Action, "Apply", StringComparison.OrdinalIgnoreCase));
            var verifyStep = steps.FirstOrDefault(step => string.Equals(step.Action, "Verify", StringComparison.OrdinalIgnoreCase));
            var rollbackStep = steps.FirstOrDefault(step => string.Equals(step.Action, "Rollback", StringComparison.OrdinalIgnoreCase));

            return new QaRunStageReport(
                stage,
                tweak.AppliedStatus.ToString(),
                tweak.StatusMessage,
                tweak.OutcomeSummary,
                tweak.CurrentValue,
                tweak.TargetValue,
                tweak.WasRolledBack,
                tweak.IsMutationAllowed,
                HasSuccessfulApplyStory: applyStep?.StatusText is "Applied" or "Verified"
                                        || verifyStep?.StatusText == "Verified",
                HasSuccessfulRollbackStory: rollbackStep?.StatusText == "Rolled back",
                steps);
        }
    }

    internal sealed record QaRunStepReport(string Action, string StatusText, string Message, string TimestampText);
}
