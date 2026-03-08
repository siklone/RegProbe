using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.App.HardwareDb;
using WindowsOptimizer.App.HardwareDb.Models;

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
    public void ResolveResult_UsesProvidedDatabaseModelBeforeRuleMap()
    {
        var matched = new StorageControllerModel
        {
            Id = "990pro",
            NormalizedName = "samsung 990 pro",
            IconKey = "storage_samsung_990pro"
        };

        var result = HardwareIconService.ResolveResult(HardwareType.Storage, "Samsung SSD 990 PRO 2TB", matched);

        Assert.Equal("storage_samsung_990pro", result.IconKey);
        Assert.Equal(HardwareIconResolutionSource.DatabaseModel, result.Source);
        Assert.Equal(HardwareMatchKind.ProvidedModel, result.MatchKind);
    }

    [Fact]
    public void ResolveResult_RefinesGenericDatabaseIconWithRuleMap()
    {
        var matched = new GpuModel
        {
            Id = "rtx4090",
            NormalizedName = "nvidia geforce rtx 4090",
            IconKey = "gpu_rtx"
        };

        var result = HardwareIconService.ResolveResult(HardwareType.Gpu, "NVIDIA GeForce RTX 4090", matched);

        Assert.Equal("gpu_rtx40", result.IconKey);
        Assert.Equal(HardwareIconResolutionSource.RuleMap, result.Source);
        Assert.Equal(HardwareMatchKind.ProvidedModel, result.MatchKind);
    }

    [Fact]
    public void ResolveResult_UsesRuleMapWhenNoDatabaseModelProvided()
    {
        EnsureKnowledgeDbLoaded();
        var result = HardwareIconService.ResolveResult(HardwareType.Storage, "WDS100T1X0E-00AFY0");

        Assert.Equal("storage_wd_black", result.IconKey);
        Assert.Equal(HardwareIconResolutionSource.RuleMap, result.Source);
    }

    [Fact]
    public void ResolveResult_UsesDatabaseModelForWdBlackSn850x()
    {
        EnsureKnowledgeDbLoaded();
        var result = HardwareIconService.ResolveResult(HardwareType.Storage, "WD_BLACK SN850X 2000GB");

        Assert.Equal("storage_wd_black", result.IconKey);
        Assert.Equal(HardwareIconResolutionSource.DatabaseModel, result.Source);
        Assert.Equal(HardwareMatchKind.PartialAlias, result.MatchKind);
    }

    [Fact]
    public void ResolveResult_UsesDatabaseModelForSamsungSkuCode()
    {
        EnsureKnowledgeDbLoaded();
        var result = HardwareIconService.ResolveResult(HardwareType.Storage, "MZ-V9P2T0B");

        Assert.Equal("storage_samsung_990pro", result.IconKey);
        Assert.Equal(HardwareIconResolutionSource.DatabaseModel, result.Source);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
    }

    [Fact]
    public void ResolveResult_UsesDatabaseModelForCrucialSkuCode()
    {
        EnsureKnowledgeDbLoaded();
        var result = HardwareIconService.ResolveResult(HardwareType.Storage, "CT1000P5PSSD8");

        Assert.Equal("storage_crucial_p5", result.IconKey);
        Assert.Equal(HardwareIconResolutionSource.DatabaseModel, result.Source);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
    }

    [Fact]
    public void BuildDisplayLookupSeed_ExpandsManufacturerCodes()
    {
        var seed = HardwareIconService.BuildDisplayLookupSeed("GSM");
        var result = HardwareIconService.ResolveResult(HardwareType.Display, seed);

        Assert.Contains("LG Electronics", seed, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("display_lg", result.IconKey);
        Assert.Equal(HardwareIconResolutionSource.RuleMap, result.Source);
    }

    [Fact]
    public void ResolveResult_UsesDisplayRuleForAlienwareModelCode()
    {
        EnsureKnowledgeDbLoaded();
        var result = HardwareIconService.ResolveResult(HardwareType.Display, "AW3423DWF");

        Assert.Equal("display_dell_alienware", result.IconKey);
        Assert.Equal(HardwareIconResolutionSource.DatabaseModel, result.Source);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
    }

    [Fact]
    public void ResolveResult_UsesDisplayRuleForUltraGearModelCode()
    {
        EnsureKnowledgeDbLoaded();
        var result = HardwareIconService.ResolveResult(HardwareType.Display, "27GP850");

        Assert.Equal("display_lg_ultragear", result.IconKey);
        Assert.Equal(HardwareIconResolutionSource.DatabaseModel, result.Source);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
    }

    [Fact]
    public async Task MatchDisplayDetailed_UsesGeneratedDellManufacturerCodeAliasFromDatabase()
    {
        await HardwareKnowledgeDbService.Instance.InitializeAsync(CancellationToken.None);

        var result = HardwareKnowledgeDbService.Instance.MatchDisplayDetailed("DEL AW3423DWF");

        Assert.True(result.HasMatch);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
        Assert.Equal("display_dell_alienware", result.Model!.IconKey);
    }

    [Fact]
    public async Task MatchDisplayDetailed_UsesGeneratedLgManufacturerCodeAliasFromDatabase()
    {
        await HardwareKnowledgeDbService.Instance.InitializeAsync(CancellationToken.None);

        var result = HardwareKnowledgeDbService.Instance.MatchDisplayDetailed("GSM 27GP850");

        Assert.True(result.HasMatch);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
        Assert.Equal("display_lg_ultragear", result.Model!.IconKey);
    }

    [Fact]
    public void ResolveResult_UsesDisplayDatabaseForCompositeLookupSeed()
    {
        EnsureKnowledgeDbLoaded();
        var seed = HardwareIconService.BuildDisplayLookupSeed("AW3423DWF", "DEL");
        var result = HardwareIconService.ResolveResult(HardwareType.Display, seed);

        Assert.Equal("display_dell_alienware", result.IconKey);
        Assert.Equal(HardwareIconResolutionSource.DatabaseModel, result.Source);
        Assert.Equal(HardwareMatchKind.PartialAlias, result.MatchKind);
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
    public void ResolveResult_UsesGpuRuleForGigabyteBoardCode()
    {
        EnsureKnowledgeDbLoaded();
        var result = HardwareIconService.ResolveResult(HardwareType.Gpu, "GV-N4090GAMING OC-24GD");

        Assert.Equal("gpu_rtx40", result.IconKey);
        Assert.Equal(HardwareIconResolutionSource.DatabaseModel, result.Source);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
    }

    [Fact]
    public void ResolveResult_UsesGpuRuleForPnyBoardCode()
    {
        EnsureKnowledgeDbLoaded();
        var result = HardwareIconService.ResolveResult(HardwareType.Gpu, "VCG507012TFXXPB1-O");

        Assert.Equal("gpu_rtx50", result.IconKey);
        Assert.Equal(HardwareIconResolutionSource.DatabaseModel, result.Source);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
    }

    [Fact]
    public void ResolveResult_UsesGpuRuleForCompactAmdModel()
    {
        EnsureKnowledgeDbLoaded();
        var result = HardwareIconService.ResolveResult(HardwareType.Gpu, "RX7900XTX NITRO+");

        Assert.Equal("gpu_rx7000", result.IconKey);
        Assert.Equal(HardwareIconResolutionSource.DatabaseModel, result.Source);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
    }

    [Fact]
    public async Task MatchGpuDetailed_UsesGeneratedGigabyteBoardCodeAliasFromDatabase()
    {
        await HardwareKnowledgeDbService.Instance.InitializeAsync(CancellationToken.None);

        var result = HardwareKnowledgeDbService.Instance.MatchGpuDetailed("GV-N4090GAMING OC-24GD");

        Assert.True(result.HasMatch);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
        Assert.Equal("gpu_rtx40", result.Model!.IconKey);
    }

    [Fact]
    public async Task MatchGpuDetailed_UsesGeneratedPnyBoardCodeAliasFromDatabase()
    {
        await HardwareKnowledgeDbService.Instance.InitializeAsync(CancellationToken.None);

        var result = HardwareKnowledgeDbService.Instance.MatchGpuDetailed("VCG507012TFXXPB1-O");

        Assert.True(result.HasMatch);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
        Assert.Equal("gpu_rtx50", result.Model!.IconKey);
    }

    [Fact]
    public async Task MatchGpuDetailed_UsesGeneratedAmdAibAliasFromDatabase()
    {
        await HardwareKnowledgeDbService.Instance.InitializeAsync(CancellationToken.None);

        var result = HardwareKnowledgeDbService.Instance.MatchGpuDetailed("RX7900XTX NITRO+");

        Assert.True(result.HasMatch);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
        Assert.Equal("gpu_rx7000", result.Model!.IconKey);
    }

    [Fact]
    public void ResolveResult_UsesMotherboardRuleForMsiMegBoard()
    {
        EnsureKnowledgeDbLoaded();
        var seed = HardwareIconService.BuildMotherboardLookupSeed(
            "Micro-Star International Co., Ltd.",
            "MEG Z790 ACE");
        var result = HardwareIconService.ResolveResult(HardwareType.Motherboard, seed);

        Assert.Equal("mb_msi_meg", result.IconKey);
        Assert.Equal(HardwareIconResolutionSource.DatabaseModel, result.Source);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
    }

    [Fact]
    public void ResolveResult_UsesMotherboardRuleForGigabyteAorusBoard()
    {
        EnsureKnowledgeDbLoaded();
        var seed = HardwareIconService.BuildMotherboardLookupSeed(
            "Gigabyte Technology Co., Ltd.",
            "B650 AORUS ELITE AX");
        var result = HardwareIconService.ResolveResult(HardwareType.Motherboard, seed);

        Assert.Equal("mb_gigabyte_aorus", result.IconKey);
        Assert.Equal(HardwareIconResolutionSource.DatabaseModel, result.Source);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
    }

    [Fact]
    public void ResolveResult_UsesMotherboardRuleForAsusStrixBoard()
    {
        EnsureKnowledgeDbLoaded();
        var seed = HardwareIconService.BuildMotherboardLookupSeed(
            "ASUSTeK COMPUTER INC.",
            "STRIX X670E-E GAMING WIFI");
        var result = HardwareIconService.ResolveResult(HardwareType.Motherboard, seed);

        Assert.Equal("mb_asus_rog", result.IconKey);
        Assert.Equal(HardwareIconResolutionSource.RuleMap, result.Source);
    }

    [Fact]
    public async Task MatchMotherboardDetailed_UsesGeneratedAsusOemAliasFromDatabase()
    {
        await HardwareKnowledgeDbService.Instance.InitializeAsync(CancellationToken.None);

        var result = HardwareKnowledgeDbService.Instance.MatchMotherboardDetailed(
            "ASUSTeK COMPUTER INC. ROG STRIX Z790-E GAMING WIFI");

        Assert.True(result.HasMatch);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
        Assert.Equal("mb_asus_rog", result.Model!.IconKey);
    }

    [Fact]
    public async Task MatchMotherboardDetailed_UsesGeneratedGigabyteOemAliasFromDatabase()
    {
        await HardwareKnowledgeDbService.Instance.InitializeAsync(CancellationToken.None);

        var result = HardwareKnowledgeDbService.Instance.MatchMotherboardDetailed(
            "Gigabyte Technology Co., Ltd. B650 AORUS ELITE AX");

        Assert.True(result.HasMatch);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
        Assert.Equal("mb_gigabyte_aorus", result.Model!.IconKey);
    }

    [Fact]
    public async Task MatchStorageDetailed_UsesGeneratedSkuAliasesFromDatabase()
    {
        await HardwareKnowledgeDbService.Instance.InitializeAsync(CancellationToken.None);

        var result = HardwareKnowledgeDbService.Instance.MatchStorageDetailed("SKC3000D2048G");

        Assert.True(result.HasMatch);
        Assert.Equal(HardwareMatchKind.ExactAlias, result.MatchKind);
        Assert.Equal("storage_kingston_kc3000", result.Model!.IconKey);
    }

    [Fact]
    public void ResolveByIconKeyResult_FallsBackWhenExplicitKeyIsUnknown()
    {
        var result = HardwareIconService.ResolveByIconKeyResult(HardwareType.Network, "network_missing_key", "Intel Ethernet");

        Assert.Equal("network_default", result.IconKey);
        Assert.Equal(HardwareIconResolutionSource.Fallback, result.Source);
        Assert.True(result.UsesFallback);
    }

    [Fact]
    public void ResolveResult_UsesWifiFamilyRuleForNetworkNames()
    {
        EnsureKnowledgeDbLoaded();
        var result = HardwareIconService.ResolveResult(HardwareType.Network, "Qualcomm FastConnect Wi-Fi 7");

        Assert.Equal("network_wifi7", result.IconKey);
        Assert.Equal(HardwareIconResolutionSource.RuleMap, result.Source);
    }

    [Fact]
    public void ResolveResult_UsesOneGigRuleForGenericGigabitName()
    {
        EnsureKnowledgeDbLoaded();
        var result = HardwareIconService.ResolveResult(HardwareType.Network, "Integrated 1GbE Ethernet");

        Assert.Equal("network_1gbe", result.IconKey);
        Assert.Equal(HardwareIconResolutionSource.RuleMap, result.Source);
    }
}
