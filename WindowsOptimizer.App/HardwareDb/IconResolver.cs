using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WindowsOptimizer.App.HardwareDb;

public enum HardwareType
{
    Os,
    Cpu,
    Gpu,
    Motherboard,
    Memory,
    Storage,
    Display,
    Network,
    Usb,
    Audio
}

public static class IconResolver
{
    private static readonly string PackBase = "pack://application:,,,/Assets/Icons/";
    
    private static readonly ConcurrentDictionary<string, ImageSource> IconCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, string> KeyCache = new(StringComparer.OrdinalIgnoreCase);
    
    private static readonly Dictionary<HardwareType, string> DefaultIcons = new()
    {
        [HardwareType.Os] = "os/windows10",
        [HardwareType.Cpu] = "cpu_default",
        [HardwareType.Gpu] = "gpu_default",
        [HardwareType.Motherboard] = "motherboard_default",
        [HardwareType.Memory] = "memory_default",
        [HardwareType.Storage] = "storage_default",
        [HardwareType.Display] = "display_default",
        [HardwareType.Network] = "network_default",
        [HardwareType.Usb] = "usb_default",
        [HardwareType.Audio] = "cpu_default"
    };

    private static readonly (Regex Pattern, string IconKey)[] CpuPatterns =
    {
        (Compile(@"intel.*core\s*ultra"), "cpu_intel_ultra"),
        (Compile(@"intel.*\bi9\b"), "cpu_intel_i9"),
        (Compile(@"intel.*\bi7\b"), "cpu_intel_i7"),
        (Compile(@"intel.*\bi5\b"), "cpu_intel_i5"),
        (Compile(@"intel.*\bi3\b"), "cpu_intel_i3"),
        (Compile(@"xeon\s*w"), "cpu_xeon_w"),
        (Compile(@"xeon"), "cpu_xeon"),
        (Compile(@"pentium"), "cpu_intel_pentium"),
        (Compile(@"celeron"), "cpu_intel_celeron"),
        (Compile(@"intel"), "cpu_intel"),
        (Compile(@"threadripper"), "cpu_amd_threadripper"),
        (Compile(@"epyc"), "cpu_amd_epyc"),
        (Compile(@"ryzen\s*9"), "cpu_amd_ryzen9"),
        (Compile(@"ryzen\s*7"), "cpu_amd_ryzen7"),
        (Compile(@"ryzen\s*5"), "cpu_amd_ryzen5"),
        (Compile(@"ryzen\s*3"), "cpu_amd_ryzen3"),
        (Compile(@"ryzen"), "cpu_amd_ryzen"),
        (Compile(@"athlon"), "cpu_amd_athlon"),
        (Compile(@"phenom"), "cpu_amd_phenom"),
        (Compile(@"fx[\s-]?\d"), "cpu_amd_fx"),
        (Compile(@"amd"), "cpu_amd")
    };

    private static readonly (Regex Pattern, string IconKey)[] GpuPatterns =
    {
        (Compile(@"rtx\s*50"), "gpu_nvidia_rtx50"),
        (Compile(@"rtx\s*40"), "gpu_nvidia_rtx40"),
        (Compile(@"rtx\s*30"), "gpu_nvidia_rtx30"),
        (Compile(@"rtx\s*20"), "gpu_nvidia_rtx20"),
        (Compile(@"rtx"), "gpu_nvidia_rtx"),
        (Compile(@"gtx\s*16"), "gpu_nvidia_gtx16"),
        (Compile(@"gtx\s*10"), "gpu_nvidia_gtx10"),
        (Compile(@"gtx"), "gpu_nvidia_gtx"),
        (Compile(@"quadro"), "gpu_nvidia_quadro"),
        (Compile(@"nvidia"), "gpu_nvidia"),
        (Compile(@"rx\s*90"), "gpu_amd_rx9000"),
        (Compile(@"rx\s*70"), "gpu_amd_rx7000"),
        (Compile(@"rx\s*60"), "gpu_amd_rx6000"),
        (Compile(@"rx\s*50"), "gpu_amd_rx5000"),
        (Compile(@"radeon\s*pro"), "gpu_amd_pro"),
        (Compile(@"radeon|rx\s*\d"), "gpu_amd_rx"),
        (Compile(@"amd.*gpu|amd.*graphics"), "gpu_amd"),
        (Compile(@"arc\s*a?\d"), "gpu_intel_arc"),
        (Compile(@"intel.*graphics|uhd\s*\d"), "gpu_intel_integrated")
    };

    private static readonly (Regex Pattern, string IconKey)[] MotherboardPatterns =
    {
        (Compile(@"\brog\b"), "mb_asus_rog"),
        (Compile(@"\btuf\b"), "mb_asus_tuf"),
        (Compile(@"\bprime\b"), "mb_asus_prime"),
        (Compile(@"asus"), "mb_asus"),
        (Compile(@"\bmeg\b"), "mb_msi_meg"),
        (Compile(@"\bmpg\b"), "mb_msi_mpg"),
        (Compile(@"\bmag\b"), "mb_msi_mag"),
        (Compile(@"msi"), "mb_msi"),
        (Compile(@"aorus"), "mb_gigabyte_aorus"),
        (Compile(@"gigabyte"), "mb_gigabyte"),
        (Compile(@"taichi"), "mb_asrock_taichi"),
        (Compile(@"phantom\s*gaming"), "mb_asrock_phantom"),
        (Compile(@"asrock"), "mb_asrock"),
        (Compile(@"biostar"), "mb_biostar"),
        (Compile(@"supermicro"), "mb_supermicro"),
        (Compile(@"evga"), "mb_evga")
    };

