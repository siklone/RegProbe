using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Infrastructure.Files;

public interface IFileSystemAccessor
{
    Task<bool> FileExistsAsync(string path, CancellationToken ct);
    Task MoveFileAsync(string sourcePath, string destinationPath, CancellationToken ct);
}
