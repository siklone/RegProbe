using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using RegProbe.Core;

namespace RegProbe.Engine.Tweaks.Commands.Cleanup;

public sealed class ClearBackgroundHistoryTweak : FileCleanupTweak
{
    public ClearBackgroundHistoryTweak()
        : base(
            id: "cleanup.background-history",
            name: "Clear Wallpaper History",
            description: "Clears the personalization wallpaper history registry entries and cached background files.",
            risk: TweakRiskLevel.Safe,
            requiresElevation: false)
    {
    }

    protected override IEnumerable<string> GetPathsToClean()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        yield return Path.Combine(appData, "Microsoft", "Windows", "Themes", "CachedFiles");
    }

    public override async Task<TweakResult> ApplyAsync(CancellationToken ct)
    {
        // First delete files
        var fileResult = await base.ApplyAsync(ct);

        // Then clear registry history
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Wallpapers",
                writable: true);

            if (key != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        key.DeleteValue($"BackgroundHistoryPath{i}", throwOnMissingValue: false);
                    }
                    catch
                    {
                        // Continue if value doesn't exist
                    }
                }
            }

            return new TweakResult(
                TweakStatus.Applied,
                "Wallpaper history cleared (files and registry)",
                DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            return new TweakResult(
                TweakStatus.Failed,
                $"Files cleared but registry cleanup failed: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex);
        }
    }
}
