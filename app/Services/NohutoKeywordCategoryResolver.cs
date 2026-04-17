namespace RegProbe.App.Services;

internal static class NohutoKeywordCategoryResolver
{
    public static string Resolve(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Misc";
        }

        var key = value.ToLowerInvariant();

        if (key.Contains("power") || key.Contains("hiber") || key.Contains("sleep") || key.Contains("acpi")) return "Power";
        if (key.Contains("tcpip") || key.Contains("dnscache") || key.Contains("dns") || key.Contains("net") || key.Contains("nic") || key.Contains("ndis") || key.Contains("nla")) return "Network";
        if (key.Contains("defender") || key.Contains("lsa") || key.Contains("security") || key.Contains("tpm") || key.Contains("bitlocker") || key.Contains("crypt")) return "Security";
        if (key.Contains("privacy") || key.Contains("error-report") || key.Contains("telemetry")) return "Privacy";
        if (key.Contains("audio") || key.Contains("sound")) return "Audio";
        if (key.Contains("explorer") || key.Contains("desktop") || key.Contains("mouse") || key.Contains("visibility") || key.Contains("monitor")) return "Display";
        if (key.Contains("dxg") || key.Contains("graphics") || key.Contains("dwm") || key.Contains("gpu") || key.Contains("nvidia")) return "Graphics";
        if (key.Contains("perf") || key.Contains("stornvme") || key.Contains("storport") || key.Contains("storage") || key.Contains("disk") || key.Contains("nvme")) return "Storage";
        if (key.Contains("usb") || key.Contains("xhci") || key.Contains("kbd") || key.Contains("mou") || key.Contains("input") || key.Contains("touch") || key.Contains("pen") || key.Contains("peripheral")) return "Peripheral";
        if (key.Contains("policy")) return "Policy";
        if (key.Contains("cleanup")) return "Maintenance";
        if (key.Contains("trace") || key.Contains("record") || key.Contains("registry")) return "Research";
        if (key.Contains("kernel") || key.Contains("system") || key.Contains("mmcss") || key.Contains("service") || key.Contains("session") || key.Contains("pnp") || key.Contains("wdf")) return "System";

        return "Misc";
    }
}
