using RegProbe.App.Services;

namespace Tests;

public sealed class StartupQaRequestTests
{
    [Fact]
    public void TryParse_ReturnsNull_WhenQaFlagMissing()
    {
        var request = StartupQaRequest.TryParse(["--tweaks"]);

        Assert.Null(request);
    }

    [Fact]
    public void TryParse_ParsesExplicitOutputAndShutdownFlags()
    {
        var request = StartupQaRequest.TryParse([
            "--tweaks",
            "--qa-run-tweak",
            "explorer.show-file-extensions",
            "--qa-output",
            "C:\\Temp\\qa.json",
            "--qa-shutdown"
        ]);

        Assert.NotNull(request);
        Assert.Equal("explorer.show-file-extensions", request!.TweakId);
        Assert.Equal("C:\\Temp\\qa.json", request.OutputPath);
        Assert.True(request.RollbackAfterApply);
        Assert.False(request.AllowGatedMutation);
        Assert.True(request.ShutdownWhenDone);
    }

    [Fact]
    public void TryParse_DisablesRollback_WhenSkipRollbackFlagIsPresent()
    {
        var request = StartupQaRequest.TryParse([
            "--qa-run-tweak",
            "system.disable-clipboard-history",
            "--qa-skip-rollback"
        ]);

        Assert.NotNull(request);
        Assert.False(request!.RollbackAfterApply);
    }

    [Fact]
    public void TryParse_EnablesQaOnlyGatedMutationOverride_WhenFlagIsPresent()
    {
        var request = StartupQaRequest.TryParse([
            "--qa-run-tweak",
            "system.disable-clipboard-history",
            "--qa-allow-gated-mutation"
        ]);

        Assert.NotNull(request);
        Assert.True(request!.AllowGatedMutation);
    }

    [Fact]
    public void StartupNavigationRequest_ParsesOpenTweakAndExpandedPlan()
    {
        var request = StartupNavigationRequest.TryParse([
            "--tweaks",
            "--open-tweak",
            "power.disable-network-power-saving.policy",
            "--expand-plan"
        ]);

        Assert.NotNull(request);
        Assert.Equal("power.disable-network-power-saving.policy", request!.OpenTweakId);
        Assert.True(request.ExpandPlanDrawer);
    }

    [Fact]
    public void StartupNavigationRequest_AllowsQaAliases()
    {
        var request = StartupNavigationRequest.TryParse([
            "--qa-open-tweak",
            "SystemResponsiveness",
            "--qa-expand-plan"
        ]);

        Assert.NotNull(request);
        Assert.Equal("SystemResponsiveness", request!.OpenTweakId);
        Assert.True(request.ExpandPlanDrawer);
    }

    [Fact]
    public void StartupNavigationRequest_ReturnsNull_WhenNoNavigationFlags()
    {
        var request = StartupNavigationRequest.TryParse(["--tweaks"]);

        Assert.Null(request);
    }

    [Fact]
    public void SingleInstanceManager_UsesIsolatedInstance_ForQaRun()
    {
        Assert.True(SingleInstanceManager.UsesIsolatedQaInstance([
            "--tweaks",
            "--qa-run-tweak",
            "power.disable-network-power-saving.policy"
        ]));
    }

    [Fact]
    public void SingleInstanceManager_DoesNotUseIsolatedInstance_ForVisualNavigationOnly()
    {
        Assert.False(SingleInstanceManager.UsesIsolatedQaInstance([
            "--tweaks",
            "--open-tweak",
            "power.disable-network-power-saving.policy",
            "--expand-plan"
        ]));
    }
}
