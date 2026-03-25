using System.Diagnostics;
using System.Threading.Tasks;

namespace OpenTraceProject.App.Services.OsDetection;

public sealed class OsDetectionService : IOsDetectionService
{
    private readonly RegistryOsProvider _registryProvider = new();
    private readonly WmiOsProvider _wmiProvider = new();
    private readonly OsActivationChecker? _activationChecker;

    public OsDetectionService(OsActivationChecker? activationChecker = null)
    {
        _activationChecker = activationChecker;
    }

    public async Task<OsInfo> DetectAsync(bool includeActivation = false)
    {
        Debug.WriteLine("[OsDetectionService] Starting OS detection (Registry primary)");

        var osInfo = await _registryProvider.GetAsync();

        Debug.WriteLine($"[OsDetectionService] Registry result: Caption={osInfo.Caption}, Build={osInfo.BuildNumberRaw}");

        if (string.IsNullOrWhiteSpace(osInfo.Caption) || osInfo.BuildNumberRaw == 0)
        {
            Debug.WriteLine("[OsDetectionService] Registry incomplete, using fallback");
            ApplyFallbacks(osInfo);
        }

        await _wmiProvider.EnrichAsync(osInfo);

        Debug.WriteLine($"[OsDetectionService] After WMI enrichment: Source={osInfo.Source}, Architecture={osInfo.Architecture}");

        if (includeActivation && _activationChecker != null)
        {
            try
            {
                osInfo.ActivationStatus = await _activationChecker.GetActivationStatusAsync();
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"[OsDetectionService] Activation check failed: {ex.Message}");
                osInfo.ActivationStatus = "Unknown";
            }
        }

        Debug.WriteLine($"[OsDetectionService] Final: NormalizedName={osInfo.NormalizedName}, IconKey={osInfo.IconKey}");

        return osInfo;
    }

    private void ApplyFallbacks(OsInfo osInfo)
    {
        if (osInfo.BuildNumberRaw == 0)
        {
            osInfo.BuildNumberRaw = System.Environment.OSVersion.Version.Build;
            osInfo.BuildNumber = osInfo.BuildNumberRaw.ToString();
            if (osInfo.Source == "Registry")
            {
                osInfo.Source = "Registry+API";
            }
            else
            {
                osInfo.Source = "API";
            }
        }

        if (string.IsNullOrWhiteSpace(osInfo.Caption))
        {
            osInfo.Caption = RegistryOsProvider.BuildCaptionFromBuildNumber(osInfo.BuildNumberRaw, osInfo.EditionId);
        }

        if (string.IsNullOrWhiteSpace(osInfo.Architecture))
        {
            osInfo.Architecture = System.Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
        }
    }
}
