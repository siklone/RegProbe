using System;
using System.Collections.Generic;
using System.IO;
using RegProbe.Core.Commands;

namespace RegProbe.Infrastructure.Commands;

public sealed class CommandAllowlist
{
    private readonly Dictionary<string, List<string[]>> _allowed;
    private readonly string _systemDirectory;

    public CommandAllowlist(Dictionary<string, List<string[]>> allowed, string systemDirectory)
    {
        _allowed = allowed ?? throw new ArgumentNullException(nameof(allowed));
        _systemDirectory = string.IsNullOrWhiteSpace(systemDirectory)
            ? throw new ArgumentException("System directory is required.", nameof(systemDirectory))
            : Path.GetFullPath(systemDirectory);
    }

    public static CommandAllowlist CreateDefault()
    {
        var systemDirectory = Environment.SystemDirectory;
        var powercfg = Path.Combine(systemDirectory, "powercfg.exe");
        var dism = Path.Combine(systemDirectory, "dism.exe");
        var bcdedit = Path.Combine(systemDirectory, "bcdedit.exe");
        var sc = Path.Combine(systemDirectory, "sc.exe");
        var ipconfig = Path.Combine(systemDirectory, "ipconfig.exe");
        var netsh = Path.Combine(systemDirectory, "netsh.exe");
        var reg = Path.Combine(systemDirectory, "reg.exe");
        var chkdsk = Path.Combine(systemDirectory, "chkdsk.exe");
        var wevtutil = Path.Combine(systemDirectory, "wevtutil.exe");
        var vssadmin = Path.Combine(systemDirectory, "vssadmin.exe");
        var cleanmgr = Path.Combine(systemDirectory, "cleanmgr.exe");
        var cscript = Path.Combine(systemDirectory, "cscript.exe");
        var powershell = Path.Combine(systemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
        var regAllowlist = new List<string[]>
        {
            new[] { "query", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "/v", "EnableLUA" },
            new[] { "add", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "/v", "EnableLUA", "/t", "REG_DWORD", "/d", "0", "/f" },
            new[] { "add", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "/v", "EnableLUA", "/t", "REG_DWORD", "/d", "1", "/f" },
            new[] { "delete", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "/v", "EnableLUA", "/f" },
            new[] { "query", @"HKLM\SOFTWARE\Policies\Microsoft\FindMyDevice", "/v", "AllowFindMyDevice" },
            new[] { "add", @"HKLM\SOFTWARE\Policies\Microsoft\FindMyDevice", "/v", "AllowFindMyDevice", "/t", "REG_DWORD", "/d", "0", "/f" },
            new[] { "add", @"HKLM\SOFTWARE\Policies\Microsoft\FindMyDevice", "/v", "AllowFindMyDevice", "/t", "REG_DWORD", "/d", "1", "/f" },
            new[] { "delete", @"HKLM\SOFTWARE\Policies\Microsoft\FindMyDevice", "/v", "AllowFindMyDevice", "/f" }
        };

        static void AddRegDwordRule(List<string[]> allowlist, string keyPath, string valueName, params string[] values)
        {
            foreach (var value in values)
            {
                allowlist.Add(new[] { "add", keyPath, "/v", valueName, "/t", "REG_DWORD", "/d", value, "/f" });
            }

            allowlist.Add(new[] { "delete", keyPath, "/v", valueName, "/f" });
        }

        AddRegDwordRule(regAllowlist, @"HKCU\Software\Policies\Microsoft\Windows\Explorer", "DisableSearchHistory", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKCU\Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKCU\Software\Policies\Microsoft\Windows\Explorer", "HideRecommendedSection", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKCU\Software\Policies\Microsoft\Windows\Explorer", "HideRecommendedPersonalizedSites", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "TurnOffSPIAnimations", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\Explorer", "HideRecommendedSection", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\Explorer", "HideRecommendedPersonalizedSites", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessDiagnosticInfo", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\AppV\CEIP", "CEIPEnable", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\SQMClient\Windows", "CEIPEnable", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Messenger\Client", "CEIP", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\CloudContent", "DisableConsumerAccountStateContent", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "DoNotUseWebResults", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Edge", "SearchSuggestEnabled", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Edge", "LocalProvidersEnabled", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\MicrosoftEdge\SearchScopes", "ShowSearchSuggestionsGlobal", "0", "1", "2");

        var allowed = new Dictionary<string, List<string[]>>(StringComparer.OrdinalIgnoreCase)
        {
            [powercfg] = new List<string[]>
            {
                // Hibernation control
                new[] { "/hibernate", "off" },
                new[] { "/hibernate", "on" },

                // Query commands (safe read-only operations)
                new[] { "/query" },
                new[] { "/list" },
                new[] { "/availablesleepstates" },

                // USB selective suspend (AC power)
                new[] { "/setacvalueindex", "SCHEME_CURRENT", "SUB_USB", "USBSELECTIVESUSPEND", "0" },
                new[] { "/setacvalueindex", "SCHEME_CURRENT", "SUB_USB", "USBSELECTIVESUSPEND", "1" },

                // USB selective suspend (DC/battery power)
                new[] { "/setdcvalueindex", "SCHEME_CURRENT", "SUB_USB", "USBSELECTIVESUSPEND", "0" },
                new[] { "/setdcvalueindex", "SCHEME_CURRENT", "SUB_USB", "USBSELECTIVESUSPEND", "1" },

                // Hidden processor core parking settings
                new[] { "/qh", "SCHEME_CURRENT", "SUB_PROCESSOR", "CPMINCORES" },
                new[] { "/qh", "SCHEME_CURRENT", "SUB_PROCESSOR", "CPMAXCORES" },
                new[] { "/setacvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", "CPMINCORES", "100" },
                new[] { "/setdcvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", "CPMINCORES", "100" },
                new[] { "/setacvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", "CPMAXCORES", "100" },
                new[] { "/setdcvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", "CPMAXCORES", "100" },

                // Apply power scheme changes
                new[] { "/setactive", "SCHEME_CURRENT" },

                // Power scheme management
                new[] { "/getactivescheme" },
                new[] { "/setactive", "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c" }, // High performance
                new[] { "/setactive", "381b4222-f694-41f0-9685-ff5bb260df2e" }, // Balanced
                new[] { "/setactive", "a1841308-3541-4fab-bc81-f71556f20b4a" }  // Power saver
            },
            [dism] = new List<string[]>
            {
                // Reserved storage control
                new[] { "/online", "/Set-ReservedStorageState", "/State:Disabled", "/NoRestart" },
                new[] { "/online", "/Set-ReservedStorageState", "/State:Enabled", "/NoRestart" },
                new[] { "/online", "/Get-ReservedStorageState" },

                // Component store cleanup
                new[] { "/online", "/Cleanup-Image", "/StartComponentCleanup" },
                new[] { "/online", "/Cleanup-Image", "/StartComponentCleanup", "/ResetBase" },
                new[] { "/online", "/Cleanup-Image", "/AnalyzeComponentStore" },

                // Image health check (safe read-only)
                new[] { "/online", "/Cleanup-Image", "/CheckHealth" },
                new[] { "/online", "/Cleanup-Image", "/ScanHealth" }
            },
            [bcdedit] = new List<string[]>
            {
                // Query only (safe read-only operation)
                new[] { "/enum" },
                new[] { "/v" },

                // DEP (Data Execution Prevention) settings
                new[] { "/set", "{current}", "nx", "AlwaysOn" },
                new[] { "/set", "{current}", "nx", "AlwaysOff" },
                new[] { "/set", "{current}", "nx", "OptIn" },
                new[] { "/set", "{current}", "nx", "OptOut" },

                // Boot menu timeout
                new[] { "/timeout", "3" },
                new[] { "/timeout", "5" },
                new[] { "/timeout", "10" },
                new[] { "/timeout", "30" }
            },
            [sc] = new List<string[]>
            {
                // Service control - query (safe read-only)
                new[] { "query", "SysMain" },
                new[] { "query", "WSearch" },
                new[] { "query", "DoSvc" },
                new[] { "query", "FontCache" },

                // Service control - stop/start
                new[] { "stop", "SysMain" },
                new[] { "start", "SysMain" },
                new[] { "stop", "WSearch" },
                new[] { "start", "WSearch" },
                new[] { "stop", "DoSvc" },
                new[] { "start", "DoSvc" },
                new[] { "stop", "FontCache" },
                new[] { "start", "FontCache" }
            },
            [ipconfig] = new List<string[]>
            {
                // Display DNS cache (read-only)
                new[] { "/displaydns" },

                // Flush DNS cache
                new[] { "/flushdns" },

                // Release/renew IP
                new[] { "/release" },
                new[] { "/renew" },

                // Display all network info (read-only)
                new[] { "/all" }
            },
            [netsh] = new List<string[]>
            {
                // Winsock reset
                new[] { "winsock", "reset" },
                new[] { "winsock", "show", "catalog" },

                // IP reset
                new[] { "int", "ip", "reset" },

                // Interface management (read-only)
                new[] { "interface", "show", "interface" },
                new[] { "interface", "ip", "show", "config" }
            },
            [reg] = regAllowlist,
            [chkdsk] = new List<string[]>
            {
                // Check disk health (read-only)
                new[] { "C:" },
                new[] { "D:" },
                new[] { "E:" }
            },
            [wevtutil] = new List<string[]>
            {
                // Get log info (read-only)
                new[] { "gli", "Application" },
                new[] { "gli", "System" },
                new[] { "gli", "Security" },

                // Clear logs
                new[] { "cl", "Application" },
                new[] { "cl", "System" },
                new[] { "cl", "Security" },

                // Enum all logs (read-only)
                new[] { "el" }
            },
            [vssadmin] = new List<string[]>
            {
                // List shadow copies (read-only)
                new[] { "list", "shadows" },
                new[] { "list", "shadows", "/for=C:" },
                new[] { "list", "shadows", "/for=D:" },
                new[] { "list", "shadows", "/for=E:" },

                // Delete all shadow copies (RISKY - requires confirmation)
                new[] { "delete", "shadows", "/all", "/quiet" },
                new[] { "delete", "shadows", "/for=C:", "/all", "/quiet" },
                new[] { "delete", "shadows", "/for=D:", "/all", "/quiet" },
                new[] { "delete", "shadows", "/for=E:", "/all", "/quiet" }
            },
            [powershell] = new List<string[]>
            {
                // Clear Recycle Bin (safe, requires confirmation in UI)
                new[] { "-NoProfile", "-NonInteractive", "-Command", "Clear-RecycleBin", "-Force", "-ErrorAction", "SilentlyContinue" },
                new[] { "-NoProfile", "-NonInteractive", "-Command", "Clear-RecycleBin", "-DriveLetter", "C", "-Force", "-ErrorAction", "SilentlyContinue" },
                new[] { "-NoProfile", "-NonInteractive", "-Command", "Clear-RecycleBin", "-DriveLetter", "D", "-Force", "-ErrorAction", "SilentlyContinue" },

                // Clear clipboard (safe)
                new[] { "-NoProfile", "-NonInteractive", "-Command", "Set-Clipboard", "-Value", "$null" }
            },
            [cscript] = new List<string[]>
            {
                // Product key removal (slmgr.vbs)
                new[] { "//NoLogo", Path.Combine(systemDirectory, "slmgr.vbs"), "/cpky" },
                new[] { "//NoLogo", Path.Combine(systemDirectory, "slmgr.vbs"), "/dli" }
            }
        };

        return new CommandAllowlist(allowed, systemDirectory);
    }

    public bool IsAllowed(CommandRequest request, out string? reason)
    {
        if (request is null)
        {
            reason = "Request is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Executable))
        {
            reason = "Executable path is required.";
            return false;
        }

        var normalizedExecutable = NormalizeExecutable(request.Executable);
        if (normalizedExecutable is null)
        {
            reason = "Executable must be a full path under System32.";
            return false;
        }

        if (Path.GetFileName(normalizedExecutable).Equals("reg.exe", StringComparison.OrdinalIgnoreCase)
            && IsGeneralRegistryRequestAllowed(request.Arguments, out reason))
        {
            return true;
        }

        if (!_allowed.TryGetValue(normalizedExecutable, out var allowedArguments))
        {
            reason = "Executable is not allowlisted.";
            return false;
        }

        foreach (var args in allowedArguments)
        {
            if (ArgumentsMatch(args, request.Arguments))
            {
                reason = null;
                return true;
            }
        }

        reason = "Arguments are not allowlisted.";
        return false;
    }

    private static bool IsGeneralRegistryRequestAllowed(IReadOnlyList<string> arguments, out string? reason)
    {
        if (arguments.Count < 2)
        {
            reason = "Registry command arguments are incomplete.";
            return false;
        }

        var operation = arguments[0];
        if (!operation.Equals("add", StringComparison.OrdinalIgnoreCase)
            && !operation.Equals("delete", StringComparison.OrdinalIgnoreCase)
            && !operation.Equals("query", StringComparison.OrdinalIgnoreCase))
        {
            reason = "Only reg add/delete/query operations are allowed.";
            return false;
        }

        if (!IsRegistryHivePath(arguments[1]))
        {
            reason = "Registry path must target a supported hive.";
            return false;
        }

        reason = null;
        return true;
    }

    private static bool IsRegistryHivePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return path.StartsWith(@"HKLM\", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(@"HKCU\", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(@"HKCR\", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(@"HKU\", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(@"HKCC\", StringComparison.OrdinalIgnoreCase)
            || path.Equals("HKLM", StringComparison.OrdinalIgnoreCase)
            || path.Equals("HKCU", StringComparison.OrdinalIgnoreCase)
            || path.Equals("HKCR", StringComparison.OrdinalIgnoreCase)
            || path.Equals("HKU", StringComparison.OrdinalIgnoreCase)
            || path.Equals("HKCC", StringComparison.OrdinalIgnoreCase);
    }

    private string? NormalizeExecutable(string executable)
    {
        if (!Path.IsPathFullyQualified(executable))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(executable);
        var directory = Path.GetDirectoryName(fullPath);
        if (directory is null)
        {
            return null;
        }

        if (!directory.Equals(_systemDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return fullPath;
    }

    private static bool ArgumentsMatch(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
    {
        if (expected.Count != actual.Count)
        {
            return false;
        }

        for (var i = 0; i < expected.Count; i++)
        {
            if (!string.Equals(expected[i], actual[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
