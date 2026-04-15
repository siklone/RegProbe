using System.Diagnostics;
using RegProbe.Infrastructure;

namespace RegProbe.App.Services;

public sealed class WinConfigCatalogService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly WinConfigCatalogStore _store;
    private readonly WinConfigCatalogClient _client;
    private bool _disposed;

    public WinConfigCatalogService(AppPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "RegProbe-WinConfigCatalog");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

        _store = new WinConfigCatalogStore(paths);
        _client = new WinConfigCatalogClient(_httpClient);
    }

    public WinConfigCatalogState LoadCachedState()
        => _store.LoadCachedState();

    public async Task<WinConfigCatalogResult> RefreshAsync(CancellationToken ct, TimeSpan? minimumRefreshInterval = null)
    {
        try
        {
            _store.EnsureDirectories();

            var cachedState = _store.LoadCachedState();
            if (WinConfigCatalogStore.ShouldUseCachedState(cachedState, minimumRefreshInterval))
            {
                return _store.BuildResult(cachedState, usedCachedData: true);
            }

            var latestCommit = await _client.GetLatestCommitAsync(ct);
            if (cachedState.Categories.Count > 0 &&
                !string.IsNullOrWhiteSpace(cachedState.LastCommitSha) &&
                string.Equals(cachedState.LastCommitSha, latestCommit.Sha, StringComparison.OrdinalIgnoreCase))
            {
                cachedState.LastCheckedAtUtc = DateTimeOffset.UtcNow;
                cachedState.LastCommitDateUtc = latestCommit.Date;
                cachedState.LastSummary = WinConfigCatalogStore.BuildSummary(cachedState.Categories);
                _store.SaveState(cachedState);
                _store.SaveMarkdownReport(cachedState);
                return _store.BuildResult(cachedState, usedCachedData: false);
            }

            var categories = await _client.LoadCategoriesAsync(ct);
            var refreshedState = new WinConfigCatalogState
            {
                LastCheckedAtUtc = DateTimeOffset.UtcNow,
                LastCommitSha = latestCommit.Sha,
                LastCommitDateUtc = latestCommit.Date,
                LastSummary = WinConfigCatalogStore.BuildSummary(categories),
                Categories = categories
            };

            _store.SaveState(refreshedState);
            _store.SaveMarkdownReport(refreshedState);
            return _store.BuildResult(refreshedState, usedCachedData: false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WinConfigCatalog] Failed: {ex.Message}");
            return new WinConfigCatalogResult
            {
                CheckedSuccessfully = false,
                UsedCachedData = false,
                Summary = $"win-config catalog refresh failed: {ex.Message}",
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
