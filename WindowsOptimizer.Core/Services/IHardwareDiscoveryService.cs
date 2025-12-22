using System.Threading.Tasks;
using WindowsOptimizer.Core.Intelligence;

namespace WindowsOptimizer.Core.Services;

public interface IHardwareDiscoveryService
{
    Task<HardwareProfile> GetHardwareProfileAsync();
}
