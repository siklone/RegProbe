using System.Collections.ObjectModel;
using RegProbe.Core;
using RegProbe.Core.Commands;
using RegProbe.Core.Files;
using RegProbe.Core.Registry;
using RegProbe.Core.Services;
using RegProbe.Core.Tasks;
using RegProbe.Engine;
using RegProbe.Engine.Services;

namespace RegProbe.App.ViewModels;

internal sealed class WorkspaceProviderTweakLoader
{
    private readonly IEnumerable<ITweakProvider>? _providerList;
    private readonly IRegistryAccessor _localRegistryAccessor;
    private readonly IRegistryAccessor _scanAwareElevatedRegistryAccessor;
    private readonly IServiceManager _elevatedServiceManager;
    private readonly IScheduledTaskManager _elevatedTaskManager;
    private readonly IFileSystemAccessor _elevatedFileSystemAccessor;
    private readonly ICommandRunner _elevatedCommandRunner;
    private readonly TweakExecutionPipeline _pipeline;
    private readonly bool _isElevated;

    public WorkspaceProviderTweakLoader(
        IEnumerable<ITweakProvider>? providerList,
        IRegistryAccessor localRegistryAccessor,
        IRegistryAccessor scanAwareElevatedRegistryAccessor,
        IServiceManager elevatedServiceManager,
        IScheduledTaskManager elevatedTaskManager,
        IFileSystemAccessor elevatedFileSystemAccessor,
        ICommandRunner elevatedCommandRunner,
        TweakExecutionPipeline pipeline,
        bool isElevated)
    {
        _providerList = providerList;
        _localRegistryAccessor = localRegistryAccessor ?? throw new ArgumentNullException(nameof(localRegistryAccessor));
        _scanAwareElevatedRegistryAccessor = scanAwareElevatedRegistryAccessor ?? throw new ArgumentNullException(nameof(scanAwareElevatedRegistryAccessor));
        _elevatedServiceManager = elevatedServiceManager ?? throw new ArgumentNullException(nameof(elevatedServiceManager));
        _elevatedTaskManager = elevatedTaskManager ?? throw new ArgumentNullException(nameof(elevatedTaskManager));
        _elevatedFileSystemAccessor = elevatedFileSystemAccessor ?? throw new ArgumentNullException(nameof(elevatedFileSystemAccessor));
        _elevatedCommandRunner = elevatedCommandRunner ?? throw new ArgumentNullException(nameof(elevatedCommandRunner));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _isElevated = isElevated;
    }

    public void Load(ObservableCollection<TweakItemViewModel> tweaks)
    {
        if (_providerList is null)
        {
            return;
        }

        var existingIds = WorkspaceTweakIdSetBuilder.Build(tweaks);
        var tweakContext = new TweakContext(
            _localRegistryAccessor,
            _scanAwareElevatedRegistryAccessor,
            _elevatedServiceManager,
            _elevatedTaskManager,
            _elevatedFileSystemAccessor,
            _elevatedCommandRunner);

        foreach (var provider in _providerList)
        {
            var providerTweaks = provider.CreateTweaks(_pipeline, tweakContext, _isElevated);
            foreach (var tweak in providerTweaks)
            {
                if (string.IsNullOrWhiteSpace(tweak.Id) || !existingIds.Add(tweak.Id))
                {
                    continue;
                }

                tweaks.Add(new TweakItemViewModel(tweak, _pipeline, _isElevated));
            }
        }
    }
}
