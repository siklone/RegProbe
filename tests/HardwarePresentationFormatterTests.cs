using System.Collections.Generic;
using RegProbe.App.Services;

public sealed class HardwarePresentationFormatterTests
{
    [Fact]
    public void BuildDisplayProductName_NormalizesStaleWindows11RegistryName()
    {
        var productName = HardwarePresentationFormatter.BuildDisplayProductName(
            "Windows 10 Professional",
            26200,
            "Professional");

        Assert.Equal("Windows 11 Pro", productName);
    }

    [Fact]
    public void ShouldShowRegistryProductName_HidesStaleRegistryValueOnWindows11()
    {
        var shouldShow = HardwarePresentationFormatter.ShouldShowRegistryProductName(
            "Windows 10 Pro",
            "Windows 11 Pro",
            26200,
            "Professional");

        Assert.False(shouldShow);
    }

    [Fact]
    public void InferVramType_ReturnsGddr7ForRtx5070()
    {
        var vramType = HardwarePresentationFormatter.InferVramType(
            "NVIDIA GeForce RTX 5070",
            "10DE");

        Assert.Equal("GDDR7", vramType);
    }

    [Fact]
    public void BuildMemoryModuleHeader_UsesChannelWhenDeviceLocatorRepeats()
    {
        var modules = new List<MemoryModuleData>
        {
            new() { Slot = "DIMM1", BankLabel = "P0 CHANNEL A" },
            new() { Slot = "DIMM1", BankLabel = "P0 CHANNEL B" }
        };

        var firstHeader = HardwarePresentationFormatter.BuildMemoryModuleHeader(modules[0], modules, 0);
        var secondHeader = HardwarePresentationFormatter.BuildMemoryModuleHeader(modules[1], modules, 1);

        Assert.Equal("Channel A / DIMM 1", firstHeader);
        Assert.Equal("Channel B / DIMM 1", secondHeader);
    }

    [Fact]
    public void BuildStorageInterfaceSummary_NormalizesNvmeOnScsiBackedWindowsStorage()
    {
        var summary = HardwarePresentationFormatter.BuildStorageInterfaceSummary("SCSI", "NVMe SSD");

        Assert.Equal("NVMe", summary);
    }

    [Fact]
    public void ClassifyUsbDeviceCategory_DetectsAudioDevices()
    {
        var category = HardwarePresentationFormatter.ClassifyUsbDeviceCategory(
            "Sound Blaster Audigy Fx V2",
            "USB",
            "usbaudio2",
            isController: false,
            isHub: false);

        Assert.Equal("Audio", category);
    }

    [Fact]
    public void BuildAudioDriverSummary_CombinesProviderAndVersion()
    {
        var summary = HardwarePresentationFormatter.BuildAudioDriverSummary("Creative", "6.0.105.7");

        Assert.Equal("Creative 6.0.105.7", summary);
    }

    [Fact]
    public void BuildMemoryPopulationSummary_ReturnsOccupiedAndTotalSlots()
    {
        var summary = HardwarePresentationFormatter.BuildMemoryPopulationSummary(2, 4);

        Assert.Equal("2 / 4 slots occupied", summary);
    }

    [Fact]
    public void BuildDisplayOccupancySummary_ReturnsActiveAndPrimaryCounts()
    {
        var summary = HardwarePresentationFormatter.BuildDisplayOccupancySummary(2, 1);

        Assert.Equal("2 active Â· 1 primary", summary);
    }

    [Fact]
    public void BuildStorageOccupancySummary_ReturnsSystemSecondaryAndExternalCounts()
    {
        var summary = HardwarePresentationFormatter.BuildStorageOccupancySummary(4, 1, 1);

        Assert.Equal("1 system Â· 3 secondary Â· 1 external", summary);
    }

    [Fact]
    public void BuildUsbOccupancySummary_ReturnsControllersHubsAndDevices()
    {
        var summary = HardwarePresentationFormatter.BuildUsbOccupancySummary(2, 1, 24);

        Assert.Equal("2 controllers Â· 1 hub Â· 24 devices", summary);
    }
}
