using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.App.HardwareDb;

public static class HardwareDbLoader
{
    public static Task LoadAllAsync(CancellationToken ct)
    {
        return HardwareKnowledgeDbService.Instance.InitializeAsync(ct);
    }
}
