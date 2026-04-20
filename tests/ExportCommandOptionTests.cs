using RegProbe.Application.Services;
using RegProbe.CLI;

namespace RegProbe.Tests;

public sealed class ExportCommandOptionTests
{
    [Fact]
    public void BuildExportOptions_DefaultsToIncludingEverything()
    {
        var options = Program.BuildExportOptions(noTweaks: false, noDns: false, noSettings: false);

        Assert.True(options.IncludeTweakStates);
        Assert.True(options.IncludeDnsSettings);
        Assert.True(options.IncludeAppSettings);
    }

    [Fact]
    public void BuildExportOptions_CanExcludeIndividualSections()
    {
        var options = Program.BuildExportOptions(noTweaks: true, noDns: false, noSettings: true);

        Assert.False(options.IncludeTweakStates);
        Assert.True(options.IncludeDnsSettings);
        Assert.False(options.IncludeAppSettings);
    }

    [Fact]
    public void BuildExportOptions_CanExcludeEverything()
    {
        var options = Program.BuildExportOptions(noTweaks: true, noDns: true, noSettings: true);

        Assert.False(options.IncludeTweakStates);
        Assert.False(options.IncludeDnsSettings);
        Assert.False(options.IncludeAppSettings);
    }
}
