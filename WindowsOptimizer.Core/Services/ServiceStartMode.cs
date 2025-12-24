namespace WindowsOptimizer.Core.Services;

public enum ServiceStartMode
{
    Unknown = -1,
    Boot = 0,
    System = 1,
    Automatic = 2,
    Manual = 3,
    Disabled = 4
}
