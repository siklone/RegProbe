using System;
using System.Collections.Generic;
using System.IO;

namespace WindowsOptimizer.Infrastructure.Commands;

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
