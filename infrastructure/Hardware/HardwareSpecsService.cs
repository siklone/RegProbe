using System;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.Infrastructure.Data;

namespace RegProbe.Infrastructure.Hardware;

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

    public async Task<MotherboardSpecs> GetMotherboardSpecsAsync(MotherboardIdentity identity, CancellationToken ct)
    {
        if (identity == null) throw new ArgumentNullException(nameof(identity));

        var provider = new FallbackDataProvider<MotherboardSpecs>();
        if (_database != null)
        {
            provider.AddSource("db", token => _database.LookupMotherboardAsync(identity, token), priority: 10,
                timeout: TimeSpan.FromSeconds(2));
        }

        provider.AddSource("identity", () => MotherboardSpecs.FromIdentity(identity), priority: 0);

        var result = await provider.GetAsync(ct).ConfigureAwait(false);
        var specs = result.Value ?? MotherboardSpecs.FromIdentity(identity);
        return specs with { IsFromDatabase = string.Equals(result.Source, "db", StringComparison.OrdinalIgnoreCase) };
    }

    public async Task<StorageSpecs> GetStorageSpecsAsync(string model, CancellationToken ct)
    {
        var provider = new FallbackDataProvider<StorageSpecs>();
        if (_database != null)
        {
            provider.AddSource("db", token => _database.LookupStorageAsync(model, token), priority: 10,
                timeout: TimeSpan.FromSeconds(2));
        }

        provider.AddSource("identity", () => StorageSpecs.FromModel(model), priority: 0);

        var result = await provider.GetAsync(ct).ConfigureAwait(false);
        var specs = result.Value ?? StorageSpecs.FromModel(model);
        return specs with { IsFromDatabase = string.Equals(result.Source, "db", StringComparison.OrdinalIgnoreCase) };
    }

    public async Task<RamSpecs> GetRamSpecsAsync(RamModule module, CancellationToken ct)
    {
        if (module == null) throw new ArgumentNullException(nameof(module));

        var provider = new FallbackDataProvider<RamSpecs>();
        if (_database != null)
        {
            provider.AddSource("db", token => _database.LookupRamAsync(module, token), priority: 10,
                timeout: TimeSpan.FromSeconds(2));
        }

        provider.AddSource("identity", () => RamSpecs.FromModule(module), priority: 0);

        var result = await provider.GetAsync(ct).ConfigureAwait(false);
        var specs = result.Value ?? RamSpecs.FromModule(module);
        return specs with { IsFromDatabase = string.Equals(result.Source, "db", StringComparison.OrdinalIgnoreCase) };
    }
}
