using OpenTraceProject.Core.Commands;
using OpenTraceProject.Core.Files;
using OpenTraceProject.Core.Registry;
using OpenTraceProject.Core.Tasks;

namespace OpenTraceProject.Core.Services;

public sealed record TweakContext(
    IRegistryAccessor LocalRegistry,
    IRegistryAccessor ElevatedRegistry,
    IServiceManager ElevatedServiceManager,
    IScheduledTaskManager ElevatedTaskManager,
    IFileSystemAccessor ElevatedFileSystem,
    ICommandRunner ElevatedCommandRunner
);
