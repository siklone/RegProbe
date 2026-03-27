using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RegProbe.Infrastructure;

public sealed class SettingsStore : ISettingsStore
{
    private readonly AppPaths _paths;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true
    };

    public SettingsStore(AppPaths paths)
    {
        _paths = paths;
    }

    public async Task<AppSettings> LoadAsync(CancellationToken ct)
    {
        _paths.EnsureDirectories();

        if (!File.Exists(_paths.SettingsFilePath))
        {
            return new AppSettings();
        }

        await using var stream = File.OpenRead(_paths.SettingsFilePath);
        var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, _serializerOptions, ct);
        return settings ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken ct)
    {
        _paths.EnsureDirectories();

        await using var stream = File.Create(_paths.SettingsFilePath);
        await JsonSerializer.SerializeAsync(stream, settings, _serializerOptions, ct);
    }
}
