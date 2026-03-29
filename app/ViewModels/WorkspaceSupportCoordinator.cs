using System.Collections.Generic;
using System.Linq;
using RegProbe.App.Services;

namespace RegProbe.App.ViewModels;

public sealed class WorkspaceSupportCoordinator
{
    private readonly TweakDocumentationLinker _documentationLinker = new();
    private readonly TweakProvenanceCatalogService _provenanceCatalogService = new();
    private readonly TweakEvidenceClassCatalogService _evidenceClassCatalogService = new();

    public void ApplyCatalogs(IEnumerable<TweakItemViewModel> tweaks)
    {
        var tweakList = (tweaks ?? Enumerable.Empty<TweakItemViewModel>()).ToList();
        _documentationLinker.Apply(tweakList);
        _provenanceCatalogService.Apply(tweakList);
        _evidenceClassCatalogService.Apply(tweakList);
    }
}
