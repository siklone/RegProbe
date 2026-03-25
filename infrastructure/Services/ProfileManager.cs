using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using OpenTraceProject.Core.Models;
using OpenTraceProject.Core.Services;

namespace OpenTraceProject.Infrastructure.Services;

public sealed class ProfileManager : IProfileManager
{
    private readonly AppPaths _appPaths;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public ProfileManager(AppPaths appPaths)
    {
        _appPaths = appPaths ?? throw new ArgumentNullException(nameof(appPaths));
    }

    public async Task<TweakProfile> LoadProfileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Profile file not found: {filePath}");
        }

        var json = await File.ReadAllTextAsync(filePath);
        var profile = JsonSerializer.Deserialize<TweakProfile>(json, JsonOptions);

        if (profile == null)
        {
            throw new InvalidOperationException($"Failed to deserialize profile from: {filePath}");
        }

        return profile;
    }

    public async Task SaveProfileAsync(TweakProfile profile, string filePath)
    {
        if (profile == null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(profile, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<List<TweakProfile>> GetPresetsAsync()
    {
        var presets = new List<TweakProfile>();

        if (!Directory.Exists(_appPaths.PresetsDirectory))
        {
            return presets;
        }

        var presetFiles = Directory.GetFiles(_appPaths.PresetsDirectory, "*.json");

        foreach (var file in presetFiles)
        {
            try
            {
                var profile = await LoadProfileAsync(file);
                presets.Add(profile);
            }
            catch
            {
                // Skip invalid preset files
            }
        }

        return presets;
    }

    public async Task InitializePresetsAsync()
    {
        Directory.CreateDirectory(_appPaths.PresetsDirectory);

        var presetNames = new[] { "Gaming", "Privacy", "Performance", "Minimal" };

        foreach (var presetName in presetNames)
        {
            var presetPath = Path.Combine(_appPaths.PresetsDirectory, $"{presetName.ToLowerInvariant()}.json");

            if (!File.Exists(presetPath))
            {
                var preset = await CreatePresetAsync(presetName);
                await SaveProfileAsync(preset, presetPath);
            }
        }
    }

    public Task<TweakProfile> CreatePresetAsync(string presetName)
    {
        var profile = presetName switch
        {
            "Gaming" => CreateGamingPreset(),
            "Privacy" => CreatePrivacyPreset(),
            "Performance" => CreatePerformancePreset(),
            "Minimal" => CreateMinimalPreset(),
            _ => throw new ArgumentException($"Unknown preset: {presetName}", nameof(presetName))
        };

        return Task.FromResult(profile);
    }

    private TweakProfile CreateGamingPreset()
    {
        var tweakIds = new List<string>
        {
            // System tweaks for gaming
            "system.enable-game-mode",
            "system.disable-background-apps",
            "system.disable-clipboard-history",
            "system.disable-clipboard-sync",

            // Performance tweaks
            "performance.disable-superfetch",
            "performance.disable-prefetch",
            "performance.disable-windows-search",
            "performance.disable-runtime-broker",
            "performance.optimize-network-throttling",
            "performance.disable-memory-compression",

            // Power settings for maximum performance
            "power.high-performance-plan",
            "power.disable-usb-selective-suspend",
            "power.disable-disk-timeout",
            "power.disable-sleep",

            // Visual effects for performance
            "visibility.disable-transparency",
            "visibility.disable-animations",
            "visibility.disable-shadow-effects",
            "visibility.disable-menu-animation",
            "visibility.disable-fade-effects",

            // Peripheral optimization
            "peripheral.disable-pointer-precision",
            "peripheral.optimize-mouse-responsiveness",

            // Audio optimization
            "audio.disable-exclusive-mode",
            "audio.disable-audio-enhancements",
            "audio.disable-communications-ducking",

            // Network optimization
            "network.optimize-tcp-settings",
            "network.disable-nagle-algorithm",
            "network.optimize-network-adapter"
        };

        return new TweakProfile
        {
            Name = "Gaming Optimization",
            Description = "Optimized settings for gaming performance. Maximizes FPS, reduces input lag, and minimizes background interference.",
            Author = "OpenTraceProject",
            CreatedDate = DateTime.Now,
            Version = "1.0",
            SelectedTweakIds = tweakIds,
            AppliedTweakIds = new List<string>(),
            Metadata = new ProfileMetadata
            {
                TargetUseCase = "Gaming",
                TotalTweakCount = tweakIds.Count
            }
        };
    }

    private TweakProfile CreatePrivacyPreset()
    {
        var tweakIds = new List<string>
        {
            // Privacy core tweaks
            "privacy.disable-telemetry",
            "privacy.disable-diagnostic-tracking",
            "privacy.disable-activity-history",
            "privacy.disable-advertising-id",
            "privacy.disable-location-tracking",
            "privacy.disable-webcam-access",
            "privacy.disable-microphone-access",
            "privacy.disable-app-diagnostics",
            "privacy.disable-background-apps",
            "privacy.disable-feedback-frequency",
            "privacy.disable-suggested-content",
            "privacy.disable-tailored-experiences",

            // System privacy
            "system.disable-clipboard-history",
            "system.disable-clipboard-sync",
            "system.disable-timeline",
            "system.disable-cortana",

            // Visibility/Spotlight
            "visibility.disable-spotlight-suggestions",
            "visibility.disable-tips-tricks",
            "visibility.disable-start-suggestions",
            "visibility.disable-lockscreen-fun-facts",

            // Network privacy
            "network.disable-teredo",
            "network.disable-ipv6",
            "network.disable-network-discovery",
            "network.disable-wifi-sense",

            // Service-based privacy
            "service.disable-diagnostics-tracking-service",
            "service.disable-dmwappushservice",
            "service.disable-connected-user-experiences",
            "service.disable-customer-experience-improvement",

            // Web browser privacy
            "edge.disable-telemetry",
            "edge.disable-personalization",
            "edge.disable-suggestions",

            // Windows Update privacy
            "update.disable-p2p-delivery",
            "update.disable-automatic-driver-updates",

            // Cloud integration
            "cloud.disable-onedrive-sync",
            "cloud.disable-settings-sync"
        };

        return new TweakProfile
        {
            Name = "Privacy Protection",
            Description = "Maximum privacy settings. Disables telemetry, tracking, and data collection features across Windows.",
            Author = "OpenTraceProject",
            CreatedDate = DateTime.Now,
            Version = "1.0",
            SelectedTweakIds = tweakIds,
            AppliedTweakIds = new List<string>(),
            Metadata = new ProfileMetadata
            {
                TargetUseCase = "Privacy",
                TotalTweakCount = tweakIds.Count
            }
        };
    }

    private TweakProfile CreatePerformancePreset()
    {
        var tweakIds = new List<string>
        {
            // Core performance
            "performance.disable-superfetch",
            "performance.disable-prefetch",
            "performance.disable-windows-search",
            "performance.disable-runtime-broker",
            "performance.optimize-network-throttling",
            "performance.disable-memory-compression",
            "performance.disable-paging-executive",

            // System optimization
            "system.enable-game-mode",
            "system.disable-background-apps",
            "system.optimize-startup-programs",

            // Visual effects
            "visibility.disable-transparency",
            "visibility.disable-animations",
            "visibility.disable-shadow-effects",
            "visibility.disable-menu-animation",
            "visibility.disable-fade-effects",
            "visibility.disable-thumbnail-preview",
            "visibility.disable-aero-peek",

            // Service optimization
            "service.disable-windows-search",
            "service.disable-superfetch",
            "service.disable-sysMain",
            "service.disable-home-group",
            "service.disable-remote-registry",
            "service.optimize-services",

            // Disk optimization
            "disk.disable-8dot3-naming",
            "disk.disable-last-access-timestamp",
            "disk.optimize-ntfs-performance",

            // Network optimization
            "network.optimize-tcp-settings",
            "network.disable-nagle-algorithm",
            "network.optimize-network-adapter",

            // Power settings
            "power.high-performance-plan",
            "power.disable-usb-selective-suspend"
        };

        return new TweakProfile
        {
            Name = "Performance Optimization",
            Description = "Comprehensive performance tweaks. Optimizes system responsiveness, reduces resource usage, and improves overall speed.",
            Author = "OpenTraceProject",
            CreatedDate = DateTime.Now,
            Version = "1.0",
            SelectedTweakIds = tweakIds,
            AppliedTweakIds = new List<string>(),
            Metadata = new ProfileMetadata
            {
                TargetUseCase = "Performance",
                TotalTweakCount = tweakIds.Count
            }
        };
    }

    private TweakProfile CreateMinimalPreset()
    {
        var tweakIds = new List<string>
        {
            // Safe system tweaks
            "system.disable-clipboard-history",
            "system.disable-background-apps",

            // Safe visibility tweaks
            "visibility.disable-transparency",
            "visibility.disable-animations",
            "visibility.disable-tips-tricks",
            "visibility.disable-start-suggestions",
            "visibility.disable-lockscreen-ads",

            // Safe privacy tweaks
            "privacy.disable-advertising-id",
            "privacy.disable-suggested-content",
            "privacy.disable-feedback-frequency",

            // Safe performance tweaks
            "performance.disable-superfetch",
            "performance.optimize-network-throttling",

            // Safe update tweaks
            "update.disable-p2p-delivery",
            "update.disable-automatic-restart",

            // Safe edge tweaks
            "edge.disable-suggestions",
            "edge.disable-preload",

            // Common annoyance removals
            "annoyance.disable-action-center-tips",
            "annoyance.disable-app-suggestions",
            "annoyance.disable-windows-welcome"
        };

        return new TweakProfile
        {
            Name = "Minimal & Safe",
            Description = "Conservative tweaks with minimal risk. Only safe, universally beneficial changes that remove common annoyances.",
            Author = "OpenTraceProject",
            CreatedDate = DateTime.Now,
            Version = "1.0",
            SelectedTweakIds = tweakIds,
            AppliedTweakIds = new List<string>(),
            Metadata = new ProfileMetadata
            {
                TargetUseCase = "Safe & Minimal",
                TotalTweakCount = tweakIds.Count
            }
        };
    }
}
