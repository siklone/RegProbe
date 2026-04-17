using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RegProbe.Core;
using RegProbe.Core.Commands;
using RegProbe.Core.Files;
using RegProbe.Core.Registry;
using RegProbe.Core.Services;
using RegProbe.Core.Tasks;
using RegProbe.Engine;
using RegProbe.Engine.Services;
using RegProbe.Infrastructure;

namespace RegProbe.App.ViewModels;

public sealed class WorkspaceCatalogCoordinator
{
    private readonly WorkspaceProviderTweakLoader _providerTweakLoader;
    private readonly WorkspacePluginTweakLoader _pluginTweakLoader;
    private readonly WorkspaceTweakMetadataApplier _metadataApplier = new();
    private readonly WinConfigCategoryCoverageMapper _coverageMapper = new();

    public WorkspaceCatalogCoordinator(
        IEnumerable<ITweakProvider>? providerList,
        IRegistryAccessor localRegistryAccessor,
        IRegistryAccessor scanAwareElevatedRegistryAccessor,
        IServiceManager elevatedServiceManager,
        IScheduledTaskManager elevatedTaskManager,
        IFileSystemAccessor elevatedFileSystemAccessor,
        ICommandRunner elevatedCommandRunner,
        TweakExecutionPipeline pipeline,
        bool isElevated,
        IAppLogger appLogger)
    {
        _providerTweakLoader = new WorkspaceProviderTweakLoader(
            providerList,
            localRegistryAccessor,
            scanAwareElevatedRegistryAccessor,
            elevatedServiceManager,
            elevatedTaskManager,
            elevatedFileSystemAccessor,
            elevatedCommandRunner,
            pipeline,
            isElevated);
        _pluginTweakLoader = new WorkspacePluginTweakLoader(
            pipeline,
            isElevated,
            appLogger);
    }

    public void LoadInitialTweaks(ObservableCollection<TweakItemViewModel> tweaks)
    {
        ArgumentNullException.ThrowIfNull(tweaks);
        _providerTweakLoader.Load(tweaks);
        _pluginTweakLoader.Load(tweaks);
        _metadataApplier.Apply(tweaks);
    }

    public IDictionary<string, int> BuildWinConfigCategoryCoverageMap(IEnumerable<TweakItemViewModel> tweaks)
        => _coverageMapper.Build(tweaks);
}
