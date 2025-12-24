using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Core.Files;

public interface IFileSystemAccessor
{
    Task<bool> FileExistsAsync(string path, CancellationToken ct);
    Task MoveFileAsync(string sourcePath, string destinationPath, CancellationToken ct);
}
