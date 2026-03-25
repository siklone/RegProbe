using System.Threading;
using System.Threading.Tasks;

namespace OpenTraceProject.Core.Commands;

public interface ICommandRunner
{
    Task<CommandResult> RunAsync(CommandRequest request, CancellationToken ct);
}
