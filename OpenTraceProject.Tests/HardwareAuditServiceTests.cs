using System.Linq;
using System.Threading;
using OpenTraceProject.App.Services;

public sealed class HardwareAuditServiceTests
{
    [Fact]
    public void CreateReport_FlagsPlaceholderMotherboardAndGenericDisplay()
    {
        var snapshot = new HardwareDetailSnapshot
        {
            Os = new OsHardwareData
            {
                NormalizedName = "Windows 11 Pro",
                ProductName = "Windows 11 Pro",
                BuildNumber = 26100,
                Architecture = "64-bit",
                IconKey = "os/windows11"
            },
            Cpu = new CpuHardwareData
            {
                Name = "Intel Core i7-14700K",
                Manufacturer = "Intel",
                Cores = 20,
                Threads = 28
            },
            Gpu = new GpuHardwareData
            {
                Name = "NVIDIA GeForce RTX 4090",
                Vendor = "NVIDIA",
                AdapterRamBytes = 0
            },
            Motherboard = new MotherboardHardwareData
            {
                Manufacturer = "To Be Filled By O.E.M.",
                Product = "Default string",
                Model = "Default string",
                Chipset = "Unknown"
            },
            Memory = new MemoryHardwareData
            {
                TotalBytes = 32L * 1024 * 1024 * 1024,
                ModuleCount = 2,
                MemoryType = "DDR5"
            },
            Storage = new StorageHardwareData
            {
                DeviceCount = 1,
                Disks =
                {
                    new DiskDriveData { Model = "Samsung 990 PRO 2TB", SizeBytes = 2L * 1024 * 1024 * 1024 * 1024, InterfaceType = "NVMe", MediaType = "NVMe SSD" }
                }
            },
            Displays = new DisplayHardwareData
            {
                DisplayCount = 1,
                Devices =
                {
                    new DisplayDeviceData
                    {
                        Name = "Generic PnP Monitor",
                        IsPrimary = true,
                        MatchMode = "PrefixAmbiguous",
                        ConnectionType = string.Empty
                    }
                }
            },
            Network = new NetworkHardwareData
            {
                AdapterCount = 1,
                AdapterUpCount = 1,
                PrimaryAdapterDescription = "Intel(R) Ethernet Controller I225-V",
                PrimaryLinkSpeed = "2.5 Gbps"
            },
            Usb = new UsbHardwareData
            {
                UsbControllerCount = 1,
                UsbDeviceCount = 2,
                PrimaryControllerName = "ASMedia ASM3142 USB Controller"
            }
        };

        var report = HardwareAuditService.Instance.CreateReport(snapshot);

        Assert.NotEmpty(report.Issues);
        Assert.True(report.ErrorCount >= 1);
        Assert.True(report.WarningCount >= 2);
        Assert.Contains(report.Issues, issue => issue.Section == "Motherboard" && issue.Severity == HardwareAuditSeverity.Error);
        Assert.Contains(report.Issues, issue => issue.Section == "Displays" && issue.Message.Contains("generic monitor name", System.StringComparison.OrdinalIgnoreCase));
        Assert.Contains(report.Issues, issue => issue.Section == "GPU" && issue.Field == "AdapterRamBytes");
    }

    [Fact]
    public async Task CreateReport_ProducesHighConfidenceCpuComponent_ForKnownDatabaseModel()
    {
        await OpenTraceProject.App.HardwareDb.HardwareKnowledgeDbService.Instance.InitializeAsync(CancellationToken.None);

        var snapshot = new HardwareDetailSnapshot
        {
            Cpu = new CpuHardwareData
            {
                Name = "Intel Core i7-14700K",
                Manufacturer = "Intel",
                Cores = 20,
                Threads = 28
            }
        };

        var report = HardwareAuditService.Instance.CreateReport(snapshot);
        var cpuComponent = report.Components.Single(component => component.Section == "CPU");

        Assert.True(cpuComponent.ConfidenceScore > 0);
        Assert.Equal("CPU", cpuComponent.Section);
        Assert.DoesNotContain(cpuComponent.Issues, issue => issue.Severity == HardwareAuditSeverity.Error);
    }

