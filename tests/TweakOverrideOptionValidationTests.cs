using RegProbe.CLI;

namespace RegProbe.Tests;

public sealed class TweakOverrideOptionValidationTests
{
    [Theory]
    [InlineData(false, null)]
    [InlineData(true, null)]
    [InlineData(true, "debug override")]
    public void ValidateOverrideOptions_AllowsSupportedCombinations(bool overrideRequested, string? overrideReason)
    {
        var error = Program.ValidateOverrideOptions(overrideRequested, overrideReason);

        Assert.Null(error);
    }

    [Theory]
    [InlineData("debug override")]
    [InlineData("audit note")]
    public void ValidateOverrideOptions_RejectsReasonWithoutOverride(string overrideReason)
    {
        var error = Program.ValidateOverrideOptions(overrideRequested: false, overrideReason);

        Assert.Equal("Override reason requires --override.", error);
    }
}
