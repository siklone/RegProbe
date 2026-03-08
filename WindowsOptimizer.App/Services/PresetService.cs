using WindowsOptimizer.App.Models;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine;

namespace WindowsOptimizer.App.Services;

/// <summary>
/// Service for managing and applying optimization presets.
/// </summary>
public class PresetService
{
    private readonly ITweakCatalog _tweakCatalog;

    public PresetService(ITweakCatalog? tweakCatalog = null)
    {
        _tweakCatalog = tweakCatalog ?? new TweakCatalogService();
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
    public async Task<ApplyPresetResult> ApplyPresetAsync(string presetId, IProgress<int>? progress, bool dryRun = false)
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
                var success = await ApplyTweakByIdAsync(tweakId, dryRun);
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
    public async Task<bool> RevertPresetAsync(string presetId, bool dryRun = false)
    {
        var preset = GetAllPresets().FirstOrDefault(p => p.Id == presetId);
        if (preset == null) return false;

        foreach (var tweakId in preset.TweakIds)
        {
            try
            {
                await RevertTweakByIdAsync(tweakId, dryRun);
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
    public async Task<PresetValidationResult> ValidatePresetAsync(string presetId)
    {
        var preset = GetAllPresets().FirstOrDefault(p => p.Id == presetId);
        var osVersion = ResolveOsVersion();
        if (preset == null)
        {
            return new PresetValidationResult(
                IsValid: false,
                IncompatibleTweaks: new List<string>(),
                OsVersion: osVersion,
                Warnings: new List<string> { "Preset not found" }
            );
        }

        var incompatibleTweaks = new List<string>();
        var warnings = new List<string>();

        foreach (var tweakId in preset.TweakIds)
        {
            var tweak = _tweakCatalog.FindById(tweakId);
            if (tweak is null)
            {
                incompatibleTweaks.Add(tweakId);
                warnings.Add($"Tweak '{tweakId}' is not available in the current catalog.");
                continue;
            }

            try
            {
                var detectStep = await _tweakCatalog.ExecuteStepAsync(tweak, TweakAction.Detect);
                if (detectStep.Result.Status is TweakStatus.NotApplicable or TweakStatus.Failed)
                {
                    incompatibleTweaks.Add(tweakId);

                    if (!string.IsNullOrWhiteSpace(detectStep.Result.Message))
                    {
                        warnings.Add($"{tweak.Name}: {detectStep.Result.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                incompatibleTweaks.Add(tweakId);
                warnings.Add($"{tweak.Name}: validation failed ({ex.Message})");
            }
        }

        return new PresetValidationResult(
            IsValid: incompatibleTweaks.Count == 0,
            IncompatibleTweaks: incompatibleTweaks,
            OsVersion: osVersion,
            Warnings: warnings
        );
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

    private async Task<bool> ApplyTweakByIdAsync(string tweakId, bool dryRun)
    {
        var tweak = _tweakCatalog.FindById(tweakId);
        if (tweak is null)
        {
            return false;
        }

        var options = new TweakExecutionOptions
        {
            DryRun = dryRun,
            VerifyAfterApply = true,
            RollbackOnFailure = true
        };

        var report = await _tweakCatalog.ExecuteAsync(tweak, options);
        if (options.DryRun)
        {
            return report.Succeeded;
        }

        return report.Succeeded && report.Applied;
    }

    private async Task RevertTweakByIdAsync(string tweakId, bool dryRun)
    {
        var tweak = _tweakCatalog.FindById(tweakId);
        if (tweak is null)
        {
            return;
        }

        var detectStep = await _tweakCatalog.ExecuteStepAsync(tweak, TweakAction.Detect);
        if (detectStep.Result.Status is TweakStatus.Failed or TweakStatus.NotApplicable)
        {
            return;
        }

        if (dryRun)
        {
            return;
        }

        await _tweakCatalog.ExecuteStepAsync(tweak, TweakAction.Rollback);
    }

    private static string ResolveOsVersion()
    {
        try
        {
            return OsDetectionResolver.Resolve(includeWmiCrossCheck: false).NormalizedName;
        }
        catch
        {
            return Environment.OSVersion.VersionString;
        }
    }
}
