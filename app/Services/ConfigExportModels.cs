namespace RegProbe.Application.Services;

public class ExportOptions
{
    public bool IncludeTweakStates { get; init; } = true;
    public bool IncludeDnsSettings { get; init; } = true;
    public bool IncludeAppSettings { get; init; } = true;
}

public class ExportedConfig
{
    public DateTime ExportDate { get; init; }
    public string AppVersion { get; init; } = string.Empty;
    public string MachineName { get; init; } = string.Empty;
    public ExportOptions Options { get; init; } = new();
    public List<string>? AppliedTweakIds { get; set; }
    public string? DnsProvider { get; set; }
    public Dictionary<string, object>? Settings { get; set; }
}

public class ImportResult
{
    public ImportResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public bool Success { get; }
    public string Message { get; }
    public int TweaksToApply { get; init; }
    public bool DnsToSet { get; init; }
    public int SettingsToApply { get; init; }

    public int TotalChanges => TweaksToApply + (DnsToSet ? 1 : 0) + SettingsToApply;
}
