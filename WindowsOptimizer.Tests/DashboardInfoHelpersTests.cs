using System.Collections.Generic;
using System.Text;
using WindowsOptimizer.App.ViewModels;
using Xunit;

public sealed class DashboardInfoHelpersTests
{
    [Fact]
    public void SumPageFileBytes_SumsPositiveValues()
    {
        var total = DashboardInfoHelpers.SumPageFileBytes(new long[] { 1024, 0, -1, 2048 });
        Assert.Equal(3072, total);
    }

    [Fact]
    public void SumPositiveInts_IgnoresNullAndNonPositiveValues()
    {
        var total = DashboardInfoHelpers.SumPositiveInts(new int?[] { 2, null, 0, -1, 3 });
        Assert.Equal(5, total);
    }

    [Fact]
    public void SumPositiveInts_ReturnsNullWhenNoPositiveValues()
    {
        var total = DashboardInfoHelpers.SumPositiveInts(new int?[] { null, 0, -2 });
        Assert.Null(total);
    }

    [Theory]
    [InlineData("MONITOR\\DEL1234\\ABC", "DISPLAY\\DEL1234\\ABC")]
    [InlineData("DISPLAY\\DEL1234\\ABC", "DISPLAY\\DEL1234\\ABC")]
    [InlineData("  DISPLAY\\X  ", "DISPLAY\\X")]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void NormalizeMonitorInstanceId_ReturnsExpected(string? input, string expected)
    {
        var normalized = DashboardInfoHelpers.NormalizeMonitorInstanceId(input);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("DISPLAY\\DEL1234\\4&123", "DISPLAY\\DEL1234")]
    [InlineData("DISPLAY\\DEL1234", "DISPLAY\\DEL1234")]
    [InlineData(null, null)]
    [InlineData("", null)]
    public void GetMonitorInstancePrefix_ReturnsExpected(string? input, string? expected)
    {
        var prefix = DashboardInfoHelpers.GetMonitorInstancePrefix(input);
        Assert.Equal(expected, prefix);
    }

    [Fact]
    public void GetMonitorMatchCandidates_ReturnsExpectedOrder()
    {
        var candidates = DashboardInfoHelpers.GetMonitorMatchCandidates("DISPLAY\\DEL1234\\ABC_0");
        var keys = new List<string>();
        foreach (var candidate in candidates)
        {
            keys.Add(candidate.Key);
        }

        Assert.Equal(
            new[]
            {
                "DISPLAY\\DEL1234\\ABC_0",
                "DISPLAY\\DEL1234\\ABC"
            },
            keys);
    }

    [Fact]
    public void GetMonitorMatchCandidates_AppendsSuffixWhenMissing()
    {
        var candidates = DashboardInfoHelpers.GetMonitorMatchCandidates("DISPLAY\\DEL1234\\ABC");
        var keys = new List<string>();
        foreach (var candidate in candidates)
        {
            keys.Add(candidate.Key);
        }

        Assert.Equal(
            new[]
            {
                "DISPLAY\\DEL1234\\ABC",
                "DISPLAY\\DEL1234\\ABC_0"
            },
            keys);
    }

    [Fact]
    public void ParseEdid_ReturnsExpectedInfo()
    {
        var edid = BuildSampleEdid();
        var info = DashboardInfoHelpers.ParseEdid(edid);

        Assert.Equal("DEL", info.ManufacturerId);
        Assert.Equal("1234", info.ProductCodeHex);
        Assert.Equal("SER1234", info.SerialNumber);
        Assert.Equal("Test Monitor", info.MonitorName);
        Assert.Equal(0x12345678u, info.SerialNumberValue);
        Assert.Equal(52, info.HorizontalSizeCm);
        Assert.Equal(32, info.VerticalSizeCm);
        Assert.Equal(48, info.MinVerticalHz);
        Assert.Equal(75, info.MaxVerticalHz);
        Assert.Equal(30, info.MinHorizontalKHz);
        Assert.Equal(83, info.MaxHorizontalKHz);
        Assert.Equal(170, info.MaxPixelClockMHz);
    }

    [Fact]
    public void GetEdidMatchCandidates_ReturnsSerialAndProduct()
    {
        var candidates = DashboardInfoHelpers.GetEdidMatchCandidates("DEL", "1234", "SER1234", 0);
        var keys = new List<string>();
        foreach (var candidate in candidates)
        {
            keys.Add(candidate.Key);
        }

        Assert.Equal(
            new[]
            {
                "DEL|1234|SER1234",
                "DEL|1234"
            },
            keys);
    }

