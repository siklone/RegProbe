using System.Threading.Tasks;
using OpenTraceProject.Core.Intelligence;

namespace OpenTraceProject.Core.Services;

public interface IHardwareDiscoveryService
{
    Task<HardwareProfile> GetHardwareProfileAsync();
}
