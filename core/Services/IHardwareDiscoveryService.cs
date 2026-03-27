using System.Threading.Tasks;
using RegProbe.Core.Intelligence;

namespace RegProbe.Core.Services;

public interface IHardwareDiscoveryService
{
    Task<HardwareProfile> GetHardwareProfileAsync();
}
