using System.Diagnostics;
using System.Text.Json;

namespace RegProbe.Application.Services;

internal sealed class TweakPromotionGateAuditLogWriter
{
    private readonly string? _repoRoot;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public TweakPromotionGateAuditLogWriter(string? repoRoot)
    {
        _repoRoot = repoRoot;
    }

    public bool TryAppend(
        string relativePath,
        string action,
        TweakMutationDecision decision,
        bool contributorMode,
        out string? error)
    {
        error = null;

        try
        {
            if (string.IsNullOrWhiteSpace(_repoRoot))
            {
                return true;
            }

            var path = Path.Combine(_repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var payload = new
            {
                timestamp_utc = DateTimeOffset.UtcNow.ToString("O"),
                action,
                candidate_id = decision.Entry.CandidateId,
                tweak_id = decision.Entry.TweakId,
                promotion_state = decision.Entry.PromotionState,
                override_requested = decision.OverrideRequested,
                override_used = decision.OverrideUsed,
                override_reason = string.IsNullOrWhiteSpace(decision.OverrideReason) ? "unspecified" : decision.OverrideReason,
                contributor_mode = contributorMode,
                allowed = decision.Allowed,
                message = decision.Message,
                warnings = decision.Warnings,
            };

            File.AppendAllText(path, JsonSerializer.Serialize(payload, JsonOptions) + Environment.NewLine);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            Debug.WriteLine($"TweakPromotionGateCatalogService: failed to append mutation audit log: {ex}");
            return false;
        }
    }
}
