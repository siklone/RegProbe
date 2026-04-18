using System.Text.Json;
using RegProbe.Infrastructure;

namespace RegProbe.App.Services;

internal sealed class NohutoRepoScanCache
{
    private readonly AppPaths _paths;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public NohutoRepoScanCache(AppPaths paths)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    public NohutoRepoScanState Load()
    {
        try
        {
            if (!File.Exists(_paths.NohutoScanStateFilePath))
            {
                return new NohutoRepoScanState();
            }

            var json = File.ReadAllText(_paths.NohutoScanStateFilePath);
            var state = JsonSerializer.Deserialize<NohutoRepoScanState>(json, JsonOptions);
            return state ?? new NohutoRepoScanState();
        }
        catch
        {
            return new NohutoRepoScanState();
        }
    }

    public void Save(NohutoRepoScanState state)
    {
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(_paths.NohutoScanStateFilePath, json);
    }

    public static bool ShouldUseCachedState(NohutoRepoScanState state, TimeSpan? minimumRefreshInterval)
    {
        if (!minimumRefreshInterval.HasValue ||
            minimumRefreshInterval.Value <= TimeSpan.Zero ||
            state.Repositories.Count == 0 ||
            state.LastCheckedAtUtc == default)
        {
            return false;
        }

        return DateTimeOffset.UtcNow - state.LastCheckedAtUtc < minimumRefreshInterval.Value;
    }
}
