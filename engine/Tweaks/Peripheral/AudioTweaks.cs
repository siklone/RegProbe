using System.Collections.Generic;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;

namespace RegProbe.Engine.Tweaks.Peripheral;

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
            new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Multimedia\Audio", "UserDuckingPreference", RegistryValueKind.DWord, 3, RegistryView.Default)
        };

        return new RegistryValueBatchTweak(
            id: "peripheral.audio-disable-ducking",
            name: "Disable Audio Ducking",
            description: "Disables Windows automatic volume adjustment when making calls or using communication apps. Equivalent to 'Do nothing' in Sound settings.",
            risk: TweakRiskLevel.Safe,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: false);
    }

    /// <summary>
    /// Checks audio enhancement endpoint flags without blindly mutating protected MMDevices keys.
    /// </summary>
    public static AudioEnhancementsTweak CreateDisableAudioEnhancementsTweak(IRegistryAccessor registryAccessor)
        => new(registryAccessor);
}
