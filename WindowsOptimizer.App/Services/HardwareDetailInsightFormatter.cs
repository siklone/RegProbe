using System;
using System.Collections.Generic;
using System.Linq;
using WindowsOptimizer.App.HardwareDb;

namespace WindowsOptimizer.App.Services;

public sealed class HardwareDetailInsightFormatter
{
    public HardwareDetailInsightSnapshot Build(HardwareDetailInsightInput input)
    {
        var specs = BuildSpecMap(input.Specs);
        var title = Clean(input.Title);
        var subtitle = Clean(input.Subtitle);
        var focus = NormalizeFocus(input.SelectedTabHeader);

        return input.HardwareType switch
        {
            HardwareType.Os => BuildOsSnapshot(title, subtitle, specs),
            HardwareType.Cpu => BuildCpuSnapshot(title, subtitle, specs),
            HardwareType.Gpu => BuildGpuSnapshot(title, subtitle, specs),
            HardwareType.Motherboard => BuildMotherboardSnapshot(title, subtitle, specs),
            HardwareType.Memory => BuildMemorySnapshot(title, subtitle, specs),
            HardwareType.Storage => BuildStorageSnapshot(title, subtitle, specs, focus),
            HardwareType.Display => BuildDisplaySnapshot(title, subtitle, specs, focus),
            HardwareType.Network => BuildNetworkSnapshot(title, subtitle, specs, focus),
            HardwareType.Usb => BuildUsbSnapshot(title, subtitle, specs, focus),
            HardwareType.Audio => BuildAudioSnapshot(title, subtitle, specs, focus),
            _ => BuildFallbackSnapshot(title, subtitle)
        };
    }

    private static HardwareDetailInsightSnapshot BuildOsSnapshot(string title, string subtitle, IReadOnlyDictionary<string, string> specs)
    {
        var identity = FirstNonEmpty(subtitle, title, "Windows installation");
        var overview = JoinParts(
            ValueWithLabel("Build", GetSpec(specs, "Build")),
            GetSpec(specs, "Architecture"),
            ValueWithLabel("Uptime", GetSpec(specs, "Uptime")));

        return new HardwareDetailInsightSnapshot(
            CombineSentences(identity, overview),
            "Windows version and build decide feature availability, security capabilities, and which drivers or runtimes newer apps can use.",
            "Edition, build, and architecture form the baseline the rest of the driver and runtime stack has to match.",
            "Check build, edition, and uptime first when a game, driver, or update expects a newer Windows feature level.");
    }

    private static HardwareDetailInsightSnapshot BuildCpuSnapshot(string title, string subtitle, IReadOnlyDictionary<string, string> specs)
    {
        var identity = FirstNonEmpty(subtitle, title, "System processor");
        var layout = JoinParts(
            ValueWithLabel("Cores", GetSpec(specs, "Cores", "Physical Cores")),
            ValueWithLabel("Threads", GetSpec(specs, "Threads", "Logical processors", "Total Threads")),
            ValueWithLabel("Socket", GetSpec(specs, "Socket", "Package")));

        return new HardwareDetailInsightSnapshot(
            CombineSentences(identity, layout),
            "CPU clocks, thermals, and core layout shape frame pacing, multitasking headroom, and how responsive the whole PC feels under load.",
            "Windows reads topology and live clocks directly, while BIOS and chipset updates influence scheduling, boosting, and platform stability.",
            "Compare boost behavior, temperature, and core-thread layout when stutter, background slowdown, or inconsistent game performance shows up.");
    }

    private static HardwareDetailInsightSnapshot BuildGpuSnapshot(string title, string subtitle, IReadOnlyDictionary<string, string> specs)
    {
        var identity = FirstNonEmpty(subtitle, title, "Graphics adapter");
        var cardSummary = JoinParts(
            GetSpec(specs, "VRAM"),
            GetSpec(specs, "VRAM Type"),
            ValueWithLabel("DirectX", GetSpec(specs, "DirectX Version", "DirectX version")));
        var driverSummary = JoinParts(
            ValueWithLabel("Driver", GetSpec(specs, "Driver Version")),
            ValueWithLabel("Date", GetSpec(specs, "Driver Date")));

        return new HardwareDetailInsightSnapshot(
            CombineSentences(identity, cardSummary),
            "The GPU drives gaming, media playback, capture, encoding, and the display modes your monitors can actually sustain.",
            CombineSentences(
                "Graphics stability depends heavily on the installed display driver and DirectX feature support.",
                driverSummary),
            "Check driver age, VRAM headroom, and hotspot or fan behavior first when you see crashes, low utilization, or refresh-rate issues.");
    }

