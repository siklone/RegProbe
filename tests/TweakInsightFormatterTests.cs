using OpenTraceProject.App.Services;
using OpenTraceProject.App.ViewModels;

public sealed class TweakInsightFormatterTests
{
    private readonly TweakInsightFormatter _formatter = new();

    [Fact]
    public void Build_ForPolicyBackedExplorerSetting_UsesPolicyRegistryLanguage()
    {
        var snapshot = _formatter.Build(new TweakInsightInput
        {
            Id = "explorer.show-file-extensions",
            Category = "Explorer",
            ImpactAreaLabel = "Registry",
            RegistryPath = @"HKCU\Software\Policies\Microsoft\Windows\Explorer\HideFileExt",
            ActionType = TweakActionType.Toggle
        });

        Assert.Contains("Policy-backed registry value", snapshot.DetectedFrom);
        Assert.Contains(@"HKCU\Software\Policies", snapshot.DetectedFrom);
        Assert.Equal("Explorer restart or sign-out may be needed to refresh the shell.", snapshot.RestartAdvice);
    }

    [Fact]
    public void Build_ForWinsockReset_MarksRebootAsRequired()
    {
        var snapshot = _formatter.Build(new TweakInsightInput
        {
            Id = "network.reset-winsock",
            Category = "Network",
            ImpactAreaLabel = "Command",
            ActionType = TweakActionType.Custom
        });

        Assert.Equal("Reboot required for the full network stack reset.", snapshot.RestartAdvice);
        Assert.Contains("live Windows command", snapshot.DetectedFrom, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_ForCleanupAction_UsesImmediateRestartGuidance()
    {
        var snapshot = _formatter.Build(new TweakInsightInput
        {
            Id = "cleanup.temp-files",
            Category = "Cleanup",
            ImpactAreaLabel = "File",
            ActionType = TweakActionType.Clean
        });

        Assert.Equal("Usually immediate. Restart only if Windows is holding a file open.", snapshot.RestartAdvice);
        Assert.Contains("Temp files", snapshot.RelatedSettings);
    }
}
