namespace RegProbe.App.Services;

internal static class PublicEvidenceLinkPolicy
{
    public const string NoLocalSourceMessage =
        "No local source-code mirror or pseudocode evidence is attached for this card. Catalog-only source context is not a value-semantics proof; Docs, Runtime, and Rollback carry the app-safety proof.";

    public static bool IsSuppressedExternalPseudocodeUrl(string? url)
        => !string.IsNullOrWhiteSpace(url)
           && url.Contains("github.com/nohuto/decompiled-pseudocode", StringComparison.OrdinalIgnoreCase);

    public static string SanitizeSourceSummary(string? summary, int visibleSourceLinkCount)
    {
        var text = summary?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return visibleSourceLinkCount > 0
                ? "Source links are discovery and naming context only. They are not value-semantics proof; app safety comes from Docs, Runtime, and Rollback."
                : NoLocalSourceMessage;
        }

        if (text.Contains("No upstream nohuto source link", StringComparison.OrdinalIgnoreCase))
        {
            return NoLocalSourceMessage;
        }

        if (text.Contains("Upstream dump / pseudocode links are attached", StringComparison.OrdinalIgnoreCase))
        {
            return visibleSourceLinkCount > 0
                ? "Source links are discovery and naming context only. They are not value-semantics proof; app safety comes from Docs, Runtime, and Rollback."
                : NoLocalSourceMessage;
        }

        return text.Replace("nohuto", "external upstream", StringComparison.OrdinalIgnoreCase);
    }
}
