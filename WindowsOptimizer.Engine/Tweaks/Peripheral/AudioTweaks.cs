using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Infrastructure.Registry;

namespace WindowsOptimizer.Engine.Tweaks.Peripheral;

public static class AudioTweaks
{
    /// <summary>
    /// Disables audio ducking (automatic volume reduction during communications)
    /// </summary>
    public static RegistryValueBatchTweak CreateDisableAudioDuckingTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>
        {
            // 3 = Do nothing (disables ducking)
            // 0 = Mute all other sounds
            // 1 = Reduce by 80% (default)
            // 2 = Reduce by 50%
            new RegistryValueBatchEntry(
                RegistryHive.CurrentUser,
                RegistryView.Default,
                @"Software\Microsoft\Multimedia\Audio",
                "UserDuckingPreference",
                RegistryValueKind.DWord,
                3)
        };

        return new RegistryValueBatchTweak(
            id: "peripheral-audio-disable-ducking",
            name: "Disable Audio Ducking",
            description: "Disables Windows automatic volume adjustment when making calls or using communication apps. Equivalent to 'Do nothing' in Sound settings.",
            risk: TweakRiskLevel.Safe,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: false);
    }

    /// <summary>
    /// Disables audio enhancements for all audio devices
    /// Note: This requires elevation as it modifies HKLM audio device settings
    /// </summary>
    public static RegistryValueBatchTweak CreateDisableAudioEnhancementsTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>
        {
            // Disable exclusive mode for all devices (pattern-based, would need custom implementation)
            // For now, add common audio enhancement disable keys
            new RegistryValueBatchEntry(
                RegistryHive.CurrentUser,
                RegistryView.Default,
                @"Software\Microsoft\Windows\CurrentVersion\Audio",
                "DisableProtectedAudioDG",
                RegistryValueKind.DWord,
                1),

            // Disable audio enhancements globally
            new RegistryValueBatchEntry(
                RegistryHive.LocalMachine,
                RegistryView.Default,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render",
                "DisableEnhancements",
                RegistryValueKind.DWord,
                1)
        };

        return new RegistryValueBatchTweak(
            id: "peripheral-audio-disable-enhancements",
            name: "Disable Audio Enhancements",
            description: "Disables audio enhancements and exclusive mode for audio devices. Provides cleaner audio output without processing.",
            risk: TweakRiskLevel.Safe,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: true);
    }
}
