using RegProbe.CLI;
using System.Reflection;
using System.Text.Json;
using RegProbe.Infrastructure.RegistryResearch;

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
        var inputPath = Path.Combine(_tempDirectory, "trace.csv");
        var outputPath = Path.Combine(_tempDirectory, "normalized.json");
        File.WriteAllText(
            inputPath,
            string.Join(
                Environment.NewLine,
                [
                    "Time of Day,Process Name,PID,Operation,Path,Result,Detail",
                    "\"4/7/2026 2:15:30 PM\",powershell.exe,4242,RegSetValue,HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer\\HideRecommendedSection,SUCCESS,\"Type: REG_DWORD, Data: 1\""
                ]));

        var main = typeof(Program).GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);
        var args = new[]
        {
            "research",
            "normalize-registry-trace",
            "--format",
            "procmon-csv",
            "--input",
            inputPath,
            "--output",
            outputPath,
            "--run-id",
            "trace-run",
            "--evidence-ref",
            "evidence/a.json",
            "evidence/b.json"
        };
        var exitCode = (int)(main!.Invoke(null, [args]) ?? -1);

        Assert.Equal(0, exitCode);
        var bundle = JsonSerializer.Deserialize<NormalizedRegistryBundle>(File.ReadAllText(outputPath));
        Assert.NotNull(bundle);
        Assert.Equal(["evidence/a.json", "evidence/b.json"], bundle.EvidenceRefs);
        Assert.Equal(["evidence/a.json", "evidence/b.json"], Assert.Single(bundle.Events).EvidenceRefs);
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
