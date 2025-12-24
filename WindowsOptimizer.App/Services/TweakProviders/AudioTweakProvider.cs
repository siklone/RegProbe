using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine;

namespace WindowsOptimizer.App.Services.TweakProviders;

public sealed class AudioTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Audio";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        return new List<ITweak>
        {
            CreateRegistryTweak(
                context,
                "audio.disable-ducking",
                "Disable Audio Ducking",
                "Prevents Windows from automatically lowering volume of other sounds during calls.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Multimedia\Audio",
                "UserDuckingPreference",
                RegistryValueKind.DWord,
                3,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "audio.disable-exclusive-mode",
                "Disable Exclusive Mode for Audio Devices",
                "Prevents applications from taking exclusive control of audio devices.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render",
                "DisableExclusiveMode",
                RegistryValueKind.DWord,
                1,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "audio.disable-system-sounds",
                "Disable System Sounds",
                "Turns off all Windows system sounds (beeps, notifications, etc.).",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"AppEvents\Schemes",
                "(Default)",
                RegistryValueKind.String,
                ".None",
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "audio.disable-spatial-sound",
                "Disable Spatial Sound",
                "Turns off Windows Sonic and other spatial audio features.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render\{default}\Properties",
                "{219ED5A0-9CBF-4F3A-B927-37C9E5C5F14F},1",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "audio.show-disconnected-devices",
                "Show Disconnected Audio Devices",
                "Displays all audio devices in sound settings, even if disconnected.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Multimedia\Audio\DeviceCpl",
                "ShowHiddenDevices",
                RegistryValueKind.DWord,
                1,
                requiresElevation: false)
        };
    }
}
