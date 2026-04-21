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
    public void FindPresetById_MatchesCaseInsensitiveIds()
    {
        var preset = Program.FindPresetById([GamingPreset], "  GAMING  ");

        Assert.NotNull(preset);
        Assert.Equal("gaming", preset.Id);
    }

    [Fact]
    public void FindPresetById_ReturnsNullWhenPresetIsMissing()
    {
        var preset = Program.FindPresetById([GamingPreset], "privacy");

        Assert.Null(preset);
    }

    [Fact]
    public void FindPresetById_ReturnsNullForBlankInput()
    {
        var preset = Program.FindPresetById([GamingPreset], "   ");

        Assert.Null(preset);
    }
}
