namespace RegProbe.App.Services;

public static class NohutoChangeAnalyzer
{
    public static NohutoChangeAnalysis Analyze(IEnumerable<NohutoChangedFile> changedFiles)
        => Analyze("win-registry", changedFiles);

    public static NohutoChangeAnalysis Analyze(string repoId, IEnumerable<NohutoChangedFile> changedFiles)
    {
        var definition = NohutoConfigurationSourceCatalog.Get(repoId);
        return Analyze(definition, changedFiles);
    }

    public static NohutoChangeAnalysis Analyze(NohutoRepositoryDefinition repository, IEnumerable<NohutoChangedFile> changedFiles)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(changedFiles);

        var files = changedFiles
            .Where(static file => file is not null && !string.IsNullOrWhiteSpace(file.Path))
            .ToList();

        var scoreByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var fileCountByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var docsCount = 0;
        var scriptCount = 0;
        var sourceCount = 0;
        var assetCount = 0;
        var dataCount = 0;

        foreach (var file in files)
        {
            var normalizedPath = NohutoChangeClassifier.NormalizePath(file.Path);
            switch (NohutoChangeClassifier.ResolveChangeKind(normalizedPath))
            {
                case NohutoChangeKind.Documentation:
                    docsCount++;
                    break;
                case NohutoChangeKind.Script:
                    scriptCount++;
                    break;
                case NohutoChangeKind.Source:
                    sourceCount++;
                    break;
                case NohutoChangeKind.Asset:
                    assetCount++;
                    break;
                default:
                    dataCount++;
                    break;
            }

            var category = NohutoChangeClassifier.ResolveCategory(repository.Id, normalizedPath);
            var weight = Math.Max(1, file.Additions + file.Deletions);
            if (!scoreByCategory.ContainsKey(category))
            {
                scoreByCategory[category] = 0;
                fileCountByCategory[category] = 0;
            }

            scoreByCategory[category] += weight;
            fileCountByCategory[category]++;
        }

        var topCategories = scoreByCategory
            .Select(pair => new NohutoCategoryInsight
            {
                Category = pair.Key,
                Score = pair.Value,
                FileCount = fileCountByCategory[pair.Key]
            })
            .OrderByDescending(static item => item.Score)
            .ThenByDescending(static item => item.FileCount)
            .Take(5)
            .ToList();

        return new NohutoChangeAnalysis
        {
            TotalChangedFiles = files.Count,
            DocumentationChangedFiles = docsCount,
            ScriptChangedFiles = scriptCount,
            SourceChangedFiles = sourceCount,
            AssetChangedFiles = assetCount,
            DataChangedFiles = dataCount,
            TopCategories = topCategories
        };
    }
}
