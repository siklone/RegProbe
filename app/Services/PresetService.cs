using RegProbe.Application.Models;

namespace RegProbe.Application.Services;

/// <summary>
/// Service for managing and applying optimization presets.
/// </summary>
public class PresetService
{
    private readonly PresetCatalog _catalog;
    private readonly PresetExecutionEngine _executionEngine;

    public PresetService(ITweakCatalog? tweakCatalog = null)
    {
        var resolvedCatalog = tweakCatalog ?? new TweakCatalogService();
        _catalog = new PresetCatalog();
        _executionEngine = new PresetExecutionEngine(resolvedCatalog);
    }

    public List<PresetModel> GetAllPresets()
    {
        return _catalog.GetAll();
    }

    public Task<ApplyPresetResult> ApplyPresetAsync(string presetId, IProgress<int>? progress, bool dryRun = false)
    {
        return _executionEngine.ApplyAsync(_catalog.FindById(presetId), progress, dryRun);
    }

    public Task<bool> RevertPresetAsync(string presetId, bool dryRun = false)
    {
        return _executionEngine.RevertAsync(_catalog.FindById(presetId), dryRun);
    }

    public Task<PresetValidationResult> ValidatePresetAsync(string presetId)
    {
        return _executionEngine.ValidateAsync(_catalog.FindById(presetId));
    }
}
