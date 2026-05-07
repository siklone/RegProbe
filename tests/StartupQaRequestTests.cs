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
}
