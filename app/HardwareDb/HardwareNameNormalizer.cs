using System;
using System.Text.RegularExpressions;

namespace OpenTraceProject.App.HardwareDb;

public static class HardwareNameNormalizer
{
    private static readonly Regex MultiSpace = new("\\s+", RegexOptions.Compiled);
    private static readonly Regex JunkTokens = new(@"\b\(R\)|\(TM\)|CPU|Processor|Graphics|Adapter|Series|To\s+Be\s+Filled\s+By\s+O\.?E\.?M\.?|Default\s+string|None|Unknown|Standard\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var value = raw.Trim().ToLowerInvariant();
        value = value.Replace("geforce", "nvidia geforce");
        value = value.Replace("radeon tm", "radeon");
        value = value.Replace('-', ' ');
        value = value.Replace("wi fi", "wifi");
        value = JunkTokens.Replace(value, " ");
        value = Regex.Replace(value, "[^a-z0-9+\\-\\s]", " ");
        value = MultiSpace.Replace(value, " ").Trim();
        return value;
    }

    public static string ExtractCpuSeries(string? cpuName)
    {
        var value = Normalize(cpuName);
        var ryzen = Regex.Match(value, @"ryzen\s+[3579]");
        if (ryzen.Success) return ryzen.Value;

        var intel = Regex.Match(value, @"core\s+i[3579]");
        if (intel.Success) return intel.Value;

        if (value.Contains("core ultra", StringComparison.Ordinal)) return "core ultra";
        return string.Empty;
    }

    public static string ExtractGpuSeries(string? gpuName)
    {
        var value = Normalize(gpuName);
        var rtx = Regex.Match(value, @"rtx\s*\d{3,4}");
        if (rtx.Success) return rtx.Value;

        var gtx = Regex.Match(value, @"gtx\s*\d{3,4}");
        if (gtx.Success) return gtx.Value;

        var rx = Regex.Match(value, @"rx\s*\d{3,4}");
        if (rx.Success) return rx.Value;

        return string.Empty;
    }
}
