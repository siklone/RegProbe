using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Infrastructure.Commands;

public interface ICommandRunner
{
    Task<CommandResult> RunAsync(CommandRequest request, CancellationToken ct);
}
