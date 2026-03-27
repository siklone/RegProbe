using System;
using System.Collections.Generic;
using System.Linq;
using RegProbe.App.ViewModels;

namespace RegProbe.App.Services;

public sealed class TweakInsightInput
{
    public string Id { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string ImpactAreaLabel { get; init; } = string.Empty;
    public string RegistryPath { get; init; } = string.Empty;
    public TweakActionType ActionType { get; init; }
    public bool HasBatchDetails { get; init; }
}

public sealed class TweakInsightSnapshot
{
    public string DetectedFrom { get; init; } = string.Empty;
    public string Affects { get; init; } = string.Empty;
    public string RestartAdvice { get; init; } = string.Empty;
    public string RelatedSettings { get; init; } = string.Empty;
}

public sealed class TweakInsightFormatter
{
    public TweakInsightSnapshot Build(TweakInsightInput input)
    {
        input ??= new TweakInsightInput();

        return new TweakInsightSnapshot
        {
            DetectedFrom = BuildDetectedFrom(input),
            Affects = BuildAffects(input.Category),
            RestartAdvice = BuildRestartAdvice(input),
            RelatedSettings = BuildRelatedSettings(input.Category)
        };
    }

    private static string BuildDetectedFrom(TweakInsightInput input)
    {
        if (!string.IsNullOrWhiteSpace(input.RegistryPath))
        {
            if (input.RegistryPath.Contains("\\Software\\Policies\\", StringComparison.OrdinalIgnoreCase))
            {
                return $"Policy-backed registry value: {AbbreviateRegistryPath(input.RegistryPath)}";
            }

            if (input.HasBatchDetails)
            {
                return $"Multiple live registry values under {AbbreviateRegistryPath(input.RegistryPath)}";
            }

            return $"Live registry value: {AbbreviateRegistryPath(input.RegistryPath)}";
        }

        return input.ImpactAreaLabel switch
        {
            "Service" => "Current Windows service startup state on this PC.",
            "Task" => "Current scheduled task state on this PC.",
            "File" => "Files and cached folders on this PC.",
            "Command" => "A live Windows command or repair action.",
            "Settings" => "Current Windows setting state on this PC.",
            _ => "Current Windows state on this PC."
        };
    }

    private static string BuildAffects(string category)
    {
        return (category ?? string.Empty).ToLowerInvariant() switch
        {
            "privacy" => "Privacy, diagnostics, and background data collection.",
            "performance" => "Responsiveness, latency, and background performance behavior.",
            "security" => "Security posture, protection features, or hardening defaults.",
            "network" => "Connectivity, DNS, SMB, transport behavior, or adapter features.",
            "visibility" => "Windows shell visibility, taskbar layout, and Explorer UI.",
            "explorer" => "File Explorer and shell layout behavior.",
            "power" => "Power plans, idle timers, and device sleep behavior.",
            "system" => "Windows platform defaults, shell behavior, and built-in services.",
            "peripheral" => "USB, input, audio, or attached-device behavior.",
            "audio" => "Playback devices, communication behavior, and system sounds.",
            "cleanup" => "Cached data, logs, temporary files, or repair actions.",
            "notifications" => "Toast, lock screen, and cross-device notification behavior.",
            "misc" => "Microsoft app features, app integrations, or developer tools.",
            _ => "The Windows area this setting belongs to."
        };
    }

    private static string BuildRestartAdvice(TweakInsightInput input)
    {
        var id = input.Id ?? string.Empty;
        var category = (input.Category ?? string.Empty).ToLowerInvariant();

        if (id.Contains("winsock", StringComparison.OrdinalIgnoreCase))
        {
            return "Reboot required for the full network stack reset.";
        }

        if (category == "cleanup" && input.ActionType == TweakActionType.Clean)
        {
            return "Usually immediate. Restart only if Windows is holding a file open.";
        }

        if (category is "visibility" or "explorer")
        {
            return "Explorer restart or sign-out may be needed to refresh the shell.";
        }

        if (category == "network")
        {
            return "Some network changes need reconnect, sign-out, or reboot to fully apply.";
        }

        if (!string.IsNullOrWhiteSpace(input.RegistryPath) &&
            input.RegistryPath.Contains("\\Software\\Policies\\", StringComparison.OrdinalIgnoreCase) &&
            input.RegistryPath.StartsWith("HKLM\\", StringComparison.OrdinalIgnoreCase))
        {
            return "Machine-wide policy changes may need sign-out or reboot to fully apply.";
        }

        if (category is "power" or "system" or "security")
        {
            return "A reboot may be needed if Windows does not refresh the setting immediately.";
        }

        return "Usually applies without a full reboot.";
    }

    private static string BuildRelatedSettings(string category)
    {
        return (category ?? string.Empty).ToLowerInvariant() switch
        {
            "privacy" => "Notifications, diagnostics, search, and background apps.",
            "performance" => "Animations, Game Mode, power throttling, and taskbar effects.",
            "security" => "Defender, SmartScreen, exploit protection, and device security.",
            "network" => "DNS, SMB, IPv6, NetBIOS, and adapter behavior.",
            "visibility" => "Taskbar layout, hidden items, shell widgets, and Explorer UI.",
            "explorer" => "File extensions, hidden files, compact view, and taskbar layout.",
            "power" => "Power plans, wake timers, USB sleep, and device idle behavior.",
            "system" => "Clipboard, search, Game Bar, startup behavior, and shell defaults.",
            "peripheral" => "USB devices, input devices, audio routing, and device power.",
            "audio" => "Spatial audio, audio ducking, hidden devices, and system sounds.",
            "cleanup" => "Temp files, shader caches, update cache, and repair actions.",
            "notifications" => "Toast banners, lock screen, mirroring, and tile updates.",
            "misc" => "Edge, Office, OneDrive, and developer tool integrations.",
            _ => "Other settings in the same Windows area."
        };
    }

    private static string AbbreviateRegistryPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length <= 72)
        {
            return path;
        }

        var segments = path.Split('\\', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length <= 5)
        {
            return path;
        }

        var shortened = new List<string>
        {
            segments[0],
            segments[1],
            "...",
            segments[^2],
            segments[^1]
        };

        return string.Join("\\", shortened.Where(static part => !string.IsNullOrWhiteSpace(part)));
    }
}
