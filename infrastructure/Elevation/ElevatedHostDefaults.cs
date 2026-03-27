namespace RegProbe.Infrastructure.Elevation;

public static class ElevatedHostDefaults
{
    public const string PipeName = "RegProbe.ElevatedHost";
    public const string ExecutableName = "RegProbe.ElevatedHost.exe";
    public const string OverridePathEnvVar = "REGPROBE_ELEVATED_HOST_PATH";

    public static string GetPipeNameForProcess(int parentProcessId)
    {
        return parentProcessId > 0
            ? $"{PipeName}.{parentProcessId}"
            : PipeName;
    }
}
