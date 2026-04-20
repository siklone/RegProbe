using RegProbe.CLI;

namespace RegProbe.Tests;

public sealed class DnsCommandOptionValidationTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ValidateDnsSetOptions_AllowsSupportedCombinations(bool apply, bool flush)
    {
        var error = Program.ValidateDnsSetOptions(apply, flush);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateDnsSetOptions_RejectsFlushWithoutApply()
    {
        var error = Program.ValidateDnsSetOptions(apply: false, flush: true);

        Assert.Equal("--flush requires --apply.", error);
    }
}
