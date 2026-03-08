using System;

namespace WindowsOptimizer.App.Services.Hardware;

public sealed class MotherboardInfo
{
    public string? Manufacturer { get; set; }
    public string? Product { get; set; }
    public string? Version { get; set; }
    public string? SerialNumber { get; set; }
    public string? Tag { get; set; }
    public string? AssetTag { get; set; }
    public string? ChassisType { get; set; }
    public bool? Replaceable { get; set; }

    public string? BiosVendor { get; set; }
    public string? BiosVersion { get; set; }
    public DateTime? BiosReleaseDate { get; set; }
    public string? BiosMode { get; set; }
    public bool? SecureBootEnabled { get; set; }

    public string? Chipset { get; set; }
    public string? CpuSocket { get; set; }
    public string? FormFactor { get; set; }
    public string? PcieVersion { get; set; }

    public int? PcieX16Slots { get; set; }
    public int? PcieX4Slots { get; set; }
    public int? M2Slots { get; set; }
    public int? DimmSlots { get; set; }
    public int? SataPorts { get; set; }

    public int? MaxRamGb { get; set; }
    public string? MemoryType { get; set; }
    public int? MaxMemorySpeedMhz { get; set; }
    public int? DimmSlotsTotal { get; set; }
    public int? DimmSlotsUsed { get; set; }

    public string? DisplayName => string.IsNullOrWhiteSpace(Manufacturer) 
        ? Product ?? "Unknown Motherboard"
        : $"{Manufacturer} {Product}".Trim();
}