    [Fact]
    public void CreateReport_FlagsVirtualPrimaryNetworkWhenPhysicalAdapterExists()
    {
        var snapshot = new HardwareDetailSnapshot
        {
            Network = new NetworkHardwareData
            {
                AdapterCount = 2,
                AdapterUpCount = 2,
                PrimaryAdapterName = "WireGuard Tunnel",
                PrimaryAdapterDescription = "WireGuard Tunnel",
                PrimaryLinkSpeed = "10 Gbps",
                Adapters =
                {
                    new NetworkAdapterData
                    {
                        Name = "WireGuard Tunnel",
                        Description = "WireGuard Tunnel",
                        AdapterType = "Ethernet",
                        Status = "Up",
                        Ipv4 = "10.7.0.2",
                        IsPrimary = true
                    },
                    new NetworkAdapterData
                    {
                        Name = "Ethernet",
                        Description = "Intel(R) Ethernet Controller I225-V",
                        AdapterType = "Ethernet",
                        Status = "Connected",
                        Ipv4 = "192.168.1.25",
                        Gateway = "192.168.1.1"
                    }
                }
            }
        };

        var report = HardwareAuditService.Instance.CreateReport(snapshot);

        Assert.Contains(
            report.Issues,
            issue => issue.Section == "Network" &&
                     issue.Field == "PrimaryAdapter" &&
                     issue.Message.Contains("virtual", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateReport_FlagsMissingWindows11SecuritySignalsAndAudioInventory()
    {
        var snapshot = new HardwareDetailSnapshot
        {
            Os = new OsHardwareData
            {
                NormalizedName = "Windows 11 Pro",
                ProductName = "Windows 11 Pro",
                BuildNumber = 26100,
                Architecture = "64-bit"
            },
            Audio = new AudioHardwareData()
        };

        var report = HardwareAuditService.Instance.CreateReport(snapshot);

        Assert.Contains(
            report.Issues,
            issue => issue.Section == "OS" &&
                     issue.Field == "TpmVersion" &&
                     issue.Severity == HardwareAuditSeverity.Warning);
        Assert.Contains(
            report.Issues,
            issue => issue.Section == "OS" &&
                     issue.Field == "SecureBootState");
        Assert.Contains(
            report.Issues,
            issue => issue.Section == "Audio" &&
                     issue.Field == "DeviceCount" &&
                     issue.Severity == HardwareAuditSeverity.Info);
    }

    [Fact]
    public void CreateReport_DoesNotTreatAudioCategoryFallbackAsAuditProblem()
    {
        var snapshot = new HardwareDetailSnapshot
        {
            Audio = new AudioHardwareData
            {
                DeviceCount = 1,
                PrimaryDeviceName = "NVIDIA High Definition Audio",
                PrimaryManufacturer = "NVIDIA",
                PrimaryStatus = "OK"
            }
        };

        var report = HardwareAuditService.Instance.CreateReport(snapshot);
        var audioComponent = report.Components.Single(component => component.Section == "Audio");

        Assert.DoesNotContain(
            audioComponent.Issues,
            issue => issue.Field == "IconKey" &&
                     issue.Message.Contains("fell back", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateReport_FlagsVirtualPrimaryAudioWhenPhysicalDeviceExists()
    {
        var snapshot = new HardwareDetailSnapshot
        {
            Audio = new AudioHardwareData
            {
                DeviceCount = 3,
                PrimaryDeviceName = "NVIDIA Virtual Audio Device (Wave Extensible) (WDM)",
                PrimaryManufacturer = "NVIDIA",
                PrimaryStatus = "OK",
                AllDevices =
                {
                    "NVIDIA Virtual Audio Device (Wave Extensible) (WDM)",
                    "High Definition Audio Device",
                    "Sound Blaster Audigy Fx V2"
                }
            }
        };

        var report = HardwareAuditService.Instance.CreateReport(snapshot);

        Assert.Contains(
            report.Issues,
            issue => issue.Section == "Audio" &&
                     issue.Field == "PrimaryDeviceName" &&
                     issue.Message.Contains("virtual endpoint", System.StringComparison.OrdinalIgnoreCase));
    }
}
