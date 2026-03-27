using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.App.HardwareDb;
using RegProbe.App.HardwareDb.Models;

public sealed class HardwareMatchingTests
{
    private static void EnsureKnowledgeDbLoaded()
    {
        HardwareKnowledgeDbService.Instance.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    [Fact]
    public void Normalize_RemovesCommonJunkTokens()
    {
        var normalized = HardwareNameNormalizer.Normalize("Intel(R) Core(TM) i7-13700K CPU");

        Assert.Equal("intel core i7 13700k", normalized);
    }

    [Fact]
    public void Normalize_CollapsesWifiTokens()
    {
        var normalized = HardwareNameNormalizer.Normalize("Intel(R) Wi-Fi 6E AX210 160MHz");

        Assert.Equal("intel wifi 6e ax210 160mhz", normalized);
    }

    [Fact]
    public void Match_PrefersMostSpecificPartialCandidate()
    {
        var generic = new CpuModel { Id = "generic", NormalizedName = "intel core i7" };
        var specific = new CpuModel { Id = "specific", NormalizedName = "intel core i7-13700k" };

        var index = new Dictionary<string, CpuModel>(StringComparer.OrdinalIgnoreCase)
        {
            [HardwareNameNormalizer.Normalize(generic.NormalizedName)] = generic,
            [HardwareNameNormalizer.Normalize(specific.NormalizedName)] = specific
        };

        var alias = new Dictionary<string, CpuModel>(StringComparer.OrdinalIgnoreCase);
        var result = HardwareMatcher.Match("Intel Core i7 13700K", index, alias);

        Assert.NotNull(result);
        Assert.Equal("specific", result!.Id);
    }

    [Fact]
    public void MatchDetailed_ReportsExactAliasMatch()
    {
        var model = new GpuModel { Id = "rtx4060", NormalizedName = "nvidia geforce rtx 4060" };
        var index = new Dictionary<string, GpuModel>(StringComparer.OrdinalIgnoreCase);
        var alias = new Dictionary<string, GpuModel>(StringComparer.OrdinalIgnoreCase)
        {
            [HardwareNameNormalizer.Normalize("rtx 4060")] = model
        };

        var result = HardwareMatcher.MatchDetailed("RTX 4060", index, alias);

        Assert.True(result.HasMatch);
        Assert.Equal("rtx4060", result.Model!.Id);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
    }

    [Fact]
    public void MatchDetailed_ReportsPartialNameMatch()
    {
        var model = new CpuModel { Id = "specific", NormalizedName = "intel core i7-13700k" };
        var index = new Dictionary<string, CpuModel>(StringComparer.OrdinalIgnoreCase)
        {
            [HardwareNameNormalizer.Normalize(model.NormalizedName)] = model
        };
        var alias = new Dictionary<string, CpuModel>(StringComparer.OrdinalIgnoreCase);

        var result = HardwareMatcher.MatchDetailed("Intel Core i7 13700K OEM", index, alias);

        Assert.True(result.HasMatch);
        Assert.Equal("specific", result.Model!.Id);
        Assert.Equal(HardwareMatchKind.PartialName, result.MatchKind);
    }

    [Fact]
    public void MatchDetailed_ReturnsNoneForAmbiguousPartialMatches()
    {
        var first = new StorageControllerModel { Id = "first", NormalizedName = "samsung 990 pro 1tb" };
        var second = new StorageControllerModel { Id = "second", NormalizedName = "samsung 990 pro 2tb" };
        var index = new Dictionary<string, StorageControllerModel>(StringComparer.OrdinalIgnoreCase)
        {
            [HardwareNameNormalizer.Normalize(first.NormalizedName)] = first,
            [HardwareNameNormalizer.Normalize(second.NormalizedName)] = second
        };

        var result = HardwareMatcher.MatchDetailed("Samsung 990 PRO", index, new Dictionary<string, StorageControllerModel>(StringComparer.OrdinalIgnoreCase));

        Assert.False(result.HasMatch);
        Assert.Equal(HardwareMatchKind.None, result.MatchKind);
    }

    [Fact]
    public void Match_ReturnsNullForOverlyGenericToken()
    {
        var intel = new CpuModel { Id = "intel", NormalizedName = "intel core i9-14900k" };
        var amd = new CpuModel { Id = "amd", NormalizedName = "amd ryzen 7 7800x3d" };

        var index = new Dictionary<string, CpuModel>(StringComparer.OrdinalIgnoreCase)
        {
            [HardwareNameNormalizer.Normalize(intel.NormalizedName)] = intel,
            [HardwareNameNormalizer.Normalize(amd.NormalizedName)] = amd
        };

        var alias = new Dictionary<string, CpuModel>(StringComparer.OrdinalIgnoreCase);
        var result = HardwareMatcher.Match("Intel", index, alias);

        Assert.Null(result);
    }

    [Fact]
    public void Match_UsesAliasWhenDirectNameMissing()
    {
        var model = new GpuModel { Id = "rtx4060", NormalizedName = "nvidia geforce rtx 4060" };
        var index = new Dictionary<string, GpuModel>(StringComparer.OrdinalIgnoreCase);
        var alias = new Dictionary<string, GpuModel>(StringComparer.OrdinalIgnoreCase)
        {
            [HardwareNameNormalizer.Normalize("rtx 4060")] = model
        };

        var result = HardwareMatcher.Match("GeForce RTX 4060", index, alias);

        Assert.NotNull(result);
        Assert.Equal("rtx4060", result!.Id);
    }

    [Fact]
    public void BuildDisplayLookupSeed_ExpandsManufacturerCodes()
    {
        var seed = HardwareIconService.BuildDisplayLookupSeed("GSM");

        Assert.Contains("LG Electronics", seed, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildGpuLookupSeed_ExpandsPciVendorId()
    {
        var seed = HardwareIconService.BuildGpuLookupSeed(@"PCI\VEN_10DE&DEV_2786&SUBSYS_196E10DE");

        Assert.Contains("NVIDIA", seed, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildMotherboardLookupSeed_ExpandsOemManufacturer()
    {
        var seed = HardwareIconService.BuildMotherboardLookupSeed("Micro-Star International Co., Ltd.", "MEG Z790 ACE");

        Assert.Contains("MSI", seed, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildMotherboardLookupSeed_SkipsFirmwareVendorNoise()
    {
        var seed = HardwareIconService.BuildMotherboardLookupSeed("ASRock", "B550 Pro4", "American Megatrends International, LLC.", "B550");

        Assert.DoesNotContain("American Megatrends", seed, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AMI", seed, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MatchDisplayDetailed_UsesGeneratedDellManufacturerCodeAliasFromDatabase()
    {
        await HardwareKnowledgeDbService.Instance.InitializeAsync(CancellationToken.None);

        var result = HardwareKnowledgeDbService.Instance.MatchDisplayDetailed("DEL AW3423DWF");

        Assert.True(result.HasMatch);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
        Assert.NotNull(result.Model);
        Assert.False(string.IsNullOrWhiteSpace(result.Model!.Id));
    }

    [Fact]
    public async Task MatchDisplayDetailed_UsesGeneratedLgManufacturerCodeAliasFromDatabase()
    {
        await HardwareKnowledgeDbService.Instance.InitializeAsync(CancellationToken.None);

        var result = HardwareKnowledgeDbService.Instance.MatchDisplayDetailed("GSM 27GP850");

        Assert.True(result.HasMatch);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
        Assert.NotNull(result.Model);
        Assert.False(string.IsNullOrWhiteSpace(result.Model!.Id));
    }

    [Fact]
    public async Task MatchGpuDetailed_UsesGeneratedGigabyteBoardCodeAliasFromDatabase()
    {
        await HardwareKnowledgeDbService.Instance.InitializeAsync(CancellationToken.None);

        var result = HardwareKnowledgeDbService.Instance.MatchGpuDetailed("GV-N4090GAMING OC-24GD");

        Assert.True(result.HasMatch);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
        Assert.NotNull(result.Model);
        Assert.False(string.IsNullOrWhiteSpace(result.Model!.Id));
    }

    [Fact]
    public async Task MatchGpuDetailed_UsesGeneratedPnyBoardCodeAliasFromDatabase()
    {
        await HardwareKnowledgeDbService.Instance.InitializeAsync(CancellationToken.None);

        var result = HardwareKnowledgeDbService.Instance.MatchGpuDetailed("VCG507012TFXXPB1-O");

        Assert.True(result.HasMatch);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
        Assert.NotNull(result.Model);
        Assert.False(string.IsNullOrWhiteSpace(result.Model!.Id));
    }

    [Fact]
    public async Task MatchGpuDetailed_UsesGeneratedAmdAibAliasFromDatabase()
    {
        await HardwareKnowledgeDbService.Instance.InitializeAsync(CancellationToken.None);

        var result = HardwareKnowledgeDbService.Instance.MatchGpuDetailed("RX7900XTX NITRO+");

        Assert.True(result.HasMatch);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
        Assert.NotNull(result.Model);
        Assert.False(string.IsNullOrWhiteSpace(result.Model!.Id));
    }

    [Fact]
    public async Task MatchMotherboardDetailed_UsesGeneratedAsusOemAliasFromDatabase()
    {
        await HardwareKnowledgeDbService.Instance.InitializeAsync(CancellationToken.None);

        var result = HardwareKnowledgeDbService.Instance.MatchMotherboardDetailed(
            "ASUSTeK COMPUTER INC. ROG STRIX Z790-E GAMING WIFI");

        Assert.True(result.HasMatch);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
        Assert.NotNull(result.Model);
        Assert.False(string.IsNullOrWhiteSpace(result.Model!.Id));
    }

    [Fact]
    public async Task MatchMotherboardDetailed_UsesGeneratedGigabyteOemAliasFromDatabase()
    {
        await HardwareKnowledgeDbService.Instance.InitializeAsync(CancellationToken.None);

        var result = HardwareKnowledgeDbService.Instance.MatchMotherboardDetailed(
            "Gigabyte Technology Co., Ltd. B650 AORUS ELITE AX");

        Assert.True(result.HasMatch);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
        Assert.NotNull(result.Model);
        Assert.False(string.IsNullOrWhiteSpace(result.Model!.Id));
    }

    [Fact]
    public async Task MatchStorageDetailed_UsesGeneratedSkuAliasesFromDatabase()
    {
        await HardwareKnowledgeDbService.Instance.InitializeAsync(CancellationToken.None);

        var result = HardwareKnowledgeDbService.Instance.MatchStorageDetailed("SKC3000D2048G");

        Assert.True(result.HasMatch);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
        Assert.NotNull(result.Model);
        Assert.False(string.IsNullOrWhiteSpace(result.Model!.Id));
    }
}
