using RegProbe.App.Services;

namespace RegProbe.Tests;

public sealed class StartupQaRunnerTests
{
    [Fact]
    public void TryBuildTruthfulNotApplicableSummary_ReturnsTrue_ForEditionGatedFlow()
    {
        var stages = new[]
        {
            new StartupQaRunner.QaRunStageReport(
                "detect-before",
                "NotApplied",
                "Current edition is Professional.",
                "Detect - Skipped",
                "Unknown",
                "Optimized",
                false,
                true,
                false,
                false,
                [
                    new StartupQaRunner.QaRunStepReport("Detect", "Not applicable", "Current edition is Professional.", "10:24:23"),
                    new StartupQaRunner.QaRunStepReport("Apply", "Pending", string.Empty, "-"),
                    new StartupQaRunner.QaRunStepReport("Verify", "Pending", string.Empty, "-"),
                    new StartupQaRunner.QaRunStepReport("Rollback", "Pending", string.Empty, "-"),
                ]),
            new StartupQaRunner.QaRunStageReport(
                "apply",
                "NotApplied",
                "Run completed.",
                "Apply - Success",
                "Unknown",
                "Optimized",
                false,
                true,
                false,
                false,
                [
                    new StartupQaRunner.QaRunStepReport("Detect", "Not applicable", "Current edition is Professional.", "10:24:23"),
                    new StartupQaRunner.QaRunStepReport("Apply", "Skipped", "Detect returned NotApplicable.", "10:24:23"),
                    new StartupQaRunner.QaRunStepReport("Verify", "Skipped", "Detect returned NotApplicable.", "10:24:23"),
                    new StartupQaRunner.QaRunStepReport("Rollback", "Skipped", "Detect returned NotApplicable.", "10:24:23"),
                ]),
            new StartupQaRunner.QaRunStageReport(
                "rollback",
                "NotApplied",
                "Current edition is Professional.",
                "Rollback - Skipped",
                "Unknown",
                "Optimized",
                false,
                true,
                false,
                false,
                [
                    new StartupQaRunner.QaRunStepReport("Detect", "Not applicable", "Current edition is Professional.", "10:24:23"),
                    new StartupQaRunner.QaRunStepReport("Apply", "Skipped", "Detect returned NotApplicable.", "10:24:23"),
                    new StartupQaRunner.QaRunStepReport("Verify", "Skipped", "Detect returned NotApplicable.", "10:24:23"),
                    new StartupQaRunner.QaRunStepReport("Rollback", "Not applicable", "Current edition is Professional.", "10:24:23"),
                ]),
            new StartupQaRunner.QaRunStageReport(
                "detect-after",
                "NotApplied",
                "Current edition is Professional.",
                "Detect - Skipped",
                "Unknown",
                "Optimized",
                false,
                true,
                false,
                false,
                [
                    new StartupQaRunner.QaRunStepReport("Detect", "Not applicable", "Current edition is Professional.", "10:24:23"),
                    new StartupQaRunner.QaRunStepReport("Apply", "Skipped", "Detect returned NotApplicable.", "10:24:23"),
                    new StartupQaRunner.QaRunStepReport("Verify", "Skipped", "Detect returned NotApplicable.", "10:24:23"),
                    new StartupQaRunner.QaRunStepReport("Rollback", "Not applicable", "Current edition is Professional.", "10:24:23"),
                ]),
        };

        var result = StartupQaRunner.TryBuildTruthfulNotApplicableSummary(stages, rollbackRequested: true, out var summary);

        Assert.True(result);
        Assert.Equal("Current edition is Professional.", summary);
    }

