using RegProbe.Application.Services;
using RegProbe.Core;
using RegProbe.Engine;
using RegProbe.Infrastructure;
using System.Text.Json;

namespace RegProbe.Tests;

public sealed class ConfigExportServiceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "RegProbe-ConfigExportService", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ExportAsync_CreatesMissingParentDirectories()
    {
        var service = new ConfigExportService(
            new EmptyTweakCatalog(),
            new DnsService(),
            new InMemorySettingsStore());
        var outputPath = Path.Combine(_tempDirectory, "nested", "config.json");

        var success = await service.ExportAsync(
            outputPath,
            new ExportOptions
            {
                IncludeTweakStates = false,
                IncludeDnsSettings = false,
                IncludeAppSettings = false
            });

        Assert.True(success);
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task ImportAsync_DryRunFailsForUnknownTweakIds()
    {
        var service = new ConfigExportService(
            new EmptyTweakCatalog(),
            new DnsService(),
            new InMemorySettingsStore());
        var inputPath = WriteConfig(new ExportedConfig
        {
            AppliedTweakIds = ["missing.tweak"]
        });

        var result = await service.ImportAsync(inputPath, dryRun: true);

        Assert.False(result.Success);
        Assert.Equal("Import validation failed with 1 issue(s).", result.Message);
        Assert.Equal(1, result.TweaksToApply);
    }

    [Fact]
    public async Task ImportAsync_DryRunIgnoresBlankTweakIds()
    {
        var service = new ConfigExportService(
            new EmptyTweakCatalog(),
            new DnsService(),
            new InMemorySettingsStore());
        var inputPath = WriteConfig(new ExportedConfig
        {
            AppliedTweakIds = ["", "   "]
        });

        var result = await service.ImportAsync(inputPath, dryRun: true);

        Assert.True(result.Success);
        Assert.Equal(0, result.TweaksToApply);
        Assert.Equal(0, result.TotalChanges);
    }

    [Fact]
    public async Task ImportAsync_DryRunFailsForUnknownDnsProvider()
    {
        var service = new ConfigExportService(
            new EmptyTweakCatalog(),
            new DnsService(),
            new InMemorySettingsStore());
        var inputPath = WriteConfig(new ExportedConfig
        {
            DnsProvider = "UnknownDns"
        });

        var result = await service.ImportAsync(inputPath, dryRun: true);

        Assert.False(result.Success);
        Assert.Equal("Import validation failed with 1 issue(s).", result.Message);
        Assert.True(result.DnsToSet);
    }

    [Fact]
    public async Task ImportAsync_DryRunDoesNotCountBlankDnsProviderAsAChange()
    {
        var service = new ConfigExportService(
            new EmptyTweakCatalog(),
            new DnsService(),
            new InMemorySettingsStore());
        var inputPath = WriteConfig(new ExportedConfig
        {
            DnsProvider = "   "
        });

        var result = await service.ImportAsync(inputPath, dryRun: true);

        Assert.True(result.Success);
        Assert.False(result.DnsToSet);
        Assert.Equal(0, result.TotalChanges);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
        }
    }

    private string WriteConfig(ExportedConfig config)
    {
        Directory.CreateDirectory(_tempDirectory);
        var path = Path.Combine(_tempDirectory, $"{Guid.NewGuid():N}.json");
        File.WriteAllText(
            path,
            JsonSerializer.Serialize(
                config,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                }));
        return path;
    }

    private sealed class EmptyTweakCatalog : ITweakCatalog
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

    private sealed class InMemorySettingsStore : ISettingsStore
    {
        public Task<AppSettings> LoadAsync(CancellationToken ct)
            => Task.FromResult(new AppSettings());

        public Task SaveAsync(AppSettings settings, CancellationToken ct)
            => Task.CompletedTask;
    }
}
