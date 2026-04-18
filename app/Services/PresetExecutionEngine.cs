using Microsoft.Win32;
using RegProbe.Application.Models;
using RegProbe.Core;
using RegProbe.Engine;

namespace RegProbe.Application.Services;

internal sealed class PresetExecutionEngine
{
    private readonly ITweakCatalog _tweakCatalog;

    public PresetExecutionEngine(ITweakCatalog tweakCatalog)
    {
        _tweakCatalog = tweakCatalog;
    }

    public async Task<ApplyPresetResult> ApplyAsync(PresetModel? preset, IProgress<int>? progress, bool dryRun)
    {
        if (preset == null)
        {
            return new ApplyPresetResult(
                Success: false,
                Applied: 0,
                Total: 0,
                FailedTweaks: new List<string>(),
                Message: "Preset was not found");
        }

        var appliedCount = 0;
        var failedTweaks = new List<string>();
        var total = preset.TweakIds.Count;

        for (int index = 0; index < preset.TweakIds.Count; index++)
        {
            var tweakId = preset.TweakIds[index];

            try
            {
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
            catch
            {
                failedTweaks.Add(tweakId);
            }

            progress?.Report((index + 1) * 100 / total);
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
            Message: message);
    }

    public async Task<bool> RevertAsync(PresetModel? preset, bool dryRun)
    {
        if (preset == null)
        {
            return false;
        }

        foreach (var tweakId in preset.TweakIds)
        {
            try
            {
                await RevertTweakByIdAsync(tweakId, dryRun);
            }
            catch
            {
            }
        }

        return true;
    }

    public async Task<PresetValidationResult> ValidateAsync(PresetModel? preset)
    {
        var osVersion = ResolveOsVersion();
        if (preset == null)
        {
            return new PresetValidationResult(
                IsValid: false,
                IncompatibleTweaks: new List<string>(),
                OsVersion: osVersion,
                Warnings: new List<string> { "Preset not found" });
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
            Warnings: warnings);
    }

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
            using var currentVersion = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (currentVersion != null)
            {
                var productName = currentVersion.GetValue("ProductName")?.ToString();
                var displayVersion = currentVersion.GetValue("DisplayVersion")?.ToString();
                var buildText = currentVersion.GetValue("CurrentBuild")?.ToString()
                    ?? currentVersion.GetValue("CurrentBuildNumber")?.ToString();

                if (!string.IsNullOrWhiteSpace(productName))
                {
                    if (!string.IsNullOrWhiteSpace(displayVersion) && !string.IsNullOrWhiteSpace(buildText))
                    {
                        return $"{productName} {displayVersion} (build {buildText})";
                    }

                    if (!string.IsNullOrWhiteSpace(displayVersion))
                    {
                        return $"{productName} {displayVersion}";
                    }

                    if (!string.IsNullOrWhiteSpace(buildText))
                    {
                        return $"{productName} (build {buildText})";
                    }

                    return productName;
                }
            }
        }
        catch
        {
        }

        var osVersion = Environment.OSVersion;
        return $"{osVersion.Version.Major}.{osVersion.Version.Minor}.{osVersion.Version.Build}";
    }
}
