using System;
using System.IO;
using System.Linq;
using RegProbe.App.Services.TweakProviders;
using Xunit;

public sealed class JsonTweakLoaderTests : IDisposable
{
    private readonly string _directory;

    public JsonTweakLoaderTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"RegProbeJsonLoader_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void Loader_ReportsValidationIssuesForUndocumentedEntries()
    {
        var filePath = Path.Combine(_directory, "batch.json");
        File.WriteAllText(
            filePath,
            """
            {
              "categories": {
                "test": {
                  "name": "Test",
                  "entries": [
                    {
                      "id": "test_entry",
                      "path": "HKLM\\Software\\RegProbe",
                      "value_name": "Value",
                      "type": "REG_DWORD",
                      "recommended_value": 1
                    }
                  ]
                }
              }
            }
            """);

        using var loader = new JsonTweakLoader(_directory);

        Assert.Equal(0, loader.Count);
        Assert.Contains(loader.ValidationIssues, issue => issue.Code == "documentation-required");
    }

    [Fact]
    public void Loader_LoadsVerifiedEntries()
    {
        var filePath = Path.Combine(_directory, "batch.json");
        File.WriteAllText(
            filePath,
            """
            {
              "categories": {
                "test": {
                  "name": "Test",
                  "entries": [
                    {
                      "id": "test_entry",
                      "name": "Test Entry",
                      "path": "HKLM\\Software\\RegProbe",
                      "value_name": "Value",
                      "type": "REG_DWORD",
                      "recommended_value": 1,
                      "verified": true
                    }
                  ]
                }
              }
            }
            """);

        using var loader = new JsonTweakLoader(_directory);

        Assert.Equal(1, loader.Count);
        Assert.Single(loader.GetTweakIds());
        Assert.Equal("test_entry", loader.GetTweakIds().Single());
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }
        catch
        {
        }
    }
}
