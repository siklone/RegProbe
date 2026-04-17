namespace RegProbe.App.Services;

internal static class NohutoChangeClassifier
{
    public static string ResolveCategory(string repoId, string path)
        => NohutoRepositoryCategoryResolver.Resolve(repoId, path);

    public static NohutoChangeKind ResolveChangeKind(string path)
        => NohutoChangeKindResolver.Resolve(path);

    public static string NormalizePath(string path)
        => NohutoChangeKindResolver.NormalizePath(path);
}
