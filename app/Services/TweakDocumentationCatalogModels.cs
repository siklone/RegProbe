using System.Text.RegularExpressions;

namespace RegProbe.App.Services;

internal sealed record TweakDocumentationCatalogEntry(
    string Id,
    string? Category,
    string? SourcePath,
    string? DocsPath);

internal sealed record TweakDocumentationTemplateCatalogEntry(
    string TemplateId,
    Regex Pattern,
    TweakDocumentationCatalogEntry Entry);
