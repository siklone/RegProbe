namespace RegProbe.App.Services;

public static class NohutoChangeAnalyzer
{
    // Public facade kept intentionally small. Delegates heavy work to the
    // internal NohutoChangeEngine so analysis logic can be tested and evolved
    // independently and the public surface remains stable.
    public static NohutoChangeAnalysis Analyze(IEnumerable<NohutoChangedFile> changedFiles)
        => Analyze("win-registry", changedFiles);

    public static NohutoChangeAnalysis Analyze(string repoId, IEnumerable<NohutoChangedFile> changedFiles)
    {
        var definition = NohutoConfigurationSourceCatalog.Get(repoId);
        return Analyze(definition, changedFiles);
    }

    public static NohutoChangeAnalysis Analyze(NohutoRepositoryDefinition repository, IEnumerable<NohutoChangedFile> changedFiles)
        => NohutoChangeEngine.Analyze(repository, changedFiles);
}
