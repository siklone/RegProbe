using RegProbe.Application.Models;
using RegProbe.Application.Services;
using RegProbe.Core;
using RegProbe.Engine;

namespace RegProbe.Tests;

public sealed class PresetServiceTests
{
    [Fact]
    public async Task ApplyPresetAsync_MatchesTrimmedCaseInsensitiveIds()
    {
        var service = new PresetService(new MissingTweakCatalog());

        var result = await service.ApplyPresetAsync("  GAMING  ", progress: null, dryRun: true);

        Assert.NotEqual("Preset was not found", result.Message);
        Assert.Equal(2, result.Total);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ApplyPresetAsync_ReturnsNotFoundForBlankIds(string presetId)
    {
        var service = new PresetService(new MissingTweakCatalog());

        var result = await service.ApplyPresetAsync(presetId, progress: null, dryRun: true);

        Assert.Equal("Preset was not found", result.Message);
        Assert.Equal(0, result.Total);
    }

    private sealed class MissingTweakCatalog : ITweakCatalog
    {
        public IReadOnlyList<TweakCatalogEntry> GetAll() => Array.Empty<TweakCatalogEntry>();

        public ITweak? FindById(string tweakId) => null;

        public Task<TweakExecutionReport> ExecuteAsync(
            ITweak tweak,
            TweakExecutionOptions options,
            IProgress<TweakExecutionUpdate>? progress = null,
            CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<TweakExecutionStep> ExecuteStepAsync(
            ITweak tweak,
            TweakAction action,
            IProgress<TweakExecutionUpdate>? progress = null,
            CancellationToken ct = default)
            => throw new NotSupportedException();

        public bool IsElevated => false;
        public bool IsElevatedHostAvailable => false;
        public string ElevatedHostPath => string.Empty;
    }
}
