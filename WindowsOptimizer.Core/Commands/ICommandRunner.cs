using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Core.Commands;

public interface ICommandRunner
{
    Task<CommandResult> RunAsync(CommandRequest request, CancellationToken ct);
}