    private static HardwareDetailInsightSnapshot BuildMotherboardSnapshot(string title, string subtitle, IReadOnlyDictionary<string, string> specs)
    {
        var identity = FirstNonEmpty(subtitle, title, "Motherboard");
        var firmware = JoinParts(
            ValueWithLabel("Chipset", GetSpec(specs, "Chipset")),
            ValueWithLabel("BIOS", GetSpec(specs, "BIOS Version", "Version")),
            GetSpec(specs, "SMBIOS"));

        return new HardwareDetailInsightSnapshot(
            CombineSentences(identity, firmware),
            "The board defines memory support, storage lanes, expansion limits, firmware features, and much of the I/O behavior of the whole PC.",
            "This area is shaped more by BIOS, chipset, and controller firmware than by a single end-user device driver.",
            "Use this page to verify BIOS generation, lane-sharing surprises, storage topology, and whether platform firmware features are where you expect.");
    }

    private static HardwareDetailInsightSnapshot BuildMemorySnapshot(string title, string subtitle, IReadOnlyDictionary<string, string> specs)
    {
        var identity = FirstNonEmpty(subtitle, title, "System memory");
        var summary = JoinParts(
            GetSpec(specs, "Capacity", "Total"),
            GetSpec(specs, "Type"),
            GetSpec(specs, "Speed"),
            GetSpec(specs, "Slots used", "Slot Usage"));

        return new HardwareDetailInsightSnapshot(
            CombineSentences(identity, summary),
            "Memory capacity and speed affect multitasking, minimum frame times, shader compilation, and how often Windows has to lean on the page file.",
            "Windows reports module layout live, while BIOS training and XMP or EXPO settings decide the speed you actually run every day.",
            "Check occupied slots, active speed, and channel layout first when RAM capacity looks right but performance still feels uneven.");
    }

    private static HardwareDetailInsightSnapshot BuildStorageSnapshot(string title, string subtitle, IReadOnlyDictionary<string, string> specs, string focus)
    {
        var identity = FirstNonEmpty(subtitle, title, "Storage device");
        var summary = JoinParts(
            GetSpec(specs, "Capacity", "Size", "Total"),
            ValueWithLabel("Firmware", GetSpec(specs, "Firmware", "Firmware Version")),
            GetSpec(specs, "Interface", "Bus Type"));

        return new HardwareDetailInsightSnapshot(
            CombineSentences(identity, summary, FocusSentence(focus)),
            "Storage affects boot speed, game load times, patch installs, and how responsive Windows feels when the active drive is under pressure.",
            "Controller drivers, firmware, and free space all influence how fast and stable the drive feels in real use.",
            "Watch free space, active time, and firmware or controller behavior when installs stall, load times spike, or external drives disconnect.");
    }

    private static HardwareDetailInsightSnapshot BuildDisplaySnapshot(string title, string subtitle, IReadOnlyDictionary<string, string> specs, string focus)
    {
        var identity = FirstNonEmpty(subtitle, title, "Display device");
        var summary = JoinParts(
            GetSpec(specs, "Resolution", "Native Resolution"),
            GetSpec(specs, "Refresh Rate"),
            GetSpec(specs, "Connection", "Video Output"));

        return new HardwareDetailInsightSnapshot(
            CombineSentences(identity, summary, FocusSentence(focus)),
            "Display configuration decides refresh rate, output format, and whether the GPU is actually driving the monitor in the mode you expect.",
            "GPU driver, monitor identification, and the cable or port standard all work together here. One weak link can cap refresh rate or color depth.",
            "Verify active resolution, refresh rate, and the cable path first when text looks soft, Hz looks capped, or multi-monitor behavior feels off.");
    }

    private static HardwareDetailInsightSnapshot BuildNetworkSnapshot(string title, string subtitle, IReadOnlyDictionary<string, string> specs, string focus)
    {
        var identity = FirstNonEmpty(subtitle, title, "Network adapter");
        var summary = JoinParts(
            GetSpec(specs, "IPv4", "IP Address"),
            GetSpec(specs, "Link Speed"),
            ValueWithLabel("DNS", GetSpec(specs, "DNS")));

        return new HardwareDetailInsightSnapshot(
            CombineSentences(identity, summary, FocusSentence(focus)),
            "Network quality affects downloads, multiplayer latency, streaming, and anything that depends on stable adapter negotiation.",
            "Adapter drivers, Windows networking services, and DNS or DHCP state all influence the connection quality you actually get.",
            "Check the active adapter, link speed, gateway, and DNS first when latency rises, downloads look slow, or wired and wireless behave differently.");
    }

