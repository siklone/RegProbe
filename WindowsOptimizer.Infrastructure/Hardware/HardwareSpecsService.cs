using System;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Infrastructure.Data;

namespace WindowsOptimizer.Infrastructure.Hardware;

public sealed class HardwareSpecsService
{
    private readonly HardwareDatabase? _database;

    public HardwareSpecsService(HardwareDatabase? database = null)
    {
        _database = database ?? (HardwareDatabase.TryGetInstance(out var db) ? db : null);
    }

    public async Task<CpuSpecs> GetCpuSpecsAsync(CpuIdentity identity, CancellationToken ct)
    {
        if (identity == null) throw new ArgumentNullException(nameof(identity));

        var provider = new FallbackDataProvider<CpuSpecs>();
        if (_database != null)
        {
            provider.AddSource("db", token => _database.LookupCpuAsync(identity, token), priority: 10,
                timeout: TimeSpan.FromSeconds(2));
        }

        provider.AddSource("identity", () => CpuSpecs.FromIdentity(identity), priority: 0);

        var result = await provider.GetAsync(ct).ConfigureAwait(false);
        var specs = result.Value ?? CpuSpecs.FromIdentity(identity);
        return specs with { IsFromDatabase = string.Equals(result.Source, "db", StringComparison.OrdinalIgnoreCase) };
    }

    public async Task<GpuSpecs> GetGpuSpecsAsync(GpuIdentity identity, CancellationToken ct)
    {
        if (identity == null) throw new ArgumentNullException(nameof(identity));

        var provider = new FallbackDataProvider<GpuSpecs>();
        if (_database != null)
        {
            provider.AddSource("db", token => _database.LookupGpuAsync(identity, token), priority: 10,
                timeout: TimeSpan.FromSeconds(2));
        }

        provider.AddSource("identity", () => GpuSpecs.FromIdentity(identity), priority: 0);

        var result = await provider.GetAsync(ct).ConfigureAwait(false);
        var specs = result.Value ?? GpuSpecs.FromIdentity(identity);
        return specs with { IsFromDatabase = string.Equals(result.Source, "db", StringComparison.OrdinalIgnoreCase) };
    }
}