    [Fact]
    public void TryBuildTruthfulNotApplicableSummary_ReturnsFalse_WhenDetectIsNotNormal()
    {
        var stages = new[]
        {
            new StartupQaRunner.QaRunStageReport(
                "detect-before",
                "Applied",
                "Value already matched.",
                "Detect - Success",
                "Enabled",
                "Enabled",
                false,
                true,
                true,
                false,
                [
                    new StartupQaRunner.QaRunStepReport("Detect", "Verified", "Already applied.", "10:24:23"),
                ]),
            new StartupQaRunner.QaRunStageReport(
                "apply",
                "Applied",
                "Run completed.",
                "Apply - Success",
                "Enabled",
                "Enabled",
                false,
                true,
                true,
                false,
                [
                    new StartupQaRunner.QaRunStepReport("Apply", "Applied", "Applied.", "10:24:23"),
                    new StartupQaRunner.QaRunStepReport("Verify", "Verified", "Verified.", "10:24:23"),
                ]),
            new StartupQaRunner.QaRunStageReport(
                "detect-after",
                "Applied",
                "Value matched.",
                "Detect - Success",
                "Enabled",
                "Enabled",
                false,
                true,
                true,
                false,
                [
                    new StartupQaRunner.QaRunStepReport("Detect", "Verified", "Verified.", "10:24:23"),
                ]),
        };

        var result = StartupQaRunner.TryBuildTruthfulNotApplicableSummary(stages, rollbackRequested: false, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryBuildAlreadyAppliedSummary_ReturnsTrue_WhenNoMutationWasNeeded()
    {
        var stages = new[]
        {
            new StartupQaRunner.QaRunStageReport(
                "detect-before",
                "Applied",
                "Current state: Superfetch is stopped",
                "Detect - Success",
                "Optimized",
                "Optimized",
                false,
                true,
                true,
                false,
                [
                    new StartupQaRunner.QaRunStepReport("Detect", "Applied", "Current state: Superfetch is stopped", "10:24:23"),
                    new StartupQaRunner.QaRunStepReport("Apply", "Pending", string.Empty, "-"),
                    new StartupQaRunner.QaRunStepReport("Verify", "Pending", string.Empty, "-"),
                    new StartupQaRunner.QaRunStepReport("Rollback", "Pending", string.Empty, "-"),
                ]),
            new StartupQaRunner.QaRunStageReport(
                "apply",
                "Applied",
                "Run completed.",
                "Apply - Success",
                "Optimized",
                "Optimized",
                false,
                true,
                true,
                false,
                [
                    new StartupQaRunner.QaRunStepReport("Detect", "Applied", "Current state: Superfetch is stopped", "10:24:23"),
                    new StartupQaRunner.QaRunStepReport("Apply", "Skipped", "Already in the desired state.", "10:24:23"),
                    new StartupQaRunner.QaRunStepReport("Verify", "Verified", "Changes verified successfully.", "10:24:23"),
                    new StartupQaRunner.QaRunStepReport("Rollback", "Skipped", "No changes were made.", "10:24:23"),
                ]),
            new StartupQaRunner.QaRunStageReport(
                "rollback",
                "Applied",
                "Rollback not supported for this tweak.",
                "Rollback - Skipped",
                "Optimized",
                "Optimized",
                false,
                true,
                true,
                false,
                [
                    new StartupQaRunner.QaRunStepReport("Detect", "Applied", "Current state: Superfetch is stopped", "10:24:23"),
                    new StartupQaRunner.QaRunStepReport("Apply", "Skipped", "Already in the desired state.", "10:24:23"),
                    new StartupQaRunner.QaRunStepReport("Verify", "Verified", "Changes verified successfully.", "10:24:23"),
                    new StartupQaRunner.QaRunStepReport("Rollback", "Not applicable", "Rollback not supported for this tweak.", "10:24:23"),
                ]),
            new StartupQaRunner.QaRunStageReport(
                "detect-after",
                "Applied",
                "Current state: Superfetch is stopped",
                "Detect - Success",
                "Optimized",
                "Optimized",
                false,
                true,
                true,
                false,
                [
                    new StartupQaRunner.QaRunStepReport("Detect", "Applied", "Current state: Superfetch is stopped", "10:24:23"),
                    new StartupQaRunner.QaRunStepReport("Apply", "Skipped", "Already in the desired state.", "10:24:23"),
                    new StartupQaRunner.QaRunStepReport("Verify", "Verified", "Changes verified successfully.", "10:24:23"),
                    new StartupQaRunner.QaRunStepReport("Rollback", "Not applicable", "Rollback not supported for this tweak.", "10:24:23"),
                ]),
        };

        var result = StartupQaRunner.TryBuildAlreadyAppliedSummary(stages, rollbackRequested: true, out var summary);

        Assert.True(result);
        Assert.Contains("already matched", summary);
    }
}
