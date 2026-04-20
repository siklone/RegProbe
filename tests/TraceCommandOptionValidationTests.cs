using RegProbe.CLI;

namespace RegProbe.Tests;

public sealed class TraceCommandOptionValidationTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _inputPath;

    public TraceCommandOptionValidationTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "RegProbe-TraceValidation", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _inputPath = Path.Combine(_tempDirectory, "trace.etl");
        File.WriteAllText(_inputPath, "stub");
    }

    [Fact]
    public void ValidateNormalizeRegistryTraceOptions_AcceptsSupportedFormats()
    {
        var error = Program.ValidateNormalizeRegistryTraceOptions(
            format: "ETL",
            input: _inputPath,
            output: Path.Combine(_tempDirectory, "normalized.json"),
            runId: "trace-run");

        Assert.Null(error);
    }

    [Fact]
    public void ValidateNormalizeRegistryTraceOptions_RejectsUnsupportedFormat()
    {
        var error = Program.ValidateNormalizeRegistryTraceOptions(
            format: "csv",
            input: _inputPath,
            output: Path.Combine(_tempDirectory, "normalized.json"),
            runId: "trace-run");

        Assert.Equal("Unsupported normalization format: csv", error);
    }

    [Fact]
    public void ValidateNormalizeRegistryTraceOptions_RejectsMissingInput()
    {
        var missingPath = Path.Combine(_tempDirectory, "missing.etl");
        var error = Program.ValidateNormalizeRegistryTraceOptions(
            format: "etl",
            input: missingPath,
            output: Path.Combine(_tempDirectory, "normalized.json"),
            runId: "trace-run");

        Assert.Equal($"Input trace path was not found: {Path.GetFullPath(missingPath)}", error);
    }

    [Fact]
    public void ValidateNormalizeRegistryTraceOptions_RejectsInputOutputCollision()
    {
        var error = Program.ValidateNormalizeRegistryTraceOptions(
            format: "etl",
            input: _inputPath,
            output: _inputPath,
            runId: "trace-run");

        Assert.Equal("--output must differ from --input.", error);
    }

    [Fact]
    public void ValidateNormalizeRegistryTraceOptions_RejectsWhitespaceRunId()
    {
        var error = Program.ValidateNormalizeRegistryTraceOptions(
            format: "etl",
            input: _inputPath,
            output: Path.Combine(_tempDirectory, "normalized.json"),
            runId: "   ");

        Assert.Equal("--run-id must not be empty.", error);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
        }
    }
}
