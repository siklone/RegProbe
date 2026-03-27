namespace RegProbe.App.Models;

public class CpuDetailedModel
{
    public string Name { get; set; } = "Unknown";
    public string CodeName { get; set; } = "";
    public string Package { get; set; } = "Unknown";
    public string Technology { get; set; } = "";
    public string Specification { get; set; } = "";
    public string Family { get; set; } = "";
    public string ExtFamily { get; set; } = "";
    public string Model { get; set; } = "";
    public string ExtModel { get; set; } = "";
    public string Stepping { get; set; } = "";
    public string Revision { get; set; } = "";
    public string Instructions { get; set; } = "";
    public string CoreSpeed { get; set; } = "";
    public string Multiplier { get; set; } = "";
    public string BusSpeed { get; set; } = "";
    public string L1Cache { get; set; } = "";
    public string L2Cache { get; set; } = "";
    public string L3Cache { get; set; } = "";
    public string Cores { get; set; } = "0";
    public string Threads { get; set; } = "0";
    public string MaxTdp { get; set; } = "";
    public string SearchUrl { get; set; } = "";
    public string Voltage { get; set; } = "";
}
