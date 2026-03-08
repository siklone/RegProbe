using WindowsOptimizer.App.HardwareDb;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.Diagnostics;
using BaseViewModel = WindowsOptimizer.App.ViewModels.Base.HardwareDetailViewModelBase;

namespace WindowsOptimizer.App.ViewModels.Hardware;

public sealed class OsDetailViewModel : BaseViewModel
{
    public override HardwareType HardwareType => HardwareType.Os;

    public OsDetailViewModel()
    {
        Title = "Operating System";
        Subtitle = "Windows";
    }

    public override void LoadFromCache()
    {
        var os = Cache.Get<OsHardwareData>("OS");
        AppDiagnostics.Log($"[OsDetailVM] Cache raw: NormalizedName={os?.NormalizedName ?? "<null>"}, ProductName={os?.ProductName ?? "<null>"}, BuildNumber={os?.BuildNumber.ToString() ?? "<null>"}");
        // Consider null or empty OS data a cache miss to allow fallback preload.
        if (os == null || (string.IsNullOrWhiteSpace(os.NormalizedName) && string.IsNullOrWhiteSpace(os.ProductName) && os.BuildNumber <= 0))
        {
            Title = "Operating System";
            Subtitle = "No data available";
            AddRow("Error", "OS data not loaded");
            AppDiagnostics.Log("[OsDetailVM] Cache miss or trivial entry");
            SetLoadingComplete();
            return;
        }

            // Mark that we successfully loaded from cache so the base fallback won't run
        WasCacheHit = true;
        AppDiagnostics.Log("[OsDetailVM] Cache hit — populating properties from cache");

            Title = "Operating System";
            Subtitle = os.NormalizedName ?? os.ProductName ?? "Windows";

        ClearSpecs();

        AddHeader("System");
        AddRow("Product Name", os.ProductName);
        AddRow("Edition", os.Edition);
        AddRow("Version", os.Version);
        AddRow("Display Version", os.DisplayVersion);
        AddRow("Release ID", os.ReleaseId);
        AddRow("Build Number", os.BuildNumber > 0 ? os.BuildNumber.ToString() : null);
        AddRowIf("UBR", os.Ubr > 0 ? os.Ubr.ToString() : null);
        AddRow("Architecture", os.Architecture);
        AddRow("Install Date", os.InstallDate);
        AddRow("Install Type", os.InstallType);

        AddHeader("Security");
        AddRow("BIOS Mode", os.BiosMode);
        AddRow("Secure Boot", os.SecureBootState);
        AddRow("TPM Version", os.TpmVersion);
        AddRow("BitLocker", os.BitLockerStatus);
        AddRow("Device Guard", os.DeviceGuardStatus);
        AddRow("Credential Guard", os.CredentialGuardStatus);
        AddRow("Virtualization", os.VirtualizationEnabled);
        AddRow("Hyper-V", os.HyperVInstalled);
        AddRow("Defender", os.DefenderStatus);
        AddRow("Firewall", os.FirewallStatus);

        AddHeader("Runtime");
        AddRow("Username", os.Username);
        AddRow("Registered Owner", os.RegisteredOwner);
        AddRow("Organization", os.RegisteredOrganization);
        AddRow("Last Boot", os.LastBootTime);
        AddRow("Uptime", os.Uptime);
        AddRowIf("Experience Index", os.WindowsExperienceIndex);

        ResolveIcon("Microsoft", os.BuildNumber > 0 ? os.BuildNumber.ToString() : null);
        AppDiagnostics.Log($"[OsDetailVM] After populate: Title={Title}, Subtitle={Subtitle}, SpecsCount={Specs.Count}");
        SetLoadingComplete();
    }
}
