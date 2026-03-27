using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.Core;
using RegProbe.Engine.Tweaks.Developer;

namespace RegProbe.Tests;

public sealed class EnableDockerWsl2BackendTweakTests
{
    [Fact]
    public async Task Apply_EnablesWslBackend_AndPreservesOtherSettings()
    {
        var root = CreateTempDirectory();
        try
        {
            var settingsPath = Path.Combine(root, "settings-store.json");
            await File.WriteAllTextAsync(settingsPath, """
            {
              "theme": "dark",
              "linuxVM": {
                "dockerDaemonOptions": {
                  "locked": false,
                  "value": "{\"debug\": false}"
                }
              }
            }
            """);

            var tweak = new EnableDockerWsl2BackendTweak(settingsPath);

            var detect = await tweak.DetectAsync(CancellationToken.None);
            var apply = await tweak.ApplyAsync(CancellationToken.None);
            var verify = await tweak.VerifyAsync(CancellationToken.None);

            Assert.Equal(TweakStatus.Detected, detect.Status);
            Assert.Equal(TweakStatus.Applied, apply.Status);
            Assert.Equal(TweakStatus.Verified, verify.Status);

            var json = await File.ReadAllTextAsync(settingsPath);
            var node = JsonNode.Parse(json)!.AsObject();
            Assert.Equal("dark", node["theme"]!.GetValue<string>());
            var linuxVm = node["linuxVM"]!.AsObject();
            var wslEngineEnabled = linuxVm["wslEngineEnabled"]!.AsObject();
            Assert.False(wslEngineEnabled["locked"]!.GetValue<bool>());
            Assert.True(wslEngineEnabled["value"]!.GetValue<bool>());
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task Rollback_RestoresOriginalJson()
    {
        var root = CreateTempDirectory();
        try
        {
            var settingsPath = Path.Combine(root, "settings-store.json");
            var originalJson = """
            {
              // keep my comments
              "theme": "light"
            }
            """;

            await File.WriteAllTextAsync(settingsPath, originalJson);

            var tweak = new EnableDockerWsl2BackendTweak(settingsPath);

            await tweak.DetectAsync(CancellationToken.None);
            await tweak.ApplyAsync(CancellationToken.None);
            var rollback = await tweak.RollbackAsync(CancellationToken.None);

            Assert.Equal(TweakStatus.RolledBack, rollback.Status);
            var restoredJson = await File.ReadAllTextAsync(settingsPath);
            Assert.Equal(originalJson, restoredJson);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "RegProbe.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTempDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
