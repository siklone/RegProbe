using System.Threading.Tasks;

namespace OpenTraceProject.App.Services.OsDetection
{
    public interface IOsDetectionService
    {
        Task<OsInfo> DetectAsync(bool includeActivation = false);
    }
}
