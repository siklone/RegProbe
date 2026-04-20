using RegProbe.CLI;

namespace RegProbe.Tests;

public sealed class TweakApplyOptionValidationTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void ValidateApplyExecutionOptions_AllowsSupportedCombinations(bool apply, bool noVerify, bool noRollback)
    {
        var error = Program.ValidateApplyExecutionOptions(apply, noVerify, noRollback);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateApplyExecutionOptions_RejectsNoVerifyWithoutApply()
    {
        var error = Program.ValidateApplyExecutionOptions(apply: false, noVerify: true, noRollback: false);

        Assert.Equal("--no-verify requires --apply.", error);
    }

    [Fact]
    public void ValidateApplyExecutionOptions_RejectsNoRollbackWithoutApply()
    {
        var error = Program.ValidateApplyExecutionOptions(apply: false, noVerify: false, noRollback: true);

        Assert.Equal("--no-rollback requires --apply.", error);
    }
}
