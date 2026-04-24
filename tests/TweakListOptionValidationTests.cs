using RegProbe.CLI;

namespace RegProbe.Tests;

public sealed class TweakListOptionValidationTests
{
    [Fact]
    public void ValidateKnownCategory_AllowsBlankCategory()
    {
        var error = Program.ValidateKnownCategory("   ", ["Privacy", "Performance"]);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateKnownCategory_AllowsKnownCategoryCaseInsensitively()
    {
        var error = Program.ValidateKnownCategory("  privacy  ", ["Performance", "Privacy"]);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateKnownCategory_RejectsUnknownCategory()
    {
        var error = Program.ValidateKnownCategory("gaming", ["Performance", "Privacy", "System"]);

        Assert.Equal(
            "Unknown category filter: gaming. Expected one of: Performance, Privacy, System.",
            error);
    }
}