    private static readonly (Regex Pattern, string IconKey)[] MemoryPatterns =
    {
        (Compile(@"dominator"), "memory_corsair_dominator"),
        (Compile(@"vengeance"), "memory_corsair_vengeance"),
        (Compile(@"corsair"), "memory_corsair"),
        (Compile(@"fury\s*(beast|renegade|impact)"), "memory_kingston_fury"),
        (Compile(@"kingston"), "memory_kingston"),
        (Compile(@"trident\s*z"), "memory_gskill_trident"),
        (Compile(@"ripjaws"), "memory_gskill_ripjaws"),
        (Compile(@"g\.?skill"), "memory_gskill"),
        (Compile(@"ballistix"), "memory_crucial_ballistix"),
        (Compile(@"crucial"), "memory_crucial"),
        (Compile(@"samsung"), "memory_samsung"),
        (Compile(@"hynix"), "memory_hynix"),
        (Compile(@"micron"), "memory_micron"),
        (Compile(@"ddr5"), "memory_ddr5"),
        (Compile(@"ddr4"), "memory_ddr4")
    };

    private static readonly (Regex Pattern, string IconKey)[] StoragePatterns =
    {
        (Compile(@"990\s*pro"), "storage_samsung_990pro"),
        (Compile(@"980\s*pro"), "storage_samsung_980pro"),
        (Compile(@"970\s*evo"), "storage_samsung_970evo"),
        (Compile(@"970"), "storage_samsung_970"),
        (Compile(@"samsung"), "storage_samsung"),
        (Compile(@"black\s*sn850"), "storage_wd_black"),
        (Compile(@"black\s*sn770"), "storage_wd_black"),
        (Compile(@"wd\s*black"), "storage_wd_black"),
        (Compile(@"wd\s*blue"), "storage_wd_blue"),
        (Compile(@"\bwd\b|western\s*digital"), "storage_wd"),
        (Compile(@"firecuda"), "storage_seagate_firecuda"),
        (Compile(@"barracuda"), "storage_seagate_barracuda"),
        (Compile(@"seagate"), "storage_seagate"),
        (Compile(@"p5\s*plus"), "storage_crucial_p5"),
        (Compile(@"crucial"), "storage_crucial"),
        (Compile(@"kc3000"), "storage_kingston_kc3000"),
        (Compile(@"kingston"), "storage_kingston"),
        (Compile(@"mp600"), "storage_corsair_mp600"),
        (Compile(@"kioxia|exceria"), "storage_kioxia"),
        (Compile(@"nvme|pcie\s*\d"), "storage_nvme"),
        (Compile(@"ssd"), "storage_ssd"),
        (Compile(@"hdd|sata"), "storage_hdd")
    };

    private static readonly (Regex Pattern, string IconKey)[] NetworkPatterns =
    {
        (Compile(@"wireguard|wintun"), "network_wireguard"),
        (Compile(@"i226"), "network_intel_i226"),
        (Compile(@"i225"), "network_intel_i225"),
        (Compile(@"ax211|ax210"), "network_intel_wifi6e"),
        (Compile(@"ax200|ax201"), "network_intel_wifi6"),
        (Compile(@"killer\s*e3"), "network_killer_e3000"),
        (Compile(@"killer\s*e2"), "network_killer_e2600"),
        (Compile(@"killer"), "network_killer"),
        (Compile(@"intel.*ethernet|intel.*wifi"), "network_intel"),
        (Compile(@"rtl8125"), "network_realtek_8125"),
        (Compile(@"rtl8111"), "network_realtek_8111"),
        (Compile(@"realtek"), "network_realtek"),
        (Compile(@"bcm|broadcom"), "network_broadcom"),
        (Compile(@"mt79|mediatek"), "network_mediatek"),
        (Compile(@"wifi\s*7|wi-fi\s*7"), "network_wifi7"),
        (Compile(@"wifi\s*6e|wi-fi\s*6e"), "network_wifi6e"),
        (Compile(@"wifi\s*6|wi-fi\s*6"), "network_wifi6"),
        (Compile(@"2\.5gbe|2\.5g"), "network_2_5gbe"),
        (Compile(@"1gbe|gigabit"), "network_1gbe")
    };

