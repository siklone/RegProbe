namespace OpenTraceProject.Infrastructure.Elevation;

public static class ElevatedHostDefaults
{
    public const string PipeName = "OpenTraceProject.ElevatedHost";
    public const string ExecutableName = "OpenTraceProject.ElevatedHost.exe";
    public const string OverridePathEnvVar = "OPEN_TRACE_PROJECT_ELEVATED_HOST_PATH";

    public static string GetPipeNameForProcess(int parentProcessId)
    {
        return parentProcessId > 0
            ? $"{PipeName}.{parentProcessId}"
            : PipeName;
    }
}
