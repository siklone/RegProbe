using System.Collections.Generic;
using Microsoft.Win32;
using OpenTraceProject.Core;
using OpenTraceProject.Core.Registry;

namespace OpenTraceProject.Engine.Tweaks.Peripheral;

public static class MouseTweaks
{
    /// <summary>
    /// Disables raw mouse throttling for background windows (improves responsiveness in MouseTester)
    /// </summary>
    public static RegistryValueBatchTweak CreateDisableMouseThrottleTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>
        {
            // Disable raw mouse throttling
            new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Control Panel\Mouse", "RawMouseThrottleEnabled", RegistryValueKind.DWord, 0, RegistryView.Default),

            // Set minimum throttle duration (1ms = 1000Hz)
            new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Control Panel\Mouse", "RawMouseThrottleDuration", RegistryValueKind.DWord, 1, RegistryView.Default),

            // Set minimum leeway
            new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Control Panel\Mouse", "RawMouseThrottleLeeway", RegistryValueKind.DWord, 2, RegistryView.Default)
        };

        return new RegistryValueBatchTweak(
            id: "peripheral.mouse-disable-throttle",
            name: "Disable Mouse Throttling for Background Windows",
            description: "Disables raw mouse input throttling for background windows, improving mouse responsiveness. Useful when checking raw-input behavior with tools like MouseTester.",
            risk: TweakRiskLevel.Safe,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: false);
    }

    /// <summary>
    /// Disables Enhanced Pointer Precision (mouse acceleration)
    /// </summary>
    public static RegistryValueBatchTweak CreateDisableMouseAccelerationTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>
        {
            // Disable mouse acceleration
            new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", RegistryValueKind.String, "0", RegistryView.Default),

            new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold1", RegistryValueKind.String, "0", RegistryView.Default),

            new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold2", RegistryValueKind.String, "0", RegistryView.Default),

            // Keep sensitivity at default
            new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseSensitivity", RegistryValueKind.String, "10", RegistryView.Default)
        };

        return new RegistryValueBatchTweak(
            id: "peripheral.mouse-disable-acceleration",
            name: "Disable Enhanced Pointer Precision (Mouse Acceleration)",
            description: "Disables mouse acceleration for 1:1 mouse movement. Preferred by gamers and precision users.",
            risk: TweakRiskLevel.Safe,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: false);
    }
}
