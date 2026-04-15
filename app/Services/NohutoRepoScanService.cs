using System.Diagnostics;
using RegProbe.Infrastructure;

namespace RegProbe.App.Services;

public sealed class NohutoRepoScanService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly NohutoRepoScanStore _store;
    private readonly NohutoRepoScanClient _client;
    private bool _disposed;

    public NohutoRepoScanService(AppPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "RegProbe-NohutoScan");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

        _store = new NohutoRepoScanStore(paths);
        _client = new NohutoRepoScanClient(_httpClient);
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
                    return ScanRepositoryAsync(definition, repoState, ct);
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

    private async Task<RepositoryScanPayload> ScanRepositoryAsync(
        NohutoRepositoryDefinition definition,
        NohutoTrackedRepositoryState? previousState,
        CancellationToken ct)
    {
        try
        {
            var latest = await _client.GetLatestCommitAsync(definition, ct);
            if (!latest.HasValue || string.IsNullOrWhiteSpace(latest.Value.Sha))
            {
                return _store.BuildFailureState(definition, previousState, "Commit metadata unavailable.");
            }

            var latestSha = latest.Value.Sha;
            var latestDate = latest.Value.Date;
            var latestMessage = latest.Value.Message;
            var isBaseline = previousState is null || string.IsNullOrWhiteSpace(previousState.LastSeenCommitSha);

            if (!isBaseline &&
                string.Equals(previousState!.LastSeenCommitSha, latestSha, StringComparison.OrdinalIgnoreCase))
            {
                var unchangedState = NohutoRepoScanStore.CloneState(definition, previousState);
                unchangedState.LastCheckedAtUtc = DateTimeOffset.UtcNow;
                unchangedState.LastSeenCommitDateUtc = latestDate;
                unchangedState.LastSeenCommitMessage = latestMessage;
                unchangedState.CheckedSuccessfully = true;
                unchangedState.HasNewCommit = false;
                unchangedState.StateKind = NohutoRepositoryStateKind.Unchanged;
                unchangedState.Summary = NohutoRepoScanStore.BuildUnchangedSummary(unchangedState.LastAnalysis, latestSha);

                return new RepositoryScanPayload
                {
                    Definition = definition,
                    State = unchangedState
                };
            }

            var changedFiles = isBaseline
                ? await _client.GetCommitFilesAsync(definition, latestSha, ct)
                : await _client.GetCompareFilesAsync(definition, previousState!.LastSeenCommitSha, latestSha, ct);

            var analysis = NohutoChangeAnalyzer.Analyze(definition, changedFiles);
            var stateKind = isBaseline
                ? NohutoRepositoryStateKind.Baseline
                : NohutoRepositoryStateKind.Updated;

            var updatedState = new NohutoTrackedRepositoryState
            {
                RepoId = definition.Id,
                DisplayName = definition.DisplayName,
                RoleLabel = definition.RoleLabel,
                RoleSummary = definition.RoleSummary,
                RepositoryUrl = definition.RepositoryUrl,
                LastSeenCommitSha = latestSha,
                LastSeenCommitMessage = latestMessage,
                LastSeenCommitDateUtc = latestDate,
                LastCheckedAtUtc = DateTimeOffset.UtcNow,
                CheckedSuccessfully = true,
                HasNewCommit = stateKind == NohutoRepositoryStateKind.Updated,
                StateKind = stateKind,
                Summary = NohutoRepoScanStore.BuildRepositorySummary(stateKind, latestSha, analysis),
                LastAnalysis = analysis
            };

            return new RepositoryScanPayload
            {
                Definition = definition,
                State = updatedState,
                ChangedFiles = changedFiles
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return _store.BuildFailureState(definition, previousState, ex.Message);
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
