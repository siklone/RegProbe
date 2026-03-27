using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;
using RegProbe.Engine.Tweaks;
using RegProbe.Infrastructure.Registry;
using Xunit;

namespace RegProbe.Tests;

public sealed class RegistryValuePresetBatchTweakTests
{
    [Fact]
    public async Task Detect_WhenCurrentValuesMatchSelectedPreset_ReturnsApplied()
    {
        var rootPath = BuildRootPath();
        SetValues(rootPath, enableValue: 0, sharingValue: 0);

        try
        {
            var tweak = BuildTweak(rootPath, "off");

            var result = await tweak.DetectAsync(CancellationToken.None);

            Assert.Equal(TweakStatus.Applied, result.Status);
            Assert.Equal("off", tweak.MatchedPresetKey);
            Assert.Equal("Off", tweak.MatchedPresetLabel);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(rootPath, false);
        }
    }

    [Fact]
    public async Task Detect_WhenCurrentValuesMatchDifferentPreset_ReturnsDetected()
    {
        var rootPath = BuildRootPath();
        SetValues(rootPath, enableValue: 1, sharingValue: 1);

        try
        {
            var tweak = BuildTweak(rootPath, "off");

            var result = await tweak.DetectAsync(CancellationToken.None);

            Assert.Equal(TweakStatus.Detected, result.Status);
            Assert.Equal("my-devices", tweak.MatchedPresetKey);
            Assert.Equal("My devices only", tweak.MatchedPresetLabel);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(rootPath, false);
        }
    }

    [Fact]
    public async Task ApplyVerifyRollback_WhenPresetChanges_RestoresOriginalPreset()
    {
        var rootPath = BuildRootPath();
        SetValues(rootPath, enableValue: 1, sharingValue: 2);

        try
        {
            var tweak = BuildTweak(rootPath, "off");

            var detect = await tweak.DetectAsync(CancellationToken.None);
            Assert.Equal(TweakStatus.Detected, detect.Status);
            Assert.Equal("everyone-nearby", tweak.MatchedPresetKey);

            var apply = await tweak.ApplyAsync(CancellationToken.None);
            Assert.Equal(TweakStatus.Applied, apply.Status);

            var verify = await tweak.VerifyAsync(CancellationToken.None);
            Assert.Equal(TweakStatus.Verified, verify.Status);
            Assert.Equal("off", tweak.MatchedPresetKey);

            var rollback = await tweak.RollbackAsync(CancellationToken.None);
            Assert.Equal(TweakStatus.RolledBack, rollback.Status);
            Assert.Equal("everyone-nearby", tweak.MatchedPresetKey);

            using var cdpKey = Registry.CurrentUser.OpenSubKey($@"{rootPath}\CDP");
            using var settingsKey = Registry.CurrentUser.OpenSubKey($@"{rootPath}\CDP\SettingsPage");
            Assert.Equal(1, Convert.ToInt32(cdpKey?.GetValue("EnableCdp")));
            Assert.Equal(2, Convert.ToInt32(cdpKey?.GetValue("RomeSdkChannelUserAuthzPolicy")));
            Assert.Equal(2, Convert.ToInt32(settingsKey?.GetValue("RomeSdkChannelUserAuthzPolicy")));
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(rootPath, false);
        }
    }

    private static RegistryValuePresetBatchTweak BuildTweak(string rootPath, string defaultPresetKey)
    {
        var presets = new List<RegistryValuePresetBatchOption>
        {
            new(
                "off",
                "Off",
                "Stops cross-device sharing on this PC.",
                new[]
                {
                    new RegistryValueBatchEntry(RegistryHive.CurrentUser, $@"{rootPath}\CDP", "EnableCdp", RegistryValueKind.DWord, 0),
                    new RegistryValueBatchEntry(RegistryHive.CurrentUser, $@"{rootPath}\CDP", "RomeSdkChannelUserAuthzPolicy", RegistryValueKind.DWord, 0),
                    new RegistryValueBatchEntry(RegistryHive.CurrentUser, $@"{rootPath}\CDP\SettingsPage", "RomeSdkChannelUserAuthzPolicy", RegistryValueKind.DWord, 0)
                }),
            new(
                "my-devices",
                "My devices only",
                "Allows only your own devices.",
                new[]
                {
                    new RegistryValueBatchEntry(RegistryHive.CurrentUser, $@"{rootPath}\CDP", "EnableCdp", RegistryValueKind.DWord, 1),
                    new RegistryValueBatchEntry(RegistryHive.CurrentUser, $@"{rootPath}\CDP", "RomeSdkChannelUserAuthzPolicy", RegistryValueKind.DWord, 1),
                    new RegistryValueBatchEntry(RegistryHive.CurrentUser, $@"{rootPath}\CDP\SettingsPage", "RomeSdkChannelUserAuthzPolicy", RegistryValueKind.DWord, 1)
                }),
            new(
                "everyone-nearby",
                "Everyone nearby",
                "Allows nearby devices to participate.",
                new[]
                {
                    new RegistryValueBatchEntry(RegistryHive.CurrentUser, $@"{rootPath}\CDP", "EnableCdp", RegistryValueKind.DWord, 1),
                    new RegistryValueBatchEntry(RegistryHive.CurrentUser, $@"{rootPath}\CDP", "RomeSdkChannelUserAuthzPolicy", RegistryValueKind.DWord, 2),
                    new RegistryValueBatchEntry(RegistryHive.CurrentUser, $@"{rootPath}\CDP\SettingsPage", "RomeSdkChannelUserAuthzPolicy", RegistryValueKind.DWord, 2)
                })
        };

        return new RegistryValuePresetBatchTweak(
            "test.registrypresetbatch",
            "Cross-device sharing",
            "Preset-backed registry tweak for cross-device sharing.",
            TweakRiskLevel.Safe,
            presets,
            defaultPresetKey,
            new LocalRegistryAccessor(),
            requiresElevation: false);
    }

    private static void SetValues(string rootPath, int enableValue, int sharingValue)
    {
        using var cdpKey = Registry.CurrentUser.CreateSubKey($@"{rootPath}\CDP", true);
        using var settingsKey = Registry.CurrentUser.CreateSubKey($@"{rootPath}\CDP\SettingsPage", true);

        cdpKey!.SetValue("EnableCdp", enableValue, RegistryValueKind.DWord);
        cdpKey.SetValue("RomeSdkChannelUserAuthzPolicy", sharingValue, RegistryValueKind.DWord);
        settingsKey!.SetValue("RomeSdkChannelUserAuthzPolicy", sharingValue, RegistryValueKind.DWord);
    }

    private static string BuildRootPath()
    {
        return $@"Software\RegProbe\Tests\RegistryValuePresetBatchTweak\{Guid.NewGuid():N}";
    }
}
