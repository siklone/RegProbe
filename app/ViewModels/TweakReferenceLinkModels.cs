namespace RegProbe.App.ViewModels;

/// <summary>
/// A reference link for documentation or sources.
/// </summary>
public sealed class ReferenceLink
{
    public ReferenceLink(string title, string url, string? tooltip = null, ReferenceLinkKind kind = ReferenceLinkKind.Other)
    {
        Title = title;
        Url = url;
        Tooltip = string.IsNullOrWhiteSpace(tooltip) ? url : tooltip;
        Kind = kind;
    }

    public string Title { get; }

    public string Url { get; }

    public string Tooltip { get; }

    public ReferenceLinkKind Kind { get; }

    public string Icon => Kind switch
    {
        ReferenceLinkKind.Catalog => "CAT",
        ReferenceLinkKind.Details => "DET",
        ReferenceLinkKind.Docs => "DOC",
        ReferenceLinkKind.Source => "SRC",
        _ => "REF"
    };
}

public enum ReferenceLinkKind
{
    Catalog,
    Details,
    Docs,
    Source,
    Other
}
