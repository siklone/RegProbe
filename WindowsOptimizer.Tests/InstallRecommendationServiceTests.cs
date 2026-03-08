using System.Linq;
using WindowsOptimizer.App.Services;

public sealed class InstallRecommendationServiceTests
{
    [Fact]
    public void BuildRecommendations_ReturnsAmdNvidiaDriversAndMissingRuntimes()
    {
        var service = new InstallRecommendationService();
        var context = new InstallRecommendationContext
        {
            Is64BitOs = true,
            CpuName = "AMD Ryzen 7 5700X3D",
            MotherboardModel = "B550 Pro4",
            MotherboardChipset = "B550",
            GpuName = "NVIDIA GeForce RTX 5070",
            GpuDriverVersion = "32.0.15.9186",
            GpuDriverDate = "2026-01-20"
        };
        var probeState = new InstallProbeState
        {
            HasWinget = true,
            VcRedistX64Installed = false,
            VcRedistX86Installed = false,
            DirectXLegacyRuntimeInstalled = false
        };

        var recommendations = service.BuildRecommendations(context, probeState);

        Assert.Contains(recommendations, item => item.Id == "driver.nvidia");
        Assert.Contains(recommendations, item => item.Id == "driver.amd-chipset");
        Assert.Contains(recommendations, item => item.Id == "runtime.vcredist-x64" && item.InstallCommand != null);
        Assert.Contains(recommendations, item => item.Id == "runtime.vcredist-x86" && item.InstallCommand != null);
        Assert.Contains(recommendations, item => item.Id == "runtime.directx-legacy" && item.StatusLabel == "Missing");
    }

    [Fact]
    public void BuildRecommendations_MarksInstalledRuntimesAsReady()
    {
        var service = new InstallRecommendationService();
        var context = new InstallRecommendationContext
        {
            Is64BitOs = true,
            CpuName = "AMD Ryzen 7 5700X3D",
            MotherboardChipset = "B550",
            GpuName = "NVIDIA GeForce RTX 5070"
        };
        var probeState = new InstallProbeState
        {
            HasWinget = true,
            VcRedistX64Installed = true,
            VcRedistX86Installed = true,
            DirectXLegacyRuntimeInstalled = true
        };

        var recommendations = service.BuildRecommendations(context, probeState);

        Assert.All(
            recommendations.Where(item => item.Category == "Runtime"),
            item => Assert.True(item.IsInstalled));
        Assert.All(
            recommendations.Where(item => item.Category == "Runtime"),
            item => Assert.Equal("Ready", item.StatusLabel));
    }

    [Fact]
    public void BuildRecommendations_AddsIntelAssistantWhenIntelHardwareNeedsCoverage()
    {
        var service = new InstallRecommendationService();
        var context = new InstallRecommendationContext
        {
            Is64BitOs = true,
            CpuName = "Intel Core Ultra 7 265K",
            NetworkHints = new[] { "Intel(R) Wi-Fi 7 BE200" }
        };
        var probeState = new InstallProbeState
        {
            HasWinget = true
        };

        var recommendations = service.BuildRecommendations(context, probeState);

        var intelAssistant = Assert.Single(recommendations, item => item.Id == "tool.intel-dsa");
        Assert.NotNull(intelAssistant.InstallCommand);
    }

    [Fact]
    public void HasVcRedistInstalled_DetectsBundledAndComponentInstalls()
    {
        var bundledNames = new[]
        {
            "Microsoft Visual C++ v14 Redistributable (x64) - 14.50.35719"
        };
        var componentNames = new[]
        {
            "Microsoft Visual C++ 2022 X86 Minimum Runtime - 14.50.35719",
            "Microsoft Visual C++ 2022 X86 Additional Runtime - 14.50.35719"
        };

        Assert.True(InstallRecommendationService.HasVcRedistInstalled(bundledNames, isX64: true));
        Assert.True(InstallRecommendationService.HasVcRedistInstalled(componentNames, isX64: false));
    }

    [Fact]
    public void HasVcRedistInstalled_DoesNotTreatSingleComponentAsCompleteInstall()
    {
        var installedApps = new[]
        {
            "Microsoft Visual C++ 2022 X64 Minimum Runtime - 14.50.35719"
        };

        Assert.False(InstallRecommendationService.HasVcRedistInstalled(installedApps, isX64: true));
    }

    [Fact]
    public void BuildRecommendations_SkipsIntelAssistantForIntelEthernetOnAmdPlatform()
    {
        var service = new InstallRecommendationService();
        var context = new InstallRecommendationContext
        {
            Is64BitOs = true,
            CpuName = "AMD Ryzen 7 5700X3D 8-Core Processor",
            MotherboardModel = "B550 Pro4",
            MotherboardChipset = "B550",
            GpuName = "NVIDIA GeForce RTX 5070",
            NetworkHints = new[] { "Intel(R) Ethernet Controller I226-V" }
        };

        var recommendations = service.BuildRecommendations(context, new InstallProbeState { HasWinget = true });

        Assert.DoesNotContain(recommendations, item => item.Id == "tool.intel-dsa");
    }

    [Fact]
    public void BuildRecommendations_UsesConciseIntelWirelessReason()
    {
        var service = new InstallRecommendationService();
        var context = new InstallRecommendationContext
        {
            Is64BitOs = true,
            CpuName = "AMD Ryzen 7 5700X3D 8-Core Processor",
            GpuName = "NVIDIA GeForce RTX 5070",
            NetworkHints = new[] { "Intel(R) Wi-Fi 7 BE200 320MHz" }
        };

        var recommendations = service.BuildRecommendations(context, new InstallProbeState { HasWinget = true });

        var intelAssistant = Assert.Single(recommendations, item => item.Id == "tool.intel-dsa");
        Assert.Equal("Wireless: Intel(R) Wi-Fi 7 BE200 320MHz", intelAssistant.Reason);
    }
}
