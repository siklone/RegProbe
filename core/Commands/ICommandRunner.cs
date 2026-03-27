using System.Threading;
using System.Threading.Tasks;

namespace RegProbe.Core.Commands;

public interface ICommandRunner
{
    Task<CommandResult> RunAsync(CommandRequest request, CancellationToken ct);
}
