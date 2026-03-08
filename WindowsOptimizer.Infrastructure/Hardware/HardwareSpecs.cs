using System;

namespace WindowsOptimizer.Infrastructure.Hardware;

public sealed record CpuSpecs
{
    public string Cpuid { get; init; } = "";
    public string Brand { get; init; } = "";
    public string? Codename { get; init; }
    public int? Cores { get; init; }
    public int? Threads { get; init; }
    public int? BaseClockMhz { get; init; }
    public int? BoostClockMhz { get; init; }
    public int? TdpWatts { get; init; }
    public int? CacheL2Kb { get; init; }
    public int? CacheL3Kb { get; init; }
    public int? LithographyNm { get; init; }
    public string? ReleaseDate { get; init; }
    public string? Socket { get; init; }
    public string? Architecture { get; init; }
    public string? Features { get; init; }
    public int? MaxMemoryGb { get; init; }
    public int? MemoryChannels { get; init; }
    public int? PcieLanes { get; init; }
    public string? IntegratedGpu { get; init; }
    public bool IsFromDatabase { get; init; }

    public static CpuSpecs FromIdentity(CpuIdentity identity)
    {
        if (identity == null) throw new ArgumentNullException(nameof(identity));

        var brand = identity.WmiName ?? identity.LookupKey;
        return new CpuSpecs
        {
            Cpuid = identity.ProcessorId ?? identity.LookupKey,
            Brand = string.IsNullOrWhiteSpace(brand) ? "Unknown CPU" : brand,
            Cores = identity.Cores > 0 ? identity.Cores : null,
            Threads = identity.Threads > 0 ? identity.Threads : null,
            BaseClockMhz = identity.MaxClockSpeed > 0 ? identity.MaxClockSpeed : null,
            Architecture = identity.Architecture,
            IsFromDatabase = false
        };
    }
}

public sealed record GpuSpecs
{
    public string DeviceId { get; init; } = "";
    public string Brand { get; init; } = "";
    public string? Codename { get; init; }
    public int? CudaCores { get; init; }
    public int? StreamProcessors { get; init; }
    public int? BaseClockMhz { get; init; }
    public int? BoostClockMhz { get; init; }
    public int? MemorySizeMb { get; init; }
    public string? MemoryType { get; init; }
    public int? MemoryBusBits { get; init; }
    public double? MemoryBandwidthGbps { get; init; }
    public int? TdpWatts { get; init; }
    public string? ReleaseDate { get; init; }
    public string? Architecture { get; init; }
    public string? DirectxVersion { get; init; }
    public string? OpenglVersion { get; init; }
    public string? VulkanVersion { get; init; }
    public string? Features { get; init; }
    public bool IsFromDatabase { get; init; }

    public static GpuSpecs FromIdentity(GpuIdentity identity)
    {
        if (identity == null) throw new ArgumentNullException(nameof(identity));

        var brand = identity.WmiName ?? identity.DriverDesc ?? identity.LookupKey;
        return new GpuSpecs
        {
            DeviceId = identity.PciId ?? identity.LookupKey,
            Brand = string.IsNullOrWhiteSpace(brand) ? "Unknown GPU" : brand,
            MemorySizeMb = identity.AdapterRam > 0 ? (int)(identity.AdapterRam / (1024 * 1024)) : null,
            IsFromDatabase = false
        };
    }
}

public sealed record MotherboardSpecs
{
    public string BoardId { get; init; } = "";
    public string Manufacturer { get; init; } = "";
    public string Model { get; init; } = "";
    public string? Chipset { get; init; }
    public string? Socket { get; init; }
    public string? FormFactor { get; init; }
    public int? MemorySlots { get; init; }
    public int? MaxMemoryGb { get; init; }
    public string? MemoryType { get; init; }
    public int? MaxMemorySpeedMhz { get; init; }
    public string? PcieSlots { get; init; }
    public int? SataPorts { get; init; }
    public int? M2Slots { get; init; }
    public string? UsbPorts { get; init; }
    public string? AudioCodec { get; init; }
    public string? NetworkChip { get; init; }
    public string? WifiChip { get; init; }
    public string? BiosType { get; init; }
    public string? ReleaseDate { get; init; }
    public bool IsFromDatabase { get; init; }

    public static MotherboardSpecs FromIdentity(MotherboardIdentity identity)
    {
        if (identity == null) throw new ArgumentNullException(nameof(identity));

        var manufacturer = identity.Manufacturer ?? "Unknown";
        var model = identity.Product ?? identity.LookupKey;
        var boardId = identity.LookupKey;
        return new MotherboardSpecs
        {
            BoardId = string.IsNullOrWhiteSpace(boardId)
                ? $"{manufacturer} {model}".Trim()
                : boardId,
            Manufacturer = manufacturer,
            Model = string.IsNullOrWhiteSpace(model) ? "Unknown" : model,
            IsFromDatabase = false
        };
    }
}

public sealed record StorageSpecs
{
    public string ModelId { get; init; } = "";
    public string Manufacturer { get; init; } = "";
    public string Model { get; init; } = "";
    public string Type { get; init; } = "";
    public int? CapacityGb { get; init; }
    public string? Interface { get; init; }
    public string? FormFactor { get; init; }
    public int? SeqReadMbps { get; init; }
    public int? SeqWriteMbps { get; init; }
    public int? RandomReadIops { get; init; }
    public int? RandomWriteIops { get; init; }
    public string? NandType { get; init; }
    public string? Controller { get; init; }
    public int? DramCacheMb { get; init; }
    public int? TbwTb { get; init; }
    public string? ReleaseDate { get; init; }
    public bool IsFromDatabase { get; init; }

    public static StorageSpecs FromModel(string model)
    {
        var normalized = (model ?? string.Empty).Trim();
        return new StorageSpecs
        {
            ModelId = string.IsNullOrWhiteSpace(normalized) ? "Unknown" : normalized,
            Manufacturer = "Unknown",
            Model = string.IsNullOrWhiteSpace(normalized) ? "Unknown" : normalized,
            Type = "Unknown",
            IsFromDatabase = false
        };
    }
}

public sealed record RamSpecs
{
    public string PartNumber { get; init; } = "";
    public string Manufacturer { get; init; } = "";
    public string Model { get; init; } = "";
    public string Type { get; init; } = "";
    public int? SpeedMhz { get; init; }
    public int? CapacityGb { get; init; }
    public int? Modules { get; init; }
    public int? CasLatency { get; init; }
    public string? Timings { get; init; }
    public double? Voltage { get; init; }
    public bool? Ecc { get; init; }
    public string? XmpProfiles { get; init; }
    public string? ReleaseDate { get; init; }
    public bool IsFromDatabase { get; init; }

    public static RamSpecs FromModule(RamModule module)
    {
        if (module == null) throw new ArgumentNullException(nameof(module));

        var partNumber = (module.PartNumber ?? string.Empty).Trim();
        var manufacturer = module.Manufacturer ?? "Unknown";
        var model = string.IsNullOrWhiteSpace(partNumber) ? manufacturer : partNumber;
        return new RamSpecs
        {
            PartNumber = string.IsNullOrWhiteSpace(partNumber) ? model : partNumber,
            Manufacturer = manufacturer,
            Model = model,
            Type = module.MemoryType ?? "Unknown",
            SpeedMhz = module.SpeedMHz > 0 ? module.SpeedMHz : null,
            CapacityGb = module.CapacityGB > 0 ? (int)Math.Round(module.CapacityGB) : null,
            Modules = 1,
            IsFromDatabase = false
        };
    }
}
