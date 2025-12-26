using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Infrastructure;
using Xunit;

public sealed class SettingsToggleTweakTests
{
    [Fact]
    public async Task Detect_WhenAlreadyInDesiredState_ReturnsApplied()
    {
        var store = new InMemorySettingsStore(new AppSettings { DemoTweakAlphaEnabled = true });
        var tweak = BuildTweak(store);

        var result = await tweak.DetectAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Applied, result.Status);
    }

    [Fact]
    public async Task RollbackWithoutDetect_SkipsAndDoesNotChangeValue()
    {
        var store = new InMemorySettingsStore(new AppSettings { DemoTweakAlphaEnabled = false });
        var tweak = BuildTweak(store);

        var result = await tweak.RollbackAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Skipped, result.Status);
        var settings = await store.LoadAsync(CancellationToken.None);
        Assert.False(settings.DemoTweakAlphaEnabled);
    }

    [Fact]
    public async Task DetectApplyRollback_RestoresDetectedValue()
    {
        var store = new InMemorySettingsStore(new AppSettings { DemoTweakAlphaEnabled = false });
        var tweak = BuildTweak(store);

        await tweak.DetectAsync(CancellationToken.None);
        await tweak.ApplyAsync(CancellationToken.None);
        var afterApply = await store.LoadAsync(CancellationToken.None);
        Assert.True(afterApply.DemoTweakAlphaEnabled);

        var result = await tweak.RollbackAsync(CancellationToken.None);
        Assert.Equal(TweakStatus.RolledBack, result.Status);
        var afterRollback = await store.LoadAsync(CancellationToken.None);
        Assert.False(afterRollback.DemoTweakAlphaEnabled);
    }

    private static SettingsToggleTweak BuildTweak(ISettingsStore store)
    {
        return new SettingsToggleTweak(
            "demo.alpha",
            "Demo: Enable performance profile",
            "Demo toggle stored in app settings.",
            TweakRiskLevel.Safe,
            store,
            settings => settings.DemoTweakAlphaEnabled,
            (settings, value) => settings.DemoTweakAlphaEnabled = value);
    }

    private sealed class InMemorySettingsStore : ISettingsStore
    {
        private AppSettings _settings;

        public InMemorySettingsStore(AppSettings initial)
        {
            _settings = Clone(initial);
        }

        public Task<AppSettings> LoadAsync(CancellationToken ct)
        {
            return Task.FromResult(Clone(_settings));
        }

        public Task SaveAsync(AppSettings settings, CancellationToken ct)
        {
            _settings = Clone(settings);
            return Task.CompletedTask;
        }

        private static AppSettings Clone(AppSettings settings)
        {
            return new AppSettings
            {
                SchemaVersion = settings.SchemaVersion,
                DemoTweakAlphaEnabled = settings.DemoTweakAlphaEnabled,
                DemoTweakBetaEnabled = settings.DemoTweakBetaEnabled
            };
        }
    }
}
