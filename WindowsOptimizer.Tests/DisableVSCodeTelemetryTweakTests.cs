using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine.Tweaks.Misc;

namespace WindowsOptimizer.Tests;

public sealed class DisableVSCodeTelemetryTweakTests
{
    [Fact]
    public async Task Apply_QuietProfile_PreservesUnmanagedSettings()
    {
        var root = CreateTempDirectory();
        try
        {
            var settingsPath = Path.Combine(root, "settings.json");
            await File.WriteAllTextAsync(settingsPath, """
            {
              "editor.fontSize": 16,
              "telemetry.telemetryLevel": "all"
            }
            """);

            var tweak = new DisableVSCodeTelemetryTweak(settingsPath)
            {
                SelectedChoiceKey = "quiet"
            };

            var detect = await tweak.DetectAsync(CancellationToken.None);
            var apply = await tweak.ApplyAsync(CancellationToken.None);
            var verify = await tweak.VerifyAsync(CancellationToken.None);

            Assert.Equal(TweakStatus.Detected, detect.Status);
            Assert.Equal(TweakStatus.Applied, apply.Status);
            Assert.Equal(TweakStatus.Verified, verify.Status);

            var json = await File.ReadAllTextAsync(settingsPath);
            var node = JsonNode.Parse(json)!.AsObject();
            Assert.Equal(16, node["editor.fontSize"]!.GetValue<int>());
            Assert.Equal("off", node["telemetry.telemetryLevel"]!.GetValue<string>());
            Assert.False(node["extensions.autoUpdate"]!.GetValue<bool>());
            Assert.False(node["git.autofetch"]!.GetValue<bool>());
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task Rollback_RestoresOriginalJsoncText()
    {
        var root = CreateTempDirectory();
        try
        {
            var settingsPath = Path.Combine(root, "settings.json");
            var originalJson = """
            {
              // keep my comments
              "editor.fontSize": 14,
            }
            """;

            await File.WriteAllTextAsync(settingsPath, originalJson);

            var tweak = new DisableVSCodeTelemetryTweak(settingsPath)
            {
                SelectedChoiceKey = "privacy"
            };

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

    [Fact]
    public async Task Apply_DefaultChoice_RemovesManagedKeysAndDeletesEmptyFile()
    {
        var root = CreateTempDirectory();
        try
        {
            var settingsPath = Path.Combine(root, "settings.json");
            await File.WriteAllTextAsync(settingsPath, """
            {
              "telemetry.telemetryLevel": "off",
              "workbench.enableExperiments": false
            }
            """);

            var tweak = new DisableVSCodeTelemetryTweak(settingsPath)
            {
                SelectedChoiceKey = "vscode-default"
            };

            var detect = await tweak.DetectAsync(CancellationToken.None);
            var apply = await tweak.ApplyAsync(CancellationToken.None);
            var verify = await tweak.VerifyAsync(CancellationToken.None);

            Assert.Equal(TweakStatus.Detected, detect.Status);
            Assert.Equal(TweakStatus.Applied, apply.Status);
            Assert.Equal(TweakStatus.Verified, verify.Status);
            Assert.False(File.Exists(settingsPath));
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "WindowsOptimizer.Tests", Guid.NewGuid().ToString("N"));
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
