using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;

namespace WindowsOptimizer.Engine.Tweaks.Peripheral;

public static class KeyboardTweaks
{
    /// <summary>
    /// Optimizes keyboard repeat delay and rate for faster typing
    /// </summary>
    public static RegistryValueBatchTweak CreateOptimizeKeyboardRepeatTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>
        {
            // Minimum repeat delay (0 = shortest delay before repeat starts)
            new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Control Panel\Keyboard", "KeyboardDelay", RegistryValueKind.String, "0", RegistryView.Default),

            // Maximum repeat rate (31 = fastest repeat)
            new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Control Panel\Keyboard", "KeyboardSpeed", RegistryValueKind.String, "31", RegistryView.Default),

            // Slower cursor blink rate (900ms, easier on eyes)
            new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Control Panel\Desktop", "CursorBlinkRate", RegistryValueKind.String, "900", RegistryView.Default)
        };

        return new RegistryValueBatchTweak(
            id: "peripheral.keyboard-optimize-repeat",
            name: "Optimize Keyboard Repeat Rate",
            description: "Sets keyboard to minimum repeat delay and maximum repeat rate for faster typing. Also slows cursor blink rate to 900ms.",
            risk: TweakRiskLevel.Safe,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: false);
    }

    /// <summary>
    /// Disables language switch hotkeys (Ctrl+Shift, Alt+Shift)
    /// </summary>
    public static RegistryValueBatchTweak CreateDisableLanguageSwitchHotkeyTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>
        {
            // 3 = Not assigned (disables hotkey)
            new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Keyboard Layout\Toggle", "Language Hotkey", RegistryValueKind.String, "3", RegistryView.Default),

            new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Keyboard Layout\Toggle", "Hotkey", RegistryValueKind.String, "3", RegistryView.Default),

            new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Keyboard Layout\Toggle", "Layout Hotkey", RegistryValueKind.String, "3", RegistryView.Default)
        };

        return new RegistryValueBatchTweak(
            id: "peripheral.keyboard-disable-language-hotkey",
            name: "Disable Language Switch Hotkey",
            description: "Disables Ctrl+Shift and Alt+Shift language switching hotkeys to prevent accidental language changes during gaming or typing.",
            risk: TweakRiskLevel.Safe,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: false);
    }
}