    private static readonly (Regex Pattern, string IconKey)[] UsbPatterns =
    {
        (Compile(@"asm3142"), "usb_asmedia_3142"),
        (Compile(@"asmedia"), "usb_asmedia"),
        (Compile(@"renesas"), "usb_renesas"),
        (Compile(@"\bvia\b"), "usb_via"),
        (Compile(@"usb4|usb\s*4"), "usb_usb4"),
        (Compile(@"usb\s*3\.?2"), "usb_32"),
        (Compile(@"usb\s*3\.?1"), "usb_31"),
        (Compile(@"usb\s*3\.?0"), "usb_30"),
        (Compile(@"usb\s*2\.?0"), "usb_20"),
        (Compile(@"intel.*usb"), "usb_intel"),
        (Compile(@"amd.*usb"), "usb_amd")
    };

    private static readonly (Regex Pattern, string IconKey)[] DisplayPatterns =
    {
        (Compile(@"e27fvc[\s-]*e|excalibur|wam\s*2700"), "display_excalibur"),
        (Compile(@"rog\s*swift"), "display_asus_rog"),
        (Compile(@"proart"), "display_asus_proart"),
        (Compile(@"asus"), "display_asus"),
        (Compile(@"predator"), "display_acer_predator"),
        (Compile(@"acer"), "display_acer"),
        (Compile(@"ultragear"), "display_lg_ultragear"),
        (Compile(@"\blg\b"), "display_lg"),
        (Compile(@"odyssey"), "display_samsung_odyssey"),
        (Compile(@"samsung"), "display_samsung"),
        (Compile(@"alienware"), "display_dell_alienware"),
        (Compile(@"ultrasharp"), "display_dell_ultrasharp"),
        (Compile(@"dell"), "display_dell"),
        (Compile(@"benq"), "display_benq"),
        (Compile(@"viewsonic"), "display_viewsonic"),
        (Compile(@"aoc"), "display_aoc")
    };

    private static Regex Compile(string pattern) => new(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static string ResolveIconKey(HardwareType type, string? vendor, string? model)
    {
        return HardwareIconService.ResolveIconKey(type, $"{vendor} {model}".Trim());
    }

    public static ImageSource Resolve(HardwareType type, string? vendor, string? model)
    {
        return HardwareIconService.Resolve(type, $"{vendor} {model}".Trim());
    }

    public static ImageSource ResolveByKey(string? iconKey, HardwareType type)
    {
        if (string.IsNullOrWhiteSpace(iconKey))
        {
            return HardwareIconService.Resolve(type, null);
        }
        return HardwareIconService.ResolveByIconKey(type, iconKey);
    }

    public static string GetDefaultKey(HardwareType type) => DefaultIcons.GetValueOrDefault(type, "cpu_default");

    private static string ResolveOsIconKey(string? vendor, string? model)
    {
        var search = Normalize($"{vendor} {model}");
        
        if (search.Contains("windows 11", StringComparison.OrdinalIgnoreCase))
        {
            return "os/windows11";
        }

        if (search.Contains("windows 10", StringComparison.OrdinalIgnoreCase))
        {
            return "os/windows10";
        }

        if (int.TryParse(ExtractBuildNumber(search), out var build) && build >= 22000)
        {
            return "os/windows11";
        }

        return "os/windows10";
    }

    private static string ExtractBuildNumber(string input)
    {
        var match = Regex.Match(input, @"build\s*(\d+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : "0";
    }

    private static string MatchPattern((Regex Pattern, string IconKey)[] patterns, string search, string fallback)
    {
        foreach (var (pattern, iconKey) in patterns)
        {
            if (pattern.IsMatch(search))
            {
                return iconKey;
            }
        }

        return fallback;
    }

    private static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        return Regex.Replace(input.Trim(), @"\s+", " ");
    }

    private static ImageSource GetOrLoadIcon(string iconKey, HardwareType fallbackType)
    {
        if (IconCache.TryGetValue(iconKey, out var cached))
        {
            return cached;
        }

        var image = TryLoadIcon(iconKey) ?? TryLoadIcon(GetDefaultKey(fallbackType));
        
        if (image == null)
        {
            image = CreateFallbackImage();
        }

        image.Freeze();
        IconCache[iconKey] = image;
        return image;
    }

    private static ImageSource? TryLoadIcon(string iconKey)
    {
        var candidates = new[]
        {
            $"{PackBase}{iconKey}.png",
            $"pack://application:,,,/Resources/Icons/{iconKey}.png"
        };

        foreach (var uri in candidates)
        {
            try
            {
                var stream = Application.GetResourceStream(new Uri(uri, UriKind.Absolute));
                if (stream == null) continue;

                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = stream.Stream;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                return image;
            }
            catch
            {
                // Continue to next candidate
            }
        }

        return null;
    }

    private static ImageSource CreateFallbackImage()
    {
        var fallback = new DrawingImage(new GeometryDrawing(
            Brushes.Transparent,
            null,
            Geometry.Empty));
        return fallback;
    }

    public static void ClearCache()
    {
        IconCache.Clear();
        KeyCache.Clear();
    }

    public static void PreloadCommonIcons()
    {
        var commonTypes = new[]
        {
            HardwareType.Os, HardwareType.Cpu, HardwareType.Gpu,
            HardwareType.Motherboard, HardwareType.Memory, HardwareType.Storage
        };

        foreach (var type in commonTypes)
        {
            _ = Resolve(type, null, null);
        }
    }
}
