using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RegProbe.App.Diagnostics;
using RegProbe.App.Utilities;

namespace RegProbe.App.Services;

public sealed class DashboardSnapshotDeltaService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public static DashboardSnapshotDeltaService Instance { get; } = new();

    private readonly string _snapshotPath;

    public DashboardSnapshotDeltaService(string? snapshotPath = null)
    {
        _snapshotPath = snapshotPath ?? Path.Combine(ApplicationPaths.DataDirectory, "Cache", "dashboard-snapshot.json");
    }

    public DashboardSnapshotDeltaResult UpdateAndSave(DashboardSnapshotDeltaState current)
    {
        var previous = LoadSnapshot();
        var result = Compare(previous, current);
        SaveSnapshot(current);
        return result;
    }

    internal DashboardSnapshotDeltaState? LoadSnapshot()
    {
        try
        {
            if (!File.Exists(_snapshotPath))
            {
                return null;
            }

            var json = File.ReadAllText(_snapshotPath);
            return JsonSerializer.Deserialize<DashboardSnapshotDeltaState>(json, SerializerOptions);
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[DashboardSnapshotDeltaService] Snapshot load failed: {ex.Message}");
            return null;
        }
    }

    internal void SaveSnapshot(DashboardSnapshotDeltaState current)
    {
        try
        {
            var directory = Path.GetDirectoryName(_snapshotPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(current, SerializerOptions);
            File.WriteAllText(_snapshotPath, json);
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[DashboardSnapshotDeltaService] Snapshot save failed: {ex.Message}");
        }
    }

    public static DashboardSnapshotDeltaResult Compare(DashboardSnapshotDeltaState? previous, DashboardSnapshotDeltaState current)
    {
        if (previous == null)
        {
            return new DashboardSnapshotDeltaResult
            {
                Headline = "Baseline captured",
                Detail = "Next refresh will highlight hardware changes.",
                Context = $"Saved {FormatTimestamp(current.CapturedAtLocal)}",
                HasChanges = false
            };
        }

        var changes = new List<string>();

        if (IsChanged(previous.BiosVersion, current.BiosVersion))
        {
            changes.Add("Firmware updated");
        }

        if (IsChanged(previous.GpuDriverVersion, current.GpuDriverVersion))
        {
            changes.Add("GPU driver changed");
        }

        if (IsChanged(previous.AudioDriverVersion, current.AudioDriverVersion))
        {
            changes.Add("Audio driver changed");
        }

        if (previous.DisplayCount != current.DisplayCount)
        {
            changes.Add($"Displays {previous.DisplayCount} -> {current.DisplayCount}");
        }
        else if (IsChanged(previous.PrimaryDisplayName, current.PrimaryDisplayName) ||
                 IsChanged(previous.PrimaryDisplayConnection, current.PrimaryDisplayConnection))
        {
            changes.Add("Display route changed");
        }

        if (previous.StorageDriveCount != current.StorageDriveCount ||
            previous.SystemDriveCount != current.SystemDriveCount ||
            previous.ExternalDriveCount != current.ExternalDriveCount)
        {
            changes.Add("Storage layout changed");
        }
        else if (IsChanged(previous.PrimaryStorageModel, current.PrimaryStorageModel))
        {
            changes.Add("Boot drive changed");
        }

        if (previous.MemoryModuleCount != current.MemoryModuleCount ||
            previous.MemorySlotCount != current.MemorySlotCount)
        {
            changes.Add("Memory population changed");
        }

        if (IsChanged(previous.PrimaryNetworkName, current.PrimaryNetworkName) ||
            IsChanged(previous.NetworkLinkSpeed, current.NetworkLinkSpeed))
        {
            changes.Add("Network path changed");
        }

        if (previous.UsbControllerCount != current.UsbControllerCount ||
            previous.UsbHubCount != current.UsbHubCount ||
            previous.UsbDeviceCount != current.UsbDeviceCount)
        {
            changes.Add("USB topology changed");
        }

        if (IsChanged(previous.SecureBootState, current.SecureBootState) ||
            IsChanged(previous.TpmVersion, current.TpmVersion))
        {
            changes.Add("Security posture changed");
        }

        if (changes.Count == 0)
        {
            return new DashboardSnapshotDeltaResult
            {
                Headline = "No hardware changes",
                Detail = "Matches the previous local snapshot.",
                Context = $"Compared with {FormatTimestamp(previous.CapturedAtLocal)}",
                HasChanges = false
            };
        }

        var detail = string.Join(" | ", changes.Take(3));
        if (changes.Count > 3)
        {
            detail = $"{detail} | +{changes.Count - 3} more";
        }

        return new DashboardSnapshotDeltaResult
        {
            Headline = changes.Count == 1 ? changes[0] : $"{changes.Count} changes since last snapshot",
            Detail = detail,
            Context = $"Compared with {FormatTimestamp(previous.CapturedAtLocal)}",
            HasChanges = true
        };
    }

    private static bool IsChanged(string? previous, string? current)
    {
        return !string.Equals(Normalize(previous), Normalize(current), StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string FormatTimestamp(DateTimeOffset timestamp)
    {
        return timestamp.ToString("yyyy-MM-dd HH:mm");
    }
}

public sealed class DashboardSnapshotDeltaState
{
    public DateTimeOffset CapturedAtLocal { get; set; } = DateTimeOffset.Now;
    public string? BiosVersion { get; set; }
    public string? GpuDriverVersion { get; set; }
    public string? AudioDriverVersion { get; set; }
    public int MemoryModuleCount { get; set; }
    public int MemorySlotCount { get; set; }
    public int DisplayCount { get; set; }
    public string? PrimaryDisplayName { get; set; }
    public string? PrimaryDisplayConnection { get; set; }
    public int StorageDriveCount { get; set; }
    public int SystemDriveCount { get; set; }
    public int ExternalDriveCount { get; set; }
    public string? PrimaryStorageModel { get; set; }
    public int UsbControllerCount { get; set; }
    public int UsbHubCount { get; set; }
    public int UsbDeviceCount { get; set; }
    public string? PrimaryNetworkName { get; set; }
    public string? NetworkLinkSpeed { get; set; }
    public string? SecureBootState { get; set; }
    public string? TpmVersion { get; set; }
}

public sealed class DashboardSnapshotDeltaResult
{
    public string Headline { get; init; } = "Baseline captured";
    public string Detail { get; init; } = "Next refresh will highlight hardware changes.";
    public string Context { get; init; } = string.Empty;
    public bool HasChanges { get; init; }
}
