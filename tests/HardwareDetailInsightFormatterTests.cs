using System.Collections.Generic;
using OpenTraceProject.App.HardwareDb;
using OpenTraceProject.App.Services;

public sealed class HardwareDetailInsightFormatterTests
{
    private readonly HardwareDetailInsightFormatter _formatter = new();

    [Fact]
    public void Build_ForGpu_IncludesDriverAndDirectXContext()
    {
        var snapshot = _formatter.Build(new HardwareDetailInsightInput
        {
            HardwareType = HardwareType.Gpu,
            Title = "GPU",
            Subtitle = "NVIDIA GeForce RTX 5070",
            Specs = new[]
            {
                new KeyValuePair<string, string>("VRAM", "12 GB"),
                new KeyValuePair<string, string>("VRAM Type", "GDDR7"),
                new KeyValuePair<string, string>("Driver Version", "32.0.15.9186"),
                new KeyValuePair<string, string>("Driver Date", "2026-01-20"),
                new KeyValuePair<string, string>("DirectX Version", "12 Ultimate")
            }
        });

        Assert.Contains("12 GB", snapshot.WhatThisIs);
        Assert.Contains("GDDR7", snapshot.WhatThisIs);
        Assert.Contains("32.0.15.9186", snapshot.DriverRuntime);
        Assert.Contains("DirectX", snapshot.DriverRuntime);
    }

    [Fact]
    public void Build_ForMemory_UsesSlotAndSpeedLanguage()
    {
        var snapshot = _formatter.Build(new HardwareDetailInsightInput
        {
            HardwareType = HardwareType.Memory,
            Title = "Memory",
            Subtitle = "31.9 GB DDR4",
            Specs = new[]
            {
                new KeyValuePair<string, string>("Capacity", "31.9 GB"),
                new KeyValuePair<string, string>("Type", "DDR4"),
                new KeyValuePair<string, string>("Speed", "3600 MHz"),
                new KeyValuePair<string, string>("Slots used", "2 / 4 slots occupied")
            }
        });

        Assert.Contains("3600 MHz", snapshot.WhatThisIs);
        Assert.Contains("2 / 4 slots occupied", snapshot.WhatThisIs);
        Assert.Contains("XMP", snapshot.DriverRuntime);
    }

    [Fact]
    public void Build_ForNetwork_PersistsFocusedAdapter()
    {
        var snapshot = _formatter.Build(new HardwareDetailInsightInput
        {
            HardwareType = HardwareType.Network,
            Title = "Network",
            Subtitle = "Intel Ethernet",
            SelectedTabHeader = "Ethernet",
            Specs = new[]
            {
                new KeyValuePair<string, string>("IPv4", "192.168.1.39"),
                new KeyValuePair<string, string>("Link Speed", "2500 Mbps"),
                new KeyValuePair<string, string>("DNS", "1.1.1.1")
            }
        });

        Assert.Contains("Focused item: Ethernet.", snapshot.WhatThisIs);
        Assert.Contains("2500 Mbps", snapshot.WhatThisIs);
        Assert.Contains("DNS", snapshot.CommonActions, System.StringComparison.OrdinalIgnoreCase);
    }
}
