using RegProbe.CLI;

namespace RegProbe.Tests;

public sealed class RegressionPackArgumentValidationTests
{
    [Fact]
    public void ValidateRegressionPackArguments_AllowsSingleCandidateMode()
    {
        var error = Program.ValidateRegressionPackArguments(
            candidateId: "power.request.override",
            allCandidates: false,
            states: Array.Empty<string>(),
            limit: null);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateRegressionPackArguments_AllowsAllCandidatesMode()
    {
        var error = Program.ValidateRegressionPackArguments(
            candidateId: "   ",
            allCandidates: true,
            states: ["Promoted", "REVALIDATION-PENDING"],
            limit: 5);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateRegressionPackArguments_RequiresCandidateOrAll()
    {
        var error = Program.ValidateRegressionPackArguments(
            candidateId: null,
            allCandidates: false,
            states: Array.Empty<string>(),
            limit: null);

        Assert.Equal("Provide <candidate-id> or use --all.", error);
    }

    [Fact]
    public void ValidateRegressionPackArguments_RejectsCandidateIdWithAll()
    {
        var error = Program.ValidateRegressionPackArguments(
            candidateId: "power.request.override",
            allCandidates: true,
            states: Array.Empty<string>(),
            limit: null);

        Assert.Equal("Provide either <candidate-id> or --all, not both.", error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidateRegressionPackArguments_RejectsNonPositiveLimit(int limit)
    {
        var error = Program.ValidateRegressionPackArguments(
            candidateId: null,
            allCandidates: true,
            states: Array.Empty<string>(),
            limit: limit);

        Assert.Equal("--limit must be a positive integer.", error);
    }

    [Fact]
    public void ValidateRegressionPackArguments_RejectsStateWithoutAll()
    {
        var error = Program.ValidateRegressionPackArguments(
            candidateId: "power.request.override",
            allCandidates: false,
            states: ["promoted"],
            limit: null);

        Assert.Equal("--state requires --all.", error);
    }

    [Fact]
    public void ValidateRegressionPackArguments_RejectsLimitWithoutAll()
    {
        var error = Program.ValidateRegressionPackArguments(
            candidateId: "power.request.override",
            allCandidates: false,
            states: Array.Empty<string>(),
            limit: 5);

        Assert.Equal("--limit requires --all.", error);
    }

    [Fact]
    public void ValidateRegressionPackArguments_RejectsUnsupportedState()
    {
        var error = Program.ValidateRegressionPackArguments(
            candidateId: null,
            allCandidates: true,
            states: [" blocked "],
            limit: null);

        Assert.Equal(
            "Unsupported --state value 'blocked'. Expected one of: promoted, promotion-eligible, revalidation-pending.",
            error);
    }
}
