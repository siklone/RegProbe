using System.Diagnostics;
using System.IO;
using RegProbe.App.Utilities;
using RegProbe.Core.Commands;
using RegProbe.Core.Files;
using RegProbe.Core.Registry;
using RegProbe.Core.Services;
using RegProbe.Core.Tasks;
using RegProbe.Infrastructure.Elevation;
using RegProbe.Infrastructure.Registry;
using RegProbe.Engine.Services;

namespace RegProbe.App.ViewModels;

internal sealed class TweaksWorkspaceInfrastructure
{
    private TweaksWorkspaceInfrastructure(
        string elevatedHostExecutablePath,
        bool isElevatedHostAvailable,
        IRegistryAccessor localRegistryAccessor,
        IRegistryAccessor scanAwareElevatedRegistryAccessor,
        IServiceManager elevatedServiceManager,
        IScheduledTaskManager elevatedTaskManager,
        IFileSystemAccessor elevatedFileSystemAccessor,
        ICommandRunner elevatedCommandRunner)
    {
        ElevatedHostExecutablePath = elevatedHostExecutablePath;
        IsElevatedHostAvailable = isElevatedHostAvailable;
        LocalRegistryAccessor = localRegistryAccessor;
        ScanAwareElevatedRegistryAccessor = scanAwareElevatedRegistryAccessor;
        ElevatedServiceManager = elevatedServiceManager;
        ElevatedTaskManager = elevatedTaskManager;
        ElevatedFileSystemAccessor = elevatedFileSystemAccessor;
        ElevatedCommandRunner = elevatedCommandRunner;
    }

    public string ElevatedHostExecutablePath { get; }

    public bool IsElevatedHostAvailable { get; }

    public IRegistryAccessor LocalRegistryAccessor { get; }

    public IRegistryAccessor ScanAwareElevatedRegistryAccessor { get; }

    public IServiceManager ElevatedServiceManager { get; }

    public IScheduledTaskManager ElevatedTaskManager { get; }

    public IFileSystemAccessor ElevatedFileSystemAccessor { get; }

    public ICommandRunner ElevatedCommandRunner { get; }

    public static string OverridePathEnvironmentVariable => ElevatedHostDefaults.OverridePathEnvVar;

    public static TweaksWorkspaceInfrastructure Create(bool isElevated)
    {
        var elevatedHostExecutablePath = ElevatedHostLocator.GetExecutablePath();
        var isElevatedHostAvailable = File.Exists(elevatedHostExecutablePath);
        var sessionToken = ElevatedHostDefaults.CreateSessionToken();
        var parentProcessId = Process.GetCurrentProcess().Id;
        var elevatedHostClient = new ElevatedHostClient(new ElevatedHostClientOptions
        {
            HostExecutablePath = elevatedHostExecutablePath,
            PipeName = ElevatedHostDefaults.GetPipeNameForProcess(parentProcessId, sessionToken),
            ParentProcessId = parentProcessId,
            SessionToken = sessionToken
        });

        var machineLocalRegistryAccessor = new LocalRegistryAccessor();
        var elevatedRegistryAccessor = new ElevatedRegistryAccessor(elevatedHostClient);
        var localRegistryAccessor = new RoutingRegistryAccessor(machineLocalRegistryAccessor, elevatedRegistryAccessor);
        var hybridRegistryAccessor = new HybridRegistryAccessor(machineLocalRegistryAccessor, elevatedRegistryAccessor);
        IRegistryAccessor scanAwareElevatedRegistryAccessor = isElevated ? elevatedRegistryAccessor : hybridRegistryAccessor;

        return new TweaksWorkspaceInfrastructure(
            elevatedHostExecutablePath,
            isElevatedHostAvailable,
            localRegistryAccessor,
            scanAwareElevatedRegistryAccessor,
            new ElevatedServiceManager(elevatedHostClient),
            new ElevatedScheduledTaskManager(elevatedHostClient),
            new ElevatedFileSystemAccessor(elevatedHostClient),
            new ElevatedCommandRunner(elevatedHostClient));
    }
}
