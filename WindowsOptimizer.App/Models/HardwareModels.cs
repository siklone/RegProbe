using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace WindowsOptimizer.App.Models;

public class MotherboardDetailedModel
{
    public string Manufacturer { get; set; } = "Unknown";
    public string Model { get; set; } = "Unknown";
    public string Version { get; set; } = "";
    public string Chipset { get; set; } = "";
    public string BiosVendor { get; set; } = "";
    public string BiosVersion { get; set; } = "";
    public string BiosDate { get; set; } = "";
    public string GraphicInterface { get; set; } = "PCI-Express"; // Placeholder/Default
    public string GraphicVersion { get; set; } = "";
    public string LinkWidth { get; set; } = "";
}

public class MemoryDetailedModel
{
    public string Type { get; set; } = "DDR4"; // To be detected
    public string Size { get; set; } = "0 GB";
    public string Channels { get; set; } = "Dual"; // To be detected
    public string Frequency { get; set; } = ""; // NB Frequency
    public string DRAMFrequency { get; set; } = "";
    public string FSB_DRAM { get; set; } = "";
    
    // Timings
    public string CAS { get; set; } = "";
    public string tRCD { get; set; } = "";
    public string tRP { get; set; } = "";
    public string tRAS { get; set; } = "";
    public string CR { get; set; } = ""; // Command Rate

    public ObservableCollection<MemoryModuleDetailedModel> Modules { get; set; } = new();
}

public class MemoryModuleDetailedModel
{
    public string Slot { get; set; } = "";
    public string Size { get; set; } = "";
    public string Type { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string PartNumber { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string WeekYear { get; set; } = "";
    
    // JEDEC/SPD Table Mockup
    public ObservableCollection<SpdTimingModel> Timings { get; set; } = new();
}

public class SpdTimingModel
{
    public string Frequency { get; set; } = "";
    public string CAS { get; set; } = "";
    public string RAS { get; set; } = "";
    public string RC { get; set; } = "";
    public string Voltage { get; set; } = "";
}