    [Fact]
    public void GetEdidMatchCandidates_IncludesSizeAndRange()
    {
        var info = new DashboardInfoHelpers.EdidInfo(
            "DEL",
            "1234",
            "",
            "",
            0,
            52,
            32,
            48,
            75,
            30,
            83,
            170);

        var candidates = DashboardInfoHelpers.GetEdidMatchCandidates(info);
        var keys = new List<string>();
        foreach (var candidate in candidates)
        {
            keys.Add(candidate.Key);
        }

        Assert.Contains("DEL|1234|SIZE:52X32", keys);
        Assert.Contains("DEL|1234|RANGE:V48-75H30-83P170", keys);
        Assert.Contains("DEL|1234", keys);
    }

    [Theory]
    [InlineData(5, "HDMI")]
    [InlineData(10, "DisplayPort External")]
    [InlineData(999, "Unknown")]
    public void MapVideoOutputTechnology_ReturnsExpected(int input, string expected)
    {
        var result = DashboardInfoHelpers.MapVideoOutputTechnology(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(3, 0, "HDD")]
    [InlineData(4, 17, "NVMe SSD")]
    [InlineData(4, 11, "SATA SSD")]
    [InlineData(2, 0, "Unknown")]
    public void ResolveDiskType_UsesMsftMap(int mediaType, int busType, string expected)
    {
        var map = new Dictionary<uint, (int mediaType, int busType)>
        {
            [1] = (mediaType, busType)
        };

        var result = DashboardInfoHelpers.ResolveDiskType(map, 1, "Generic", "SATA");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolveDiskType_UsesHeuristicsWhenMapMissing()
    {
        var map = new Dictionary<uint, (int mediaType, int busType)>();

        var nvme = DashboardInfoHelpers.ResolveDiskType(map, null, "Generic", "NVMe");
        var ssd = DashboardInfoHelpers.ResolveDiskType(map, null, "Samsung SSD 980", "SATA");
        var hdd = DashboardInfoHelpers.ResolveDiskType(map, null, "ST1000DM", "SATA");

        Assert.Equal("NVMe SSD", nvme);
        Assert.Equal("SSD", ssd);
        Assert.Equal("HDD", hdd);
    }

    private static byte[] BuildSampleEdid()
    {
        var edid = new byte[128];
        edid[8] = 0x10;
        edid[9] = 0xAC;
        edid[10] = 0x34;
        edid[11] = 0x12;
        edid[12] = 0x78;
        edid[13] = 0x56;
        edid[14] = 0x34;
        edid[15] = 0x12;
        edid[21] = 52;
        edid[22] = 32;

        WriteDescriptor(edid, 54, 0xFC, "Test Monitor");
        WriteDescriptor(edid, 72, 0xFF, "SER1234");
        WriteRangeDescriptor(edid, 90, minV: 48, maxV: 75, minH: 30, maxH: 83, maxPixelClock10MHz: 17);

        return edid;
    }

    private static void WriteDescriptor(byte[] edid, int offset, byte type, string text)
    {
        edid[offset] = 0x00;
        edid[offset + 1] = 0x00;
        edid[offset + 2] = 0x00;
        edid[offset + 3] = type;
        edid[offset + 4] = 0x00;

        var bytes = Encoding.ASCII.GetBytes(text);
        for (var i = 0; i < 13; i++)
        {
            edid[offset + 5 + i] = i < bytes.Length ? bytes[i] : (byte)0x20;
        }
    }

    private static void WriteRangeDescriptor(
        byte[] edid,
        int offset,
        byte minV,
        byte maxV,
        byte minH,
        byte maxH,
        byte maxPixelClock10MHz)
    {
        edid[offset] = 0x00;
        edid[offset + 1] = 0x00;
        edid[offset + 2] = 0x00;
        edid[offset + 3] = 0xFD;
        edid[offset + 4] = 0x00;
        edid[offset + 5] = minV;
        edid[offset + 6] = maxV;
        edid[offset + 7] = minH;
        edid[offset + 8] = maxH;
        edid[offset + 9] = maxPixelClock10MHz;
    }
}
