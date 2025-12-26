using System;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;
using WindowsOptimizer.Infrastructure;

namespace WindowsOptimizer.Engine.Tweaks;

public sealed class SettingsToggleTweak : ITweak
{
    private readonly ISettingsStore _settingsStore;
    private readonly Func<AppSettings, bool> _getter;
    private readonly Action<AppSettings, bool> _setter;
    private readonly bool _targetValue;
    private bool? _detectedValue;

    public SettingsToggleTweak(
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        ISettingsStore settingsStore,
        Func<AppSettings, bool> getter,
        Action<AppSettings, bool> setter,
        bool targetValue = true,
        bool requiresElevation = false)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Risk = risk;
        RequiresElevation = requiresElevation;
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _getter = getter ?? throw new ArgumentNullException(nameof(getter));
        _setter = setter ?? throw new ArgumentNullException(nameof(setter));
        _targetValue = targetValue;
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public TweakRiskLevel Risk { get; }
    public bool RequiresElevation { get; }

    public async Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        var settings = await _settingsStore.LoadAsync(ct);
        var current = _getter(settings);
        _detectedValue = current;
        var state = current ? "enabled" : "disabled";
        var status = current == _targetValue ? TweakStatus.Applied : TweakStatus.Detected;
        return new TweakResult(status, $"Current value is {state}.", DateTimeOffset.UtcNow);
    }

    public async Task<TweakResult> ApplyAsync(CancellationToken ct)
    {
        await UpdateSettingAsync(_targetValue, ct);
        var state = _targetValue ? "enabled" : "disabled";
        return new TweakResult(TweakStatus.Applied, $"Set value to {state}.", DateTimeOffset.UtcNow);
    }

    public async Task<TweakResult> VerifyAsync(CancellationToken ct)
    {
        var settings = await _settingsStore.LoadAsync(ct);
        var current = _getter(settings);
        if (current == _targetValue)
        {
            return new TweakResult(TweakStatus.Verified, "Verified desired value.", DateTimeOffset.UtcNow);
        }

        return new TweakResult(
            TweakStatus.Failed,
            "Verification failed. Value does not match expected state.",
            DateTimeOffset.UtcNow);
    }

    public async Task<TweakResult> RollbackAsync(CancellationToken ct)
    {
        if (_detectedValue is null)
        {
            return new TweakResult(
                TweakStatus.Skipped,
                "Rollback skipped because no prior detect state is available.",
                DateTimeOffset.UtcNow);
        }

        await UpdateSettingAsync(_detectedValue.Value, ct);
        var state = _detectedValue.Value ? "enabled" : "disabled";
        return new TweakResult(TweakStatus.RolledBack, $"Rolled back to {state}.", DateTimeOffset.UtcNow);
    }

    private async Task UpdateSettingAsync(bool value, CancellationToken ct)
    {
        var settings = await _settingsStore.LoadAsync(ct);
        _setter(settings, value);
        await _settingsStore.SaveAsync(settings, ct);
    }
}
