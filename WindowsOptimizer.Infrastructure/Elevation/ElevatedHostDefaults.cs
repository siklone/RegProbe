namespace WindowsOptimizer.Infrastructure.Elevation;

public static class ElevatedHostDefaults
{
    public const string PipeName = "WindowsOptimizer.ElevatedHost";
    public const string ExecutableName = "WindowsOptimizer.ElevatedHost.exe";
    public const string OverridePathEnvVar = "WINDOWS_OPTIMIZER_ELEVATED_HOST_PATH";

    public static string GetPipeNameForProcess(int parentProcessId)
    {
        return parentProcessId > 0
            ? $"{PipeName}.{parentProcessId}"
            : PipeName;
    }
}
