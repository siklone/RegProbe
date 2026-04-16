using System.Diagnostics;
using RegProbe.Infrastructure;

namespace RegProbe.App.Services;

public sealed class NohutoRepoScanService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly NohutoRepoScanStore _store;
    private readonly NohutoRepositoryScanner _scanner;
    private bool _disposed;

    public NohutoRepoScanService(AppPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "RegProbe-NohutoScan");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

        _store = new NohutoRepoScanStore(paths);
        _scanner = new NohutoRepositoryScanner(new NohutoRepoScanClient(_httpClient), _store);
    }

    public NohutoRepoScanState LoadCachedState()
        => _store.LoadCachedState();

    public async Task<NohutoRepoScanResult> CheckAndAnalyzeAsync(CancellationToken ct, TimeSpan? minimumRefreshInterval = null)
    {
        try
        {
            _store.EnsureDirectories();

            var previous = _store.LoadCachedState();
            if (NohutoRepoScanStore.ShouldUseCachedState(previous, minimumRefreshInterval))
            {
                return _store.BuildResultFromState(previous, usedCachedData: true);
            }

            var previousByRepoId = previous.Repositories.ToDictionary(
                static repository => repository.RepoId,
                StringComparer.OrdinalIgnoreCase);

            var scanTasks = NohutoConfigurationSourceCatalog.All
                .Select(definition =>
                {
                    previousByRepoId.TryGetValue(definition.Id, out var repoState);
                    return _scanner.ScanAsync(definition, repoState, ct);
                })
                .ToArray();

            var repoScans = await Task.WhenAll(scanTasks);
            var orderedRepositories = repoScans
                .OrderBy(scan => NohutoRepoScanStore.GetDefinitionOrder(scan.State.RepoId))
                .Select(scan => scan.State)
                .ToList();

            var state = new NohutoRepoScanState
            {
                LastCheckedAtUtc = DateTimeOffset.UtcNow,
                LastSummary = NohutoRepoScanStore.BuildAggregateSummary(orderedRepositories),
                Repositories = orderedRepositories
            };

            _store.SaveState(state);
            _store.SaveReport(state, repoScans);

            return _store.BuildResultFromState(state, usedCachedData: false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NohutoRepoScan] Failed: {ex.Message}");
            return new NohutoRepoScanResult
            {
                CheckedSuccessfully = false,
                Summary = $"Configuration source scan failed: {ex.Message}",
                JsonReportPath = _store.JsonReportPath,
                MarkdownReportPath = _store.MarkdownReportPath
            };
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _httpClient.Dispose();
    }
}
