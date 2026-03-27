using System;
using System.Collections.Generic;

namespace RegProbe.App.HardwareDb.Models;

public sealed class HardwareComparisonResult
{
    public double LeftScore { get; init; }
    public double RightScore { get; init; }
    public double Delta => LeftScore - RightScore;
    public string Winner => Delta > 0 ? "Left" : Delta < 0 ? "Right" : "Tie";
}

public abstract class HardwareModelBase
{
    public string Id { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Series { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Generation { get; set; } = string.Empty;
    public string Codename { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }
    public string Architecture { get; set; } = string.Empty;
    public string ProcessNode { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string IconKey { get; set; } = string.Empty;
    public List<string> Aliases { get; set; } = new();
    public string NormalizedName { get; set; } = string.Empty;

    public virtual double GetScore() => 0;
    public virtual string GetTier() => "Unknown";

    public HardwareComparisonResult CompareTo(HardwareModelBase? other)
    {
        if (other == null)
        {
            return new HardwareComparisonResult { LeftScore = GetScore(), RightScore = 0 };
        }

        return new HardwareComparisonResult
        {
            LeftScore = GetScore(),
            RightScore = other.GetScore()
        };
    }
}

public sealed class CpuModel : HardwareModelBase
{
    public int CoreCount { get; set; }
    public int ThreadCount { get; set; }
    public double MaxBoostGHz { get; set; }

    public override double GetScore() => (CoreCount * 2.0) + ThreadCount + (MaxBoostGHz * 8.0);
    public override string GetTier() => GetScore() switch
    {
        >= 90 => "Enthusiast",
        >= 65 => "High",
        >= 40 => "Mid",
        _ => "Entry"
    };
}

public sealed class GpuModel : HardwareModelBase
{
    public int Units { get; set; }
    public int VramGB { get; set; }
    public double BoostMHz { get; set; }

    public override double GetScore() => (Units * 0.02) + (VramGB * 2.2) + (BoostMHz / 100.0);
    public override string GetTier() => GetScore() switch
    {
        >= 120 => "Enthusiast",
        >= 85 => "High",
        >= 50 => "Mid",
        _ => "Entry"
    };
}

public sealed class ChipsetModel : HardwareModelBase
{
    public int Units { get; set; }
}

public sealed class MemoryModel : HardwareModelBase
{
    public int Units { get; set; }
    public string MemoryType { get; set; } = string.Empty;
    public int MaxDataRateMTs { get; set; }
    public override double GetScore() => MaxDataRateMTs / 200.0;
}

public sealed class MemoryChipModel : HardwareModelBase
{
    public int Units { get; set; }
    public string VendorPartFamily { get; set; } = string.Empty;
}

public sealed class StorageControllerModel : HardwareModelBase
{
    public int Units { get; set; }
    public string Interface { get; set; } = string.Empty;
    public int MaxQueueDepth { get; set; }
}

public sealed class UsbControllerModel : HardwareModelBase
{
    public int Units { get; set; }
    public string UsbStandard { get; set; } = string.Empty;
}

public sealed class NetworkAdapterModel : HardwareModelBase
{
    public int Units { get; set; }
}

public sealed class MotherboardModel : HardwareModelBase
{
    public int Units { get; set; }
    public string Chipset { get; set; } = string.Empty;
    public string Socket { get; set; } = string.Empty;
}

public sealed class DisplayModel : HardwareModelBase
{
    public double ScreenSizeInches { get; set; }
    public string PanelType { get; set; } = string.Empty;
}

public sealed class HardwareDbDocument<TModel> where TModel : HardwareModelBase
{
    public string Version { get; set; } = "1.0.0";
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public List<TModel> Items { get; set; } = new();
}
