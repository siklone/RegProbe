using RegProbe.Core.Commands;
using RegProbe.Core.Files;
using RegProbe.Core.Registry;
using RegProbe.Core.Tasks;

namespace RegProbe.Core.Services;

public sealed record TweakContext(
    IRegistryAccessor LocalRegistry,
    IRegistryAccessor ElevatedRegistry,
    IServiceManager ElevatedServiceManager,
    IScheduledTaskManager ElevatedTaskManager,
    IFileSystemAccessor ElevatedFileSystem,
    ICommandRunner ElevatedCommandRunner
);
