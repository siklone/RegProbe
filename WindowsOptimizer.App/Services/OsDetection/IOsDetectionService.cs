using System.Threading.Tasks;

namespace WindowsOptimizer.App.Services.OsDetection
{
    public interface IOsDetectionService
    {
        Task<OsInfo> DetectAsync(bool includeActivation = false);
    }
}
