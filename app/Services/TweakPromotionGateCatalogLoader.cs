using System.Text.Json;

namespace RegProbe.Application.Services;

internal sealed class TweakPromotionGateCatalogLoader
{
    private readonly TweakPromotionGatePathResolver _pathResolver;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public TweakPromotionGateCatalogLoader(TweakPromotionGatePathResolver pathResolver)
    {
        _pathResolver = pathResolver ?? throw new ArgumentNullException(nameof(pathResolver));
    }

    public TweakPromotionGateCatalog LoadCatalog(string relativePath)
    {
        var path = _pathResolver.ResolvePath(relativePath);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new TweakPromotionGateCatalog();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<TweakPromotionGateCatalog>(json, JsonOptions)
                   ?? new TweakPromotionGateCatalog();
        }
        catch
        {
            return new TweakPromotionGateCatalog();
        }
    }

    public BlockedWorklistCatalog LoadBlockedWorklist(string relativePath)
    {
        var path = _pathResolver.ResolvePath(relativePath);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new BlockedWorklistCatalog();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<BlockedWorklistCatalog>(json, JsonOptions)
                   ?? new BlockedWorklistCatalog();
        }
        catch
        {
            return new BlockedWorklistCatalog();
        }
    }
}