    private static HardwareDetailInsightSnapshot BuildUsbSnapshot(string title, string subtitle, IReadOnlyDictionary<string, string> specs, string focus)
    {
        var identity = FirstNonEmpty(subtitle, title, "USB topology");
        var summary = JoinParts(
            ValueWithLabel("Controllers", GetSpec(specs, "Controllers")),
            ValueWithLabel("Hubs", GetSpec(specs, "Hubs")),
            ValueWithLabel("Endpoints", GetSpec(specs, "USB Endpoints", "Device Count")));

        return new HardwareDetailInsightSnapshot(
            CombineSentences(identity, summary, FocusSentence(focus)),
            "USB layout affects peripherals, capture gear, storage accessories, and whether several busy devices are fighting on the same controller path.",
            "Chipset and controller drivers matter more here than app runtimes, and Windows power management can suspend devices that look idle.",
            "Use this page to spot crowded hubs, identify storage or audio devices on shared controllers, and trace disconnects back to the active endpoint.");
    }

    private static HardwareDetailInsightSnapshot BuildAudioSnapshot(string title, string subtitle, IReadOnlyDictionary<string, string> specs, string focus)
    {
        var identity = FirstNonEmpty(subtitle, title, "Audio device");
        var summary = JoinParts(
            ValueWithLabel("Provider", GetSpec(specs, "Driver Provider")),
            ValueWithLabel("Driver", GetSpec(specs, "Driver Version")),
            ValueWithLabel("Date", GetSpec(specs, "Driver Date")));

        return new HardwareDetailInsightSnapshot(
            CombineSentences(identity, summary, FocusSentence(focus)),
            "Audio devices affect playback quality, voice chat, routing, and any app that depends on stable low-latency input or output.",
            "Driver provider and version matter here, and virtual audio devices can reroute apps even when the physical hardware is fine.",
            "Confirm the default playback path, compare physical versus virtual devices, and review driver age first when crackle, delay, or routing bugs appear.");
    }

    private static HardwareDetailInsightSnapshot BuildFallbackSnapshot(string title, string subtitle)
    {
        var identity = FirstNonEmpty(subtitle, title, "Hardware device");

        return new HardwareDetailInsightSnapshot(
            $"{identity}.",
            "This page shows the most useful live identifiers and configuration details Windows is currently exposing for the selected device.",
            "Driver, firmware, and runtime relevance change by device type, so the exact details shown here depend on what Windows can read directly.",
            "Use this page as the quick reference before you compare driver versions, firmware, ports, or device-specific troubleshooting steps.");
    }

    private static Dictionary<string, string> BuildSpecMap(IEnumerable<KeyValuePair<string, string>> specs)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in specs ?? Enumerable.Empty<KeyValuePair<string, string>>())
        {
            var key = Clean(item.Key);
            var value = Clean(item.Value);
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value) || map.ContainsKey(key))
            {
                continue;
            }

            map[key] = value;
        }

        return map;
    }

    private static string GetSpec(IReadOnlyDictionary<string, string> specs, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (specs.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static string FirstNonEmpty(params string[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static string JoinParts(params string[] values)
    {
        return string.Join(" - ", values.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string ValueWithLabel(string label, string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : $"{label} {value}";
    }

    private static string CombineSentences(params string[] values)
    {
        var parts = values
            .Select(Clean)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(EnsureSentence);

        return string.Join(" ", parts);
    }

    private static string FocusSentence(string focus)
    {
        return string.IsNullOrWhiteSpace(focus) ? string.Empty : $"Focused item: {focus}.";
    }

    private static string NormalizeFocus(string? focus)
    {
        var cleaned = Clean(focus);
        return cleaned.Equals("Overview", StringComparison.OrdinalIgnoreCase) ? string.Empty : cleaned;
    }

    private static string EnsureSentence(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.EndsWith(".", StringComparison.Ordinal) ? value : $"{value}.";
    }

    private static string Clean(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }
}

public sealed class HardwareDetailInsightInput
{
    public HardwareType HardwareType { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public IEnumerable<KeyValuePair<string, string>> Specs { get; init; } = Enumerable.Empty<KeyValuePair<string, string>>();
    public string? SelectedTabHeader { get; init; }
}

public sealed record HardwareDetailInsightSnapshot(
    string WhatThisIs,
    string WhyItMatters,
    string DriverRuntime,
    string CommonActions);
