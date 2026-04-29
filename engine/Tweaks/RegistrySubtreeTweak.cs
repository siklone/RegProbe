using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using RegProbe.Core;

namespace RegProbe.Engine.Tweaks;

public sealed class RegistrySubtreeTweak : ITweak
{
    public RegistrySubtreeTweak(
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        RegistryHive hive,
        string keyPath,
        string subtreeLabel,
        RegistryView view = RegistryView.Default,
        bool requiresElevation = false)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Risk = risk;
        Hive = hive;
        KeyPath = string.IsNullOrWhiteSpace(keyPath)
            ? throw new ArgumentException("Key path is required.", nameof(keyPath))
            : keyPath;
        SubtreeLabel = string.IsNullOrWhiteSpace(subtreeLabel) ? "(subtree)" : subtreeLabel;
        View = view;
        RequiresElevation = requiresElevation;
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public TweakRiskLevel Risk { get; }
    public bool RequiresElevation { get; }
    public RegistryHive Hive { get; }
    public string KeyPath { get; }
    public string SubtreeLabel { get; }
    public RegistryView View { get; }

    public Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Task.FromResult(new TweakResult(
                TweakStatus.Skipped,
                "Registry subtree detection is only available on Windows hosts.",
                DateTimeOffset.UtcNow));
        }

        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(Hive, View);
            using var key = baseKey.OpenSubKey(KeyPath, false);
            if (key is null)
            {
                return Task.FromResult(new TweakResult(
                    TweakStatus.Detected,
                    "Research subtree is not present on this system.",
                    DateTimeOffset.UtcNow));
            }

            var subKeyCount = key.SubKeyCount;
            var valueCount = key.ValueCount;
            var message = $"Research subtree present ({subKeyCount} child keys, {valueCount} values at root).";
            return Task.FromResult(new TweakResult(TweakStatus.Detected, message, DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TweakResult(
                TweakStatus.Failed,
                $"Subtree detect failed: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex));
        }
    }

    public Task<TweakResult> ApplyAsync(CancellationToken ct)
        => Task.FromResult(new TweakResult(
            TweakStatus.Skipped,
            "This research subtree card is read-only and cannot be applied directly.",
            DateTimeOffset.UtcNow));

    public Task<TweakResult> VerifyAsync(CancellationToken ct)
        => Task.FromResult(new TweakResult(
            TweakStatus.Skipped,
            "This research subtree card is read-only. Review the attached evidence instead of running Verify.",
            DateTimeOffset.UtcNow));

    public Task<TweakResult> RollbackAsync(CancellationToken ct)
        => Task.FromResult(new TweakResult(
            TweakStatus.Skipped,
            "This research subtree card is read-only and has no rollback action.",
            DateTimeOffset.UtcNow));
}
