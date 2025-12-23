using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Infrastructure.Registry;

namespace WindowsOptimizer.Engine.Tweaks.Misc;

public static class SetNotepadPlusPlusDefaultEditorTweak
{
    public static RegistryValueBatchTweak CreateSetNotepadPlusPlusDefaultEditorTweak(IRegistryAccessor registryAccessor)
    {
        // Set Notepad++ as default editor for batch files and text files
        var entries = new List<RegistryValueBatchEntry>
        {
            // Batch files editor
            new RegistryValueBatchEntry(RegistryHive.ClassesRoot, @"batfile\shell\edit\command", "", RegistryValueKind.String, "\"C:\\Program Files\\Notepad++\\notepad++.exe\" \"%1\"", RegistryView.Default),

            // Text files editor (optional, can be extended)
            new RegistryValueBatchEntry(RegistryHive.ClassesRoot, @"txtfile\shell\edit\command", "", RegistryValueKind.String, "\"C:\\Program Files\\Notepad++\\notepad++.exe\" \"%1\"", RegistryView.Default)
        };

        return new RegistryValueBatchTweak(
            id: "misc-notepadplusplus-default-editor",
            name: "Set Notepad++ as Default Editor",
            description: "Sets Notepad++ as the default editor for batch files and text files. Requires Notepad++ to be installed.",
            risk: TweakRiskLevel.Safe,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: true);
    }
}
