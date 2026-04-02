namespace RegProbe.Infrastructure.Elevation;

public static class ElevatedHostDefaults
{
    public const string PipeName = "RegProbe.ElevatedHost";
    public const string ExecutableName = "RegProbe.ElevatedHost.exe";
    public const string OverridePathEnvVar = "REGPROBE_ELEVATED_HOST_PATH";

    public static string CreateSessionToken()
    {
        return ElevatedHostSessionSecurity.CreateSessionToken();
    }

    public static string GetPipeNameForProcess(int parentProcessId, string? sessionToken = null)
    {
        var pipeName = parentProcessId > 0
            ? $"{PipeName}.{parentProcessId}"
            : PipeName;

        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return pipeName;
        }

        var nonce = ElevatedHostSessionSecurity.BuildPipeNonceSuffix(sessionToken);
        return $"{pipeName}.{nonce}";
    }
}
