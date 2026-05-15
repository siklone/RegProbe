namespace RegProbe.App.Services;

internal static class PublicEvidenceLinkPolicy
{
    public const string NoLocalSourceMessage =
        "No local source-code mirror is attached for this card. This is not a blocker when Docs, Runtime, and Rollback carry the proof.";

    public static bool IsSuppressedExternalPseudocodeUrl(string? url)
        => !string.IsNullOrWhiteSpace(url)
           && url.Contains("github.com/nohuto/decompiled-pseudocode", StringComparison.OrdinalIgnoreCase);

    public static string SanitizeSourceSummary(string? summary, int visibleSourceLinkCount)
    {
        var text = summary?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return visibleSourceLinkCount > 0
                ? "Source links are discovery and naming context only. Value semantics and apply safety come from Docs, Runtime, and Rollback."
                : NoLocalSourceMessage;
        }

        if (text.Contains("No upstream nohuto source link", StringComparison.OrdinalIgnoreCase))
        {
            return NoLocalSourceMessage;
        }

        if (text.Contains("Upstream dump / pseudocode links are attached", StringComparison.OrdinalIgnoreCase))
        {
            return "Source links are discovery and naming context only. Value semantics and apply safety come from Docs, Runtime, and Rollback.";
        }

        return text.Replace("nohuto", "external upstream", StringComparison.OrdinalIgnoreCase);
    }
}
