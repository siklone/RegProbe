namespace OpenTraceProject.App.Services;

public static class CacheKeys
{
    // Use the same keys used by the runtime preload publisher (HardwarePreloadService)
    public const string Os = "OS";
    public const string Cpu = "CPU";
    public const string Gpu = "GPU";
    public const string Motherboard = "Motherboard";
    public const string Memory = "Memory";
    public const string Storage = "Storage";
    public const string Network = "Network";
    public const string Display = "Displays";
    public const string Usb = "USB";
}
