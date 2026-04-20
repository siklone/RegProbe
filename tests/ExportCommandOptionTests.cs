using RegProbe.Application.Services;
using RegProbe.CLI;

namespace RegProbe.Tests;

public sealed class ExportCommandOptionTests
{
    [Fact]
    public void BuildExportOptions_DefaultsToIncludingEverything()
    {
        var options = Program.BuildExportOptions(
            includeTweaks: true,
            noTweaks: false,
            includeDns: true,
            noDns: false,
            includeSettings: true,
            noSettings: false);

        Assert.True(options.IncludeTweakStates);
        Assert.True(options.IncludeDnsSettings);
        Assert.True(options.IncludeAppSettings);
    }

    [Fact]
    public void BuildExportOptions_CanExcludeIndividualSections()
    {
        var options = Program.BuildExportOptions(
            includeTweaks: true,
            noTweaks: true,
            includeDns: true,
            noDns: false,
            includeSettings: true,
            noSettings: true);

        Assert.False(options.IncludeTweakStates);
        Assert.True(options.IncludeDnsSettings);
        Assert.False(options.IncludeAppSettings);
    }

    [Fact]
    public void BuildExportOptions_CanExcludeEverything()
    {
        var options = Program.BuildExportOptions(
            includeTweaks: true,
            noTweaks: true,
            includeDns: true,
            noDns: true,
            includeSettings: true,
            noSettings: true);

        Assert.False(options.IncludeTweakStates);
        Assert.False(options.IncludeDnsSettings);
        Assert.False(options.IncludeAppSettings);
    }

    [Fact]
    public void BuildExportOptions_RespectsLegacyIncludeFalseValues()
    {
        var options = Program.BuildExportOptions(
            includeTweaks: false,
            noTweaks: false,
            includeDns: false,
            noDns: false,
            includeSettings: true,
            noSettings: false);

        Assert.False(options.IncludeTweakStates);
        Assert.False(options.IncludeDnsSettings);
        Assert.True(options.IncludeAppSettings);
    }

    [Fact]
    public void ValidateExportOptions_RejectsConflictingExplicitIncludeAndExcludeFlags()
    {
        var error = Program.ValidateExportOptions(
            includeTweaks: true,
            includeTweaksSpecified: true,
            noTweaks: true,
            includeDns: true,
            includeDnsSpecified: false,
            noDns: false,
            includeSettings: true,
            includeSettingsSpecified: false,
            noSettings: false);

        Assert.Equal("Do not combine --include-tweaks with --no-tweaks.", error);
    }
}
