using WindowsOptimizer.App.Services;
using WindowsOptimizer.Core;

public sealed class PolicyReferenceCatalogBuilderTests
{
    [Fact]
    public void Build_GroupsWindowsPoliciesByComponent()
    {
        var builder = new PolicyReferenceCatalogBuilder();
        var catalog = builder.Build(
        [
            new PolicyReferenceSourceItem
            {
                Name = "Show File Extensions",
                Category = "Explorer",
                EffectSummary = "Shows file name extensions in File Explorer so common file types are easier to identify.",
                RegistryPath = @"HKCU\Software\Policies\Microsoft\Windows\Explorer\HideFileExt",
                Risk = TweakRiskLevel.Safe,
                HasDetectedState = true,
                IsApplied = true
            },
            new PolicyReferenceSourceItem
            {
                Name = "Disable Aero Shake",
                Category = "System",
                EffectSummary = "Stops the Aero Shake gesture from minimizing other open windows.",
                RegistryPath = @"HKCU\Software\Policies\Microsoft\Windows\Explorer\NoWindowMinimizingShortcuts",
                Risk = TweakRiskLevel.Safe,
                HasDetectedState = true,
                IsApplied = false
            }
        ]);

        var explorer = Assert.Single(catalog.Entries);
        Assert.Equal("Windows Explorer", explorer.ComponentName);
        Assert.Equal("2 settings", explorer.SettingCountLabel);
        Assert.Equal(@"Microsoft\Windows\Explorer", explorer.SearchFragment);
        Assert.Equal(@"HKCU\Software\Policies\Microsoft\Windows\Explorer", explorer.ReadPathLabel);
        Assert.Equal("User policy stored under HKCU and applied to the current account.", explorer.ScopeDetail);
        Assert.Contains("Show File Extensions", explorer.RelatedSettingsLabel);
        Assert.Contains("Shows file name extensions", explorer.ExpectedBehavior);
    }

    [Fact]
    public void Build_DistinguishesMachineAndUserScopes()
    {
        var builder = new PolicyReferenceCatalogBuilder();
        var catalog = builder.Build(
        [
            new PolicyReferenceSourceItem
            {
                Name = "Disable Clipboard History",
                Category = "System",
                RegistryPath = @"HKLM\Software\Policies\Microsoft\Windows\System\AllowClipboardHistory",
                Risk = TweakRiskLevel.Advanced,
                HasDetectedState = false,
                IsApplied = false
            },
            new PolicyReferenceSourceItem
            {
                Name = "Disable Search Web Results",
                Category = "Misc",
                RegistryPath = @"HKCU\Software\Policies\Microsoft\Windows\Explorer\DisableSearchBoxSuggestions",
                Risk = TweakRiskLevel.Advanced,
                HasDetectedState = false,
                IsApplied = false
            }
        ]);

        Assert.Equal(2, catalog.PolicyBackedSettingCount);
        Assert.Equal(1, catalog.MachineScopedSettingCount);
        Assert.Equal(1, catalog.UserScopedSettingCount);
        Assert.Contains(catalog.Entries, entry => entry.ScopeLabel == "Machine");
        Assert.Contains(catalog.Entries, entry => entry.ScopeLabel == "User");
    }

    [Fact]
    public void Build_IgnoresNonPolicyRegistryPaths()
    {
        var builder = new PolicyReferenceCatalogBuilder();
        var catalog = builder.Build(
        [
            new PolicyReferenceSourceItem
            {
                Name = "Disable Menu Delay",
                Category = "Performance",
                RegistryPath = @"HKCU\Control Panel\Desktop\MenuShowDelay",
                Risk = TweakRiskLevel.Safe,
                HasDetectedState = true,
                IsApplied = true
            }
        ]);

        Assert.Empty(catalog.Entries);
        Assert.Equal(0, catalog.PolicyBackedSettingCount);
    }
}
