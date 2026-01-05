using WindowsOptimizer.App.Models;

namespace WindowsOptimizer.App.Services;

/// <summary>
/// Service for managing and applying optimization presets.
/// </summary>
public class PresetService
{
    public PresetService()
    {
    }

    /// <summary>
    /// Gets all available presets.
    /// </summary>
    public List<PresetModel> GetAllPresets()
    {
        return new List<PresetModel>
        {
            CreateGamingPreset(),
            CreatePrivacyPreset(),
            CreateMinimalistPreset()
        };
    }

    /// <summary>
    /// Applies a preset with progress reporting.
    /// </summary>
    public async Task<ApplyPresetResult> ApplyPresetAsync(string presetId, IProgress<int>? progress)
    {
        var preset = GetAllPresets().FirstOrDefault(p => p.Id == presetId);
        if (preset == null)
        {
            return new ApplyPresetResult(
                Success: false,
                Applied: 0,
                Total: 0,
                FailedTweaks: new List<string>(),
                Message: $"Preset '{presetId}' not found"
            );
        }


        var appliedCount = 0;
        var failedTweaks = new List<string>();
        var total = preset.TweakIds.Count;

        for (int i = 0; i < preset.TweakIds.Count; i++)
        {
            var tweakId = preset.TweakIds[i];
            
            try
            {
                // Apply tweak through existing TweakService
                var success = await ApplyTweakByIdAsync(tweakId);
                if (success)
                {
                    appliedCount++;
                }
                else
                {
                    failedTweaks.Add(tweakId);
                }
            }
            catch (Exception)
            {
                failedTweaks.Add(tweakId);
            }

            // Report progress
            progress?.Report((i + 1) * 100 / total);
        }

        var allApplied = appliedCount == total;
        var message = allApplied
            ? $"Successfully applied {appliedCount} tweaks"
            : $"Applied {appliedCount}/{total} tweaks. {failedTweaks.Count} failed.";


        return new ApplyPresetResult(
            Success: allApplied,
            Applied: appliedCount,
            Total: total,
            FailedTweaks: failedTweaks,
            Message: message
        );
    }

    /// <summary>
    /// Reverts all tweaks in a preset.
    /// </summary>
    public async Task<bool> RevertPresetAsync(string presetId)
    {
        var preset = GetAllPresets().FirstOrDefault(p => p.Id == presetId);
        if (preset == null) return false;


        foreach (var tweakId in preset.TweakIds)
        {
            try
            {
                await RevertTweakByIdAsync(tweakId);
            }
            catch (Exception)
            {
                // Silently ignore revert failures
            }
        }

        return true;
    }

    /// <summary>
    /// Validates if preset is compatible with current system.
    /// </summary>
    public Task<PresetValidationResult> ValidatePresetAsync(string presetId)
    {
        var preset = GetAllPresets().FirstOrDefault(p => p.Id == presetId);
        if (preset == null)
        {
            return Task.FromResult(new PresetValidationResult(
                IsValid: false,
                IncompatibleTweaks: new List<string>(),
                OsVersion: Environment.OSVersion.VersionString,
                Warnings: new List<string> { "Preset not found" }
            ));
        }

        // For now, assume all tweaks are compatible
        // TODO: Add actual compatibility checks based on OS version
        return Task.FromResult(new PresetValidationResult(
            IsValid: true,
            IncompatibleTweaks: new List<string>(),
            OsVersion: Environment.OSVersion.VersionString,
            Warnings: new List<string>()
        ));
    }

    // Preset Definitions

    private PresetModel CreateGamingPreset()
    {
        return new PresetModel(
            Id: "gaming",
            Name: "Gaming Optimization",
            Description: "Maximize FPS and minimize latency for gaming. Disables background services, enables high performance mode, and optimizes network settings.",
            IconPath: "pack://application:,,,/Resources/Icons/gaming.png",
            Category: PresetCategory.Gaming,
            TweakIds: new List<string>
            {
                // Note: These IDs must match existing tweak IDs in TweakProviders
                "disable-game-bar",
                "disable-game-dvr",
                // Add more gaming-related tweak IDs here as they're implemented
            },
            Level: PresetDifficulty.Beginner
        );
    }

    private PresetModel CreatePrivacyPreset()
    {
        return new PresetModel(
            Id: "privacy",
            Name: "Privacy Protection",
            Description: "Maximum privacy by disabling telemetry, tracking, and data collection. Includes disabling Cortana, ad tracking, and cloud sync.",
            IconPath: "pack://application:,,,/Resources/Icons/privacy.png",
            Category: PresetCategory.Privacy,
            TweakIds: new List<string>
            {
                "disable-telemetry",
                "disable-activity-history",
                "disable-advertising-id",
                // Add more privacy-related tweak IDs
            },
            Level: PresetDifficulty.Beginner
        );
    }

    private PresetModel CreateMinimalistPreset()
    {
        return new PresetModel(
            Id: "minimalist",
            Name: "Minimalist Interface",
            Description: "Clean, fast interface by removing visual effects, disabling widgets, and removing taskbar bloat. Best for older hardware.",
            IconPath: "pack://application:,,,/Resources/Icons/minimal.png",
            Category: PresetCategory.Minimal,
            TweakIds: new List<string>
            {
                "disable-animations",
                "disable-transparency",
                // Add more UI-related tweak IDs
            },
            Level: PresetDifficulty.Beginner
        );
    }

    // Helper methods to integrate with existing TweakService

    private async Task<bool> ApplyTweakByIdAsync(string tweakId)
    {
        // TODO: Implement integration with TweakService
        // This will need to find the tweak by ID and apply it
        await Task.Delay(100); // Simulate work
        return true;
    }

    private async Task RevertTweakByIdAsync(string tweakId)
    {
        // TODO: Implement integration with TweakService
        await Task.Delay(100); // Simulate work
    }
}
