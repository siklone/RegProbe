namespace RegProbe.App.Services;

internal sealed class NohutoRepositoryScanner
{
    private readonly NohutoRepoScanClient _client;
    private readonly NohutoRepoScanStore _store;

    public NohutoRepositoryScanner(NohutoRepoScanClient client, NohutoRepoScanStore store)
    {
        _client = client;
        _store = store;
    }

    public async Task<RepositoryScanPayload> ScanAsync(
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
}
