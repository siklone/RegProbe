namespace RegProbe.App.Services;

internal static class PublicEvidenceLinkPolicy
{
    public const string NoLocalSourceMessage =
        "No RegProbe-controlled local source mirror or pseudocode evidence is attached for this card. Catalog-only source context is not a value-semantics proof; the catalog index is naming/navigation context only. Docs, Runtime, and Rollback carry the app-safety proof.";

    public static bool IsSuppressedExternalPseudocodeUrl(string? url)
        => !string.IsNullOrWhiteSpace(url)
           && url.Contains("github.com/nohuto/decompiled-pseudocode", StringComparison.OrdinalIgnoreCase);

    public static bool IsSuppressedExternalSourceUrl(string? url)
        => IsSuppressedExternalPseudocodeUrl(url)
           || (!string.IsNullOrWhiteSpace(url)
               && url.Contains("github.com/nohuto/", StringComparison.OrdinalIgnoreCase));

    public static string SanitizeSourceSummary(string? summary, int visibleSourceLinkCount)
    {
        var text = summary?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return visibleSourceLinkCount > 0
                ? "Local source/pseudocode links provide discovery and naming context. They are not value-semantics proof by themselves; app safety comes from Docs, Runtime, and Rollback."
                : NoLocalSourceMessage;
        }

        if (text.Contains("No upstream nohuto source link", StringComparison.OrdinalIgnoreCase))
        {
            return NoLocalSourceMessage;
        }

        if (text.Contains("Upstream dump / pseudocode links are attached", StringComparison.OrdinalIgnoreCase))
        {
            return visibleSourceLinkCount > 0
                ? "Local source/pseudocode links provide discovery and naming context. They are not value-semantics proof by themselves; app safety comes from Docs, Runtime, and Rollback."
                : NoLocalSourceMessage;
        }

        if (visibleSourceLinkCount <= 0
            && (text.Contains("nohuto", StringComparison.OrdinalIgnoreCase)
                || text.Contains("upstream dump", StringComparison.OrdinalIgnoreCase)
                || text.Contains("pseudocode", StringComparison.OrdinalIgnoreCase)
                || text.Contains("upstream documentation", StringComparison.OrdinalIgnoreCase)))
        {
            return NoLocalSourceMessage;
        }

        return text.Replace("nohuto", "external upstream", StringComparison.OrdinalIgnoreCase);
    }
}
