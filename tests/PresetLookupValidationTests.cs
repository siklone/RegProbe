using RegProbe.Application.Models;
using RegProbe.CLI;

namespace RegProbe.Tests;

public sealed class PresetLookupValidationTests
{
    private static readonly PresetModel GamingPreset = new(
        Id: "gaming",
        Name: "Gaming Optimization",
        Description: "desc",
        IconPath: "icon",
        Category: PresetCategory.Gaming,
        TweakIds: [],
        Level: PresetDifficulty.Beginner);

    [Fact]
    public void FindPresetByIdentifier_MatchesCaseInsensitiveIds()
    {
        var preset = Program.FindPresetByIdentifier([GamingPreset], "  GAMING  ");

        Assert.NotNull(preset);
        Assert.Equal("gaming", preset.Id);
    }

    [Fact]
    public void FindPresetByIdentifier_MatchesCaseInsensitiveNames()
    {
        var preset = Program.FindPresetByIdentifier([GamingPreset], "  gaming optimization  ");

        Assert.NotNull(preset);
        Assert.Equal("gaming", preset.Id);
    }

    [Fact]
    public void FindPresetByIdentifier_ReturnsNullWhenPresetIsMissing()
    {
        var preset = Program.FindPresetByIdentifier([GamingPreset], "privacy");

        Assert.Null(preset);
    }

    [Fact]
    public void FindPresetByIdentifier_ReturnsNullForBlankInput()
    {
        var preset = Program.FindPresetByIdentifier([GamingPreset], "   ");

        Assert.Null(preset);
    }
}
