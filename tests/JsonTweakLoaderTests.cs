using System;
using System.IO;
using System.Linq;
using System.Threading;
using Moq;
using RegProbe.Core.Registry;
using RegProbe.Application.Services.TweakProviders;
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

    [Fact]
    public void Loader_CanPreserveRawEntryIds()
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
                      "id": "policy.system.enable-virtualization",
                      "name": "Enable Virtualization",
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

        using var loader = new JsonTweakLoader(_directory, preserveEntryIds: true);

        Assert.Equal("policy.system.enable-virtualization", loader.GetTweakIds().Single());
    }

    [Fact]
    public void Loader_Creates_Tweaks_From_Numeric_Json_Values()
    {
        var filePath = Path.Combine(_directory, "batch.json");
        File.WriteAllText(
            filePath,
            """
            {
              "categories": {
                "power": {
                  "name": "Power",
                  "entries": [
                    {
                      "id": "power.control.class1-initial-unpark-count",
                      "name": "Class1 Initial Unpark Count",
                      "path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                      "value_name": "Class1InitialUnparkCount",
                      "type": "REG_DWORD",
                      "recommended_value": 64,
                      "verified": true
                    }
                  ]
                }
              }
            }
            """);

        using var loader = new JsonTweakLoader(_directory, preserveEntryIds: true);
        var registry = new Mock<IRegistryAccessor>(MockBehavior.Loose).Object;

        var tweaks = loader.CreateTweaks(registry).ToArray();

        Assert.Single(tweaks);
        Assert.Equal("power.control.class1-initial-unpark-count", tweaks[0].Id);
    }

    [Fact]
    public void Loader_ReportsDuplicateIdsAcrossFiles()
    {
        File.WriteAllText(
            Path.Combine(_directory, "a-batch.json"),
            """
            {
              "categories": {
                "test": {
                  "name": "Test",
                  "entries": [
                    {
                      "id": "duplicate_entry",
                      "name": "First Entry",
                      "path": "HKLM\\Software\\RegProbe",
                      "value_name": "ValueA",
                      "type": "REG_DWORD",
                      "recommended_value": 1,
                      "verified": true
                    }
                  ]
                }
              }
            }
            """);

        File.WriteAllText(
            Path.Combine(_directory, "b-batch.json"),
            """
            {
              "categories": {
                "test": {
                  "name": "Test",
                  "entries": [
                    {
                      "id": "duplicate_entry",
                      "name": "Second Entry",
                      "path": "HKLM\\Software\\RegProbe",
                      "value_name": "ValueB",
                      "type": "REG_DWORD",
                      "recommended_value": 2,
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
        Assert.Contains(loader.ValidationIssues, issue => issue.Code == "duplicate-id" && issue.EntryId == "duplicate_entry");
    }

    [Fact]
    public void Loader_HotReloadsSingleFileChanges()
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
                      "id": "entry_before",
                      "name": "Before",
                      "path": "HKLM\\Software\\RegProbe",
                      "value_name": "ValueBefore",
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
        loader.EnableHotReload();

        Assert.Contains("entry_before", loader.GetTweakIds());

        using var reloaded = new ManualResetEventSlim(false);
        loader.DefinitionsReloaded += () => reloaded.Set();

        File.WriteAllText(
            filePath,
            """
            {
              "categories": {
                "test": {
                  "name": "Test",
                  "entries": [
                    {
                      "id": "entry_after",
                      "name": "After",
                      "path": "HKLM\\Software\\RegProbe",
                      "value_name": "ValueAfter",
                      "type": "REG_DWORD",
                      "recommended_value": 2,
                      "verified": true
                    }
                  ]
                }
              }
            }
            """);

        Assert.True(reloaded.Wait(TimeSpan.FromSeconds(5)), "Expected DefinitionsReloaded after single-file rewrite.");
        Assert.True(SpinWait.SpinUntil(
            () => !loader.GetTweakIds().Contains("entry_before") && loader.GetTweakIds().Contains("entry_after"),
            TimeSpan.FromSeconds(5)),
            "Expected loader to replace only the changed file definitions.");
        Assert.Equal(1, loader.Count);
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
