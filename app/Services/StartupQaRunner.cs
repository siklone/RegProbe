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

        await tweak.RunDetectAsync(CancellationToken.None);
        stages.Add(QaRunStageReport.Create("detect-before", tweak));

        if (!tweak.IsMutationAllowed)
        {
            return new QaRunReport(
                tweak.Id,
                tweak.Name,
                false,
                "mutation-blocked",
                "The app loaded the tweak, but the current evidence/promotion gate still blocks mutation.",
                request.RollbackAfterApply,
                stages,
                startedAt,
                DateTimeOffset.UtcNow);
        }

        await tweak.RunApplyAsync(CancellationToken.None);
        stages.Add(QaRunStageReport.Create("apply", tweak));

        if (request.RollbackAfterApply)
        {
            await tweak.RunRollbackAsync(CancellationToken.None);
            stages.Add(QaRunStageReport.Create("rollback", tweak));
        }

        await tweak.RunDetectAsync(CancellationToken.None);
        stages.Add(QaRunStageReport.Create("detect-after", tweak));

        var applyStage = stages.FirstOrDefault(stage => stage.Stage == "apply");
        var rollbackStage = stages.FirstOrDefault(stage => stage.Stage == "rollback");
        var success = applyStage?.HasSuccessfulApplyStory == true
                      && (!request.RollbackAfterApply || rollbackStage?.HasSuccessfulRollbackStory == true);

        var summary = success
            ? "Apply/verify path completed and rollback restored the tweak."
            : "The tweak flow completed, but at least one apply or rollback checkpoint did not come back clean.";

        return new QaRunReport(
            tweak.Id,
            tweak.Name,
            success,
            success ? "ok" : "check-failed",
            summary,
            request.RollbackAfterApply,
            stages,
            startedAt,
            DateTimeOffset.UtcNow);
    }

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
                Stages: Array.Empty<QaRunStageReport>(),
                startedAt,
                DateTimeOffset.UtcNow);
    }

    private sealed record QaRunStageReport(
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

    private sealed record QaRunStepReport(string Action, string StatusText, string Message, string TimestampText);
}
