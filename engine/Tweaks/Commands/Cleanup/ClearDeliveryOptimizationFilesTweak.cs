using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.Core;

namespace RegProbe.Engine.Tweaks.Commands.Cleanup;

public sealed class ClearDeliveryOptimizationFilesTweak : FileCleanupTweak
{
    public ClearDeliveryOptimizationFilesTweak()
        : base(
            id: "cleanup.delivery-optimization",
            name: "Clear Delivery Optimization Files",
            description: "Deletes Delivery Optimization cache used for Windows Update P2P sharing. The DoSvc service will be stopped.",
            risk: TweakRiskLevel.Safe,
            requiresElevation: true)
    {
    }

    protected override IEnumerable<string> GetPathsToClean()
    {
        var winDir = Environment.GetEnvironmentVariable("WINDIR") ?? "C:\\Windows";
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        yield return Path.Combine(winDir, "SoftwareDistribution", "DeliveryOptimization");
        yield return Path.Combine(programData, "Microsoft", "Network", "Downloader");
    }

    protected override async Task<TweakResult?> StopServicesAsync(CancellationToken ct)
    {
        // Stop DoSvc (Delivery Optimization) service
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = "stop DoSvc",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                await process.WaitForExitAsync(ct);
                await Task.Delay(1000, ct);
            }

            return null;
        }
        catch
        {
            // Continue even if service stop fails
            return null;
        }
    }
}
