using RegProbe.Core.Models;

namespace RegProbe.App.ViewModels;

internal sealed class WorkspaceTweakMetadataApplier
{
    public void Apply(IEnumerable<TweakItemViewModel> tweaks)
    {
        var tweakList = (tweaks ?? Enumerable.Empty<TweakItemViewModel>()).ToList();

        var aeroShake = tweakList.FirstOrDefault(t => t.Id == "system.aero-shake");
        if (aeroShake != null)
        {
            aeroShake.RegistryPath = @"HKCU\Software\Policies\Microsoft\Windows\Explorer\NoWindowMinimizingShortcuts";
            aeroShake.CodeExample = "reg add \"HKCU\\Software\\Policies\\Microsoft\\Windows\\Explorer\" /v \"NoWindowMinimizingShortcuts\" /t REG_DWORD /d 1 /f";
            aeroShake.ReferenceLinks.Add(new ReferenceLink("Policy Documentation", "https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-explorer"));
        }

        var gameMode = tweakList.FirstOrDefault(t => t.Id == "system.enable-game-mode");
        if (gameMode != null)
        {
            gameMode.RegistryPath = @"HKCU\Software\Microsoft\GameBar\AutoGameModeEnabled";
            gameMode.CodeExample = "reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v \"AutoGameModeEnabled\" /t REG_DWORD /d 1 /f";
            gameMode.SubOptions.Add(new TweakSubOption("Enable Game Bar", TweakSubOptionType.Toggle) { IsEnabled = true });
            gameMode.SubOptions.Add(new TweakSubOption("Allow Background DVR", TweakSubOptionType.Toggle));
            gameMode.ReferenceLinks.Add(new ReferenceLink("Xbox Support", "https://support.xbox.com/en-US/help/games-apps/game-setup-and-play/use-game-mode-gaming-on-pc"));
        }

        var clipboard = tweakList.FirstOrDefault(t => t.Id == "system.disable-clipboard-history");
        if (clipboard != null)
        {
            clipboard.RegistryPath = @"HKLM\Software\Policies\Microsoft\Windows\System\AllowClipboardHistory";
            clipboard.CodeExample = "reg add \"HKLM\\Software\\Policies\\Microsoft\\Windows\\System\" /v \"AllowClipboardHistory\" /t REG_DWORD /d 0 /f";
            clipboard.TargetValue = "0 (Disabled)";
            clipboard.ReferenceLinks.Add(new ReferenceLink("Security Best Practices", "https://learn.microsoft.com/en-us/windows/security/threat-protection/security-policy-settings/user-rights-assignment"));
        }

        var edgeBoost = tweakList.FirstOrDefault(t => t.Id == "system.disable-edge-startup-boost");
        if (edgeBoost != null)
        {
            edgeBoost.ActionType = TweakActionType.Clean;
            edgeBoost.ActionButtonText = "Disable Boost";
            edgeBoost.RegistryPath = @"HKLM\Software\Policies\Microsoft\Edge\StartupBoostEnabled";
            edgeBoost.TargetValue = "0 (Disabled)";
        }

        var mitigations = tweakList.FirstOrDefault(t => t.Id == "security.disable-system-mitigations");
        if (mitigations != null)
        {
            mitigations.RegistryPath = @"HKLM\System\CurrentControlSet\Control\Session Manager\kernel\MitigationOptions";
            mitigations.CodeExample = "# View current mitigation options\nGet-ItemProperty -Path 'HKLM:\\System\\CurrentControlSet\\Control\\Session Manager\\kernel' -Name MitigationOptions\n\n# Set mitigations to 22202022... (Hex)";
            mitigations.TargetValue = "22202022 (Optimized)";
            mitigations.ReferenceLinks.Add(new ReferenceLink("Exploit Protection Reference", "https://learn.microsoft.com/en-us/microsoft-365/security/defender-endpoint/exploit-protection-reference"));
            mitigations.ReferenceLinks.Add(new ReferenceLink("Bypass Mitigations Guide", "https://github.com/SirenOfTitan/Exploit-Mitigations-Bypass"));
        }

        var priority = tweakList.FirstOrDefault(t => t.Id == "system.priority-control");
        if (priority != null)
        {
            priority.RegistryPath = @"HKLM\System\CurrentControlSet\Control\PriorityControl\Win32PrioritySeparation";
            priority.CodeExample = "Set-ItemProperty -Path 'HKLM:\\System\\CurrentControlSet\\Control\\PriorityControl' -Name Win32PrioritySeparation -Value 38";
            priority.PriorityCalculator = new PriorityCalculatorViewModel { Bitmask = 0x26 };
            priority.ReferenceLinks.Add(new ReferenceLink("MSDN PriorityControl", "https://learn.microsoft.com/en-us/windows/win32/procthread/scheduling-priorities"));
        }

        var vscode = tweakList.FirstOrDefault(t => t.Id == "misc.disable-vscode-telemetry");
        if (vscode != null)
        {
            vscode.CodeExample =
                "\"telemetry.telemetryLevel\": \"off\"\n" +
                "\"workbench.enableExperiments\": false\n" +
                "\"update.mode\": \"manual\"\n" +
                "\"extensions.autoUpdate\": false";
            vscode.ReferenceLinks.Add(new ReferenceLink("VS Code telemetry docs", "https://code.visualstudio.com/docs/getstarted/telemetry", kind: ReferenceLinkKind.Docs));
            vscode.ReferenceLinks.Add(new ReferenceLink("VS Code update behavior", "https://code.visualstudio.com/docs/setup/setup-overview#_updates", kind: ReferenceLinkKind.Docs));
        }
    }
}
