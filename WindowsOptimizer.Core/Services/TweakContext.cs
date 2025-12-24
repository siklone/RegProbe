using WindowsOptimizer.Core.Files;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Tasks;

namespace WindowsOptimizer.Core.Services;

public sealed record TweakContext(
    IRegistryAccessor LocalRegistry,
    IRegistryAccessor ElevatedRegistry,
    IServiceManager ElevatedServiceManager,
    IScheduledTaskManager ElevatedTaskManager,
    IFileSystemAccessor ElevatedFileSystem
);
