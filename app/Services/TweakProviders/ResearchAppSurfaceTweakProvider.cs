using RegProbe.Application.Utilities;
using RegProbe.Core;
using RegProbe.Core.Services;
using RegProbe.Engine;

namespace RegProbe.Application.Services.TweakProviders;

public sealed class ResearchAppSurfaceTweakProvider : BaseTweakProvider
{
    private const string SurfaceRelativePath = "research/app-surface";

    public override string CategoryName => "Research App Surface";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        var surfaceDirectory = ResolveSurfaceDirectory();
        if (string.IsNullOrWhiteSpace(surfaceDirectory) || !Directory.Exists(surfaceDirectory))
        {
            yield break;
        }

        using var loader = new JsonTweakLoader(surfaceDirectory, preserveEntryIds: true);
        foreach (var tweak in loader.CreateTweaks(context.LocalRegistry))
        {
            yield return tweak;
        }
    }

    private static string? ResolveSurfaceDirectory()
    {
        var docsRoot = DocsLocator.TryFindDocsRoot();
        if (string.IsNullOrWhiteSpace(docsRoot))
        {
            return null;
        }

        return Path.Combine(docsRoot, SurfaceRelativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
