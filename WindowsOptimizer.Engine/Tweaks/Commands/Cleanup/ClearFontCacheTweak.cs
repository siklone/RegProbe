using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;

namespace WindowsOptimizer.Engine.Tweaks.Commands.Cleanup;

public sealed class ClearFontCacheTweak : FileCleanupTweak
{
    public ClearFontCacheTweak()
        : base(
            id: "cleanup-font-cache",
            name: "Clear Font Cache",
            description: "Clears the Windows font cache. Use this if fonts are not rendering properly. The FontCache service will be stopped and restarted.",
            risk: TweakRiskLevel.Safe,
            requiresElevation: true)
    {
    }

    protected override IEnumerable<string> GetPathsToClean()
    {
        var winDir = Environment.GetEnvironmentVariable("WINDIR") ?? "C:\\Windows";

        yield return Path.Combine(winDir, "ServiceProfiles", "LocalService", "AppData", "Local", "FontCache");
        yield return Path.Combine(winDir, "System32", "FNTCACHE.DAT");
    }

    protected override async Task<TweakResult?> StopServicesAsync(CancellationToken ct)
    {
        // Stop FontCache service before cleanup
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = "stop FontCache",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                await process.WaitForExitAsync(ct);
                // Give service time to stop
                await Task.Delay(1000, ct);
            }

            return null;
        }
        catch
        {
            return new TweakResult(
                TweakStatus.Failed,
                "Failed to stop FontCache service.",
                DateTimeOffset.UtcNow);
        }
    }

    protected override async Task<TweakResult?> StartServicesAsync(CancellationToken ct)
    {
        // Restart FontCache service after cleanup
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = "start FontCache",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                await process.WaitForExitAsync(ct);
            }

            return null;
        }
        catch
        {
            // Not critical if service doesn't restart
            return null;
        }
    }
}
