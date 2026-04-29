using System;
using System.IO;
using System.Linq;
using System.Threading;
using Moq;
using RegProbe.Core.Registry;
using RegProbe.Application.Services.TweakProviders;
using RegProbe.Engine.Tweaks;
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
    public void Loader_Creates_Batch_Tweaks_From_BatchEntries()
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
                      "id": "power.session-watchdog-timeouts",
                      "name": "Session Manager Watchdog Timeouts",
                      "batch_entries": [
                        {
                          "path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power",
                          "value_name": "WatchdogResumeTimeout",
                          "type": "REG_DWORD",
                          "target_value": 120
                        },
                        {
                          "path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power",
                          "value_name": "WatchdogSleepTimeout",
                          "type": "REG_DWORD",
                          "target_value": 300
                        }
                      ],
                      "verified": true
                    }
                  ]
                }
              }
            }
            """);

        using var loader = new JsonTweakLoader(_directory, preserveEntryIds: true);
        var registry = new Mock<IRegistryAccessor>(MockBehavior.Loose).Object;

        var tweak = Assert.Single(loader.CreateTweaks(registry));

        Assert.Equal("power.session-watchdog-timeouts", tweak.Id);
        Assert.IsType<RegistryValueBatchTweak>(tweak);
    }

    [Fact]
    public void Loader_Creates_Preset_Batch_Tweaks_From_Presets()
    {
        var filePath = Path.Combine(_directory, "batch.json");
        File.WriteAllText(
            filePath,
            """
            {
              "categories": {
                "policy": {
                  "name": "Policy",
                  "entries": [
                    {
                      "id": "policy.system.enable-virtualization",
                      "name": "Enable Virtualization",
                      "default_preset_key": "observed-baseline",
                      "presets": [
                        {
                          "key": "virtualization-off",
                          "label": "Virtualization Off",
                          "entries": [
                            {
                              "path": "HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System",
                              "value_name": "EnableVirtualization",
                              "type": "REG_DWORD",
                              "target_value": 0
                            }
                          ]
                        },
                        {
                          "key": "observed-baseline",
                          "label": "Observed Baseline",
                          "entries": [
                            {
                              "path": "HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System",
                              "value_name": "EnableVirtualization",
                              "type": "REG_DWORD",
                              "target_value": 1
                            }
                          ]
                        }
                      ],
                      "verified": true
                    }
                  ]
                }
              }
            }
            """);

        using var loader = new JsonTweakLoader(_directory, preserveEntryIds: true);
        var registry = new Mock<IRegistryAccessor>(MockBehavior.Loose).Object;

        var tweak = Assert.Single(loader.CreateTweaks(registry));
        var presetTweak = Assert.IsType<RegistryValuePresetBatchTweak>(tweak);

        Assert.Equal("policy.system.enable-virtualization", presetTweak.Id);
        Assert.Equal("observed-baseline", presetTweak.SelectedPresetKey);
        Assert.Equal(2, presetTweak.Presets.Count);
    }

    [Fact]
    public void Loader_Creates_ReadOnly_Subtree_Tweaks_From_Subtree_Entries()
    {
        var filePath = Path.Combine(_directory, "subtree.json");
        File.WriteAllText(
            filePath,
            """
            {
              "categories": {
                "power": {
                  "name": "Power",
                  "entries": [
                    {
                      "id": "power.control.power-request-override-subtree",
                      "name": "Power Request Override Subtree",
                      "path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerRequestOverride",
                      "value_name": "(subtree root, Driver, Process, Service)",
                      "type": "REG_SUBTREE",
                      "documentation": "research/records/power.control.power-request-override-subtree.json",
                      "verified": false
                    }
                  ]
                }
              }
            }
            """);

        using var loader = new JsonTweakLoader(_directory, preserveEntryIds: true);
        var registry = new Mock<IRegistryAccessor>(MockBehavior.Loose).Object;

        var tweak = Assert.Single(loader.CreateTweaks(registry));
        var subtreeTweak = Assert.IsType<RegistrySubtreeTweak>(tweak);

        Assert.Equal("power.control.power-request-override-subtree", subtreeTweak.Id);
        Assert.Equal(@"SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride", subtreeTweak.KeyPath);
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
