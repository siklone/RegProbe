using RegProbe.CLI;

namespace RegProbe.Tests;

public sealed class CliTextInputValidationTests
{
    [Fact]
    public void NormalizeCliText_TrimsWhitespace()
    {
        var value = Program.NormalizeCliText("  power.request.override  ");

        Assert.Equal("power.request.override", value);
    }

    [Fact]
    public void NormalizeCliText_MapsNullToEmptyString()
    {
        var value = Program.NormalizeCliText(null);

        Assert.Equal(string.Empty, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateRequiredCliText_RejectsBlankValues(string value)
    {
        var error = Program.ValidateRequiredCliText(value, "tweak-id");

        Assert.Equal("tweak-id must not be empty.", error);
    }

    [Fact]
    public void ValidateRequiredCliText_AllowsTrimmedContent()
    {
        var error = Program.ValidateRequiredCliText(" power.request.override ", "tweak-id");

        Assert.Null(error);
    }
}
