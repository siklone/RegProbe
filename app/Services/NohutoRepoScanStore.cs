using RegProbe.Infrastructure;

namespace RegProbe.App.Services;

internal sealed class NohutoRepoScanStore
{
    private readonly AppPaths _paths;
    private readonly NohutoRepoScanCache _cache;
    private readonly NohutoRepoScanReportWriter _reportWriter;

    public NohutoRepoScanStore(AppPaths paths)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _cache = new NohutoRepoScanCache(paths);
        _reportWriter = new NohutoRepoScanReportWriter(paths);
    }

    public string JsonReportPath => _paths.NohutoAnalysisReportPath;

    public string MarkdownReportPath => _paths.NohutoAnalysisMarkdownPath;

    public void EnsureDirectories()
        => _paths.EnsureDirectories();

    public NohutoRepoScanState LoadCachedState()
        => _cache.Load();

    public void SaveState(NohutoRepoScanState state)
        => _cache.Save(state);

    public void SaveReport(NohutoRepoScanState state, IReadOnlyList<RepositoryScanPayload> repoScans)
        => _reportWriter.Save(state, repoScans);

    public NohutoRepoScanResult BuildResultFromState(NohutoRepoScanState state, bool usedCachedData)
        => NohutoRepoScanResultBuilder.BuildResult(
            state,
            usedCachedData,
            _paths.NohutoAnalysisReportPath,
            _paths.NohutoAnalysisMarkdownPath);

    public RepositoryScanPayload BuildFailureState(
        NohutoRepositoryDefinition definition,
        NohutoTrackedRepositoryState? previousState,
        string error)
        => NohutoRepoScanResultBuilder.BuildFailureState(definition, previousState, error);

    public static NohutoTrackedRepositoryState CloneState(
        NohutoRepositoryDefinition definition,
        NohutoTrackedRepositoryState source)
        => NohutoRepoScanResultBuilder.CloneState(definition, source);

    public static bool ShouldUseCachedState(NohutoRepoScanState state, TimeSpan? minimumRefreshInterval)
        => NohutoRepoScanCache.ShouldUseCachedState(state, minimumRefreshInterval);

    public static string BuildAggregateSummary(IReadOnlyList<NohutoTrackedRepositoryState> repositories)
        => NohutoRepoScanResultBuilder.BuildAggregateSummary(repositories);

    public static string BuildRepositorySummary(
        NohutoRepositoryStateKind stateKind,
        string latestSha,
        NohutoChangeAnalysis analysis)
        => NohutoRepoScanResultBuilder.BuildRepositorySummary(stateKind, latestSha, analysis);

    public static string BuildUnchangedSummary(NohutoChangeAnalysis analysis, string latestSha)
        => NohutoRepoScanResultBuilder.BuildUnchangedSummary(analysis, latestSha);

    public static int GetDefinitionOrder(string repoId)
        => NohutoRepoScanResultBuilder.GetDefinitionOrder(repoId);
}
