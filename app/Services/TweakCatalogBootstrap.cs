using System.Diagnostics;
using System.IO;
using RegProbe.Application.Utilities;
using RegProbe.Core.Services;
using RegProbe.Engine;
using RegProbe.Engine.Services;
using RegProbe.Infrastructure;
using RegProbe.Infrastructure.Elevation;
using RegProbe.Infrastructure.Registry;

namespace RegProbe.Application.Services;

internal sealed record TweakCatalogBootstrapResult(
    TweakExecutionPipeline Pipeline,
    TweakContext Context,
    bool IsElevated,
    bool IsElevatedHostAvailable,
    string ElevatedHostPath);

internal static class TweakCatalogBootstrap
{
    public static TweakCatalogBootstrapResult Create()
    {
        var paths = AppPaths.FromEnvironment();
        paths.EnsureDirectories();
        var logger = new FileAppLogger(paths);
        var logStore = new FileTweakLogStore(paths);
        var rollbackStore = new RollbackStateStore(paths);
        var pipeline = new TweakExecutionPipeline(logger, logStore, rollbackStore);

        var isElevated = ProcessElevation.IsElevated();
        var elevatedHostPath = ElevatedHostLocator.GetExecutablePath();
        var isElevatedHostAvailable = File.Exists(elevatedHostPath);
        var sessionToken = ElevatedHostDefaults.CreateSessionToken();
        var parentProcessId = Process.GetCurrentProcess().Id;

        var elevatedHostClient = new ElevatedHostClient(new ElevatedHostClientOptions
        {
            HostExecutablePath = elevatedHostPath,
            PipeName = ElevatedHostDefaults.GetPipeNameForProcess(parentProcessId, sessionToken),
            ParentProcessId = parentProcessId,
            SessionToken = sessionToken
        });

        var elevatedRegistryAccessor = new ElevatedRegistryAccessor(elevatedHostClient);
        var context = new TweakContext(
            new RoutingRegistryAccessor(new LocalRegistryAccessor(), elevatedRegistryAccessor),
            elevatedRegistryAccessor,
            new ElevatedServiceManager(elevatedHostClient),
            new ElevatedScheduledTaskManager(elevatedHostClient),
            new ElevatedFileSystemAccessor(elevatedHostClient),
            new ElevatedCommandRunner(elevatedHostClient));

        return new TweakCatalogBootstrapResult(
            pipeline,
            context,
            isElevated,
            isElevatedHostAvailable,
            elevatedHostPath);
    }
}
