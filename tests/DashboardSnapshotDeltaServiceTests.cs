using System;
using System.IO;
using OpenTraceProject.App.Services;

public sealed class DashboardSnapshotDeltaServiceTests
{
    [Fact]
    public void Compare_ReturnsBaselineWhenPreviousSnapshotIsMissing()
    {
        var current = new DashboardSnapshotDeltaState
        {
            CapturedAtLocal = new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.FromHours(3))
        };

        var result = DashboardSnapshotDeltaService.Compare(null, current);

        Assert.Equal("Baseline captured", result.Headline);
        Assert.Equal("Next refresh will highlight hardware changes.", result.Detail);
        Assert.Equal("Saved 2026-03-08 12:00", result.Context);
        Assert.False(result.HasChanges);
    }

    [Fact]
    public void Compare_SummarizesMultipleHardwareChanges()
    {
        var previous = new DashboardSnapshotDeltaState
        {
            CapturedAtLocal = new DateTimeOffset(2026, 3, 7, 21, 30, 0, TimeSpan.FromHours(3)),
            BiosVersion = "P3.80",
            GpuDriverVersion = "32.0.15.9000",
            DisplayCount = 1,
            StorageDriveCount = 3,
            SystemDriveCount = 1,
            ExternalDriveCount = 0
        };

        var current = new DashboardSnapshotDeltaState
        {
            CapturedAtLocal = new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.FromHours(3)),
            BiosVersion = "P3.90",
            GpuDriverVersion = "32.0.15.9186",
            DisplayCount = 2,
            StorageDriveCount = 4,
            SystemDriveCount = 1,
            ExternalDriveCount = 1
        };

        var result = DashboardSnapshotDeltaService.Compare(previous, current);

        Assert.Equal("4 changes since last snapshot", result.Headline);
        Assert.Equal("Firmware updated | GPU driver changed | Displays 1 -> 2 | +1 more", result.Detail);
        Assert.Equal("Compared with 2026-03-07 21:30", result.Context);
        Assert.True(result.HasChanges);
    }

    [Fact]
    public void UpdateAndSave_PersistsSnapshotAndReportsNoChangesOnSecondRun()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "OpenTraceProject.Tests", Guid.NewGuid().ToString("N"));
        var snapshotPath = Path.Combine(tempDirectory, "dashboard-snapshot.json");

        try
        {
            var service = new DashboardSnapshotDeltaService(snapshotPath);
            var current = new DashboardSnapshotDeltaState
            {
                CapturedAtLocal = new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.FromHours(3)),
                BiosVersion = "P3.90",
                GpuDriverVersion = "32.0.15.9186",
                AudioDriverVersion = "6.0.105.7",
                MemoryModuleCount = 2,
                MemorySlotCount = 4,
                DisplayCount = 2,
                PrimaryDisplayName = "E27FVC-E",
                PrimaryDisplayConnection = "DisplayPort",
                StorageDriveCount = 4,
                SystemDriveCount = 1,
                ExternalDriveCount = 1,
                PrimaryStorageModel = "Samsung SSD",
                UsbControllerCount = 2,
                UsbHubCount = 1,
                UsbDeviceCount = 24,
                PrimaryNetworkName = "Ethernet",
                NetworkLinkSpeed = "100 Mbps",
                SecureBootState = "Off",
                TpmVersion = "2.0"
            };

            var first = service.UpdateAndSave(current);
            var second = service.UpdateAndSave(current);

            Assert.Equal("Baseline captured", first.Headline);
            Assert.Equal("No hardware changes", second.Headline);
            Assert.True(File.Exists(snapshotPath));
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }
}
