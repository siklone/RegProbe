using System.Threading.Tasks;

namespace RegProbe.App.Services.OsDetection
{
    public interface IOsDetectionService
    {
        Task<OsInfo> DetectAsync(bool includeActivation = false);
    }
}
