using RegProbe.CLI;

namespace RegProbe.Tests;

public sealed class CliTextInputValidationTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "RegProbe-CliTextValidation", Guid.NewGuid().ToString("N"));

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

    [Fact]
    public void NormalizeOptionalCliText_TrimsMeaningfulValues()
    {
        var value = Program.NormalizeOptionalCliText("  debug override  ");

        Assert.Equal("debug override", value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeOptionalCliText_MapsBlankValuesToNull(string? value)
    {
        var normalized = Program.NormalizeOptionalCliText(value);

        Assert.Null(normalized);
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

    [Fact]
    public void ValidateExistingFilePath_AcceptsExistingTrimmedPath()
    {
        Directory.CreateDirectory(_tempDirectory);
        var path = Path.Combine(_tempDirectory, "config.json");
        File.WriteAllText(path, "{}");

        var error = Program.ValidateExistingFilePath($"  {path}  ", "file");

        Assert.Null(error);
    }

    [Fact]
    public void ValidateExistingFilePath_RejectsMissingFile()
    {
        Directory.CreateDirectory(_tempDirectory);
        var path = Path.Combine(_tempDirectory, "missing.json");

        var error = Program.ValidateExistingFilePath(path, "file");

        Assert.Equal($"file was not found: {Path.GetFullPath(path)}", error);
    }

    [Fact]
    public void ValidateExistingFilePath_RejectsBlankPath()
    {
        var error = Program.ValidateExistingFilePath("   ", "file");

        Assert.Equal("file must not be empty.", error);
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
