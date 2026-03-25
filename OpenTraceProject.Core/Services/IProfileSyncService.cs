using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenTraceProject.Core.Services;

public interface IProfileSyncService
{
    Task ExportProfileAsync(string filePath, string password, IEnumerable<string> enabledTweakIds);
    Task<IEnumerable<string>> ImportProfileAsync(string filePath, string password);
}
