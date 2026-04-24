using RegProbe.CLI;
using System.Reflection;

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
    public void ValidateNormalizeRegistryTraceOptions_AcceptsTrimmedInputs()
    {
        var error = Program.ValidateNormalizeRegistryTraceOptions(
            format: "  ETL  ",
            input: $"  {_inputPath}  ",
            output: $"  {Path.Combine(_tempDirectory, "normalized.json")}  ",
            runId: "  trace-run  ");

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

        Assert.Equal($"input was not found: {Path.GetFullPath(missingPath)}", error);
    }

    [Fact]
    public void ValidateNormalizeRegistryTraceOptions_RejectsDirectoryInput()
    {
        var error = Program.ValidateNormalizeRegistryTraceOptions(
            format: "etl",
            input: _tempDirectory,
            output: Path.Combine(_tempDirectory, "normalized.json"),
            runId: "trace-run");

        Assert.Equal($"input must be a file path, not a directory: {Path.GetFullPath(_tempDirectory)}", error);
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

    [Fact]
    public void ValidateOutputFilePath_RejectsDirectoryTarget()
    {
        var error = Program.ValidateOutputFilePath(_tempDirectory, "output");

        Assert.Equal($"output must be a file path, not a directory: {Path.GetFullPath(_tempDirectory)}", error);
    }

    [Fact]
    public void CreateResearchNormalizeRegistryTraceCommand_AllowsMultipleEvidenceRefsPerToken()
    {
        var factory = typeof(Program).GetMethod(
            "CreateResearchNormalizeRegistryTraceCommand",
            BindingFlags.Static | BindingFlags.NonPublic);

        var command = factory!.Invoke(null, null);
        Assert.NotNull(command);
        var optionsProperty = command.GetType().GetProperty("Options");
        var options = Assert.IsAssignableFrom<System.Collections.IEnumerable>(optionsProperty!.GetValue(command));
        var evidenceRefOption = FindOption(options, "evidence-ref", "--evidence-ref");

        Assert.NotNull(evidenceRefOption);
        var allowMultipleArgumentsProperty = evidenceRefOption.GetType().GetProperty("AllowMultipleArgumentsPerToken");
        Assert.True((bool)(allowMultipleArgumentsProperty!.GetValue(evidenceRefOption) ?? false));
    }

    private static object? FindOption(System.Collections.IEnumerable options, string name, string alias)
    {
        foreach (var option in options)
        {
            if (option is null)
            {
                continue;
            }

            var nameProperty = option.GetType().GetProperty("Name");
            if (string.Equals(nameProperty?.GetValue(option)?.ToString(), name, StringComparison.Ordinal))
            {
                return option;
            }

            var aliasesProperty = option.GetType().GetProperty("Aliases");
            var aliases = aliasesProperty?.GetValue(option) as System.Collections.IEnumerable;
            if (aliases is null)
            {
                continue;
            }

            foreach (var candidate in aliases)
            {
                if (string.Equals(candidate?.ToString(), alias, StringComparison.Ordinal))
                {
                    return option;
                }
            }
        }

        return null;
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
