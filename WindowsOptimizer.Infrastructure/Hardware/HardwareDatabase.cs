using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using WindowsOptimizer.Infrastructure;

namespace WindowsOptimizer.Infrastructure.Hardware;

public sealed class HardwareDatabase
{
    private static readonly SemaphoreSlim SchemaLock = new(1, 1);
    private static bool _schemaReady;
    private static HardwareDatabase? _instance;
    private static readonly JsonSerializerOptions SeedJsonOptions = new() { PropertyNameCaseInsensitive = true };
    private const string SeedResourceName = "WindowsOptimizer.Infrastructure.Hardware.hardware-seed.json";
    private const string SeedVersionKey = "seed_version";
    private const string SeedAppliedKey = "seed_applied";

    private readonly string _dbPath;

    private HardwareDatabase(string dbPath)
    {
        _dbPath = dbPath;
    }

    public static HardwareDatabase Instance => _instance
        ?? throw new InvalidOperationException("Hardware database is not initialized.");

    public static bool TryGetInstance(out HardwareDatabase? database)
    {
        database = _instance;
        return database != null;
    }

    public static Task<HardwareDatabase> InitializeAsync(CancellationToken ct)
    {
        var paths = AppPaths.FromEnvironment();
        return InitializeAsync(paths, ct);
    }

    public static async Task<HardwareDatabase> InitializeAsync(AppPaths paths, CancellationToken ct)
    {
        if (paths == null) throw new ArgumentNullException(nameof(paths));

        var dbPath = paths.HardwareDatabasePath;
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var database = new HardwareDatabase(dbPath);
        await database.EnsureSchemaAsync(ct).ConfigureAwait(false);
        await database.EnsureSeedDataAsync(ct).ConfigureAwait(false);
        _instance = database;
        return database;
    }

    public async Task<CpuSpecs?> LookupCpuAsync(CpuIdentity identity, CancellationToken ct)
    {
        if (identity == null) throw new ArgumentNullException(nameof(identity));

        if (!string.IsNullOrWhiteSpace(identity.ProcessorId))
        {
            var byId = await QueryCpuAsync(
                "SELECT * FROM cpu_specs WHERE cpuid = @cpuid LIMIT 1",
                ct,
                new SqliteParameter("@cpuid", identity.ProcessorId)).ConfigureAwait(false);
            if (byId != null) return byId;
        }

        var brand = identity.WmiName ?? identity.LookupKey;
        if (!string.IsNullOrWhiteSpace(brand))
        {
            var normalized = NormalizeCpuBrand(brand);
            var byBrand = await QueryCpuAsync(
                "SELECT * FROM cpu_specs WHERE brand LIKE @brand LIMIT 1",
                ct,
                new SqliteParameter("@brand", $"%{normalized}%")).ConfigureAwait(false);
            if (byBrand != null) return byBrand;

            foreach (var token in ExtractCpuTokens(normalized))
            {
                var byToken = await QueryCpuAsync(
                    "SELECT * FROM cpu_specs WHERE brand LIKE @brand LIMIT 1",
                    ct,
                    new SqliteParameter("@brand", $"%{token}%")).ConfigureAwait(false);
                if (byToken != null) return byToken;
            }
        }

        return null;
    }

    public async Task<GpuSpecs?> LookupGpuAsync(GpuIdentity identity, CancellationToken ct)
    {
        if (identity == null) throw new ArgumentNullException(nameof(identity));

        if (!string.IsNullOrWhiteSpace(identity.PciId))
        {
            var byDevice = await QueryGpuAsync(
                "SELECT * FROM gpu_specs WHERE device_id = @device LIMIT 1",
                ct,
                new SqliteParameter("@device", identity.PciId)).ConfigureAwait(false);
            if (byDevice != null) return byDevice;
        }

        var brand = identity.WmiName ?? identity.DriverDesc ?? identity.LookupKey;
        if (!string.IsNullOrWhiteSpace(brand))
        {
            var byBrand = await QueryGpuAsync(
                "SELECT * FROM gpu_specs WHERE brand LIKE @brand LIMIT 1",
                ct,
                new SqliteParameter("@brand", $"%{brand}%")).ConfigureAwait(false);
            if (byBrand != null) return byBrand;
        }

        return null;
    }

    public async Task<bool> CheckForUpdatesAsync(CancellationToken ct)
    {
        await EnsureSchemaAsync(ct).ConfigureAwait(false);
        return await EnsureSeedDataAsync(ct).ConfigureAwait(false);
    }

    private async Task EnsureSchemaAsync(CancellationToken ct)
    {
        if (_schemaReady)
        {
            return;
        }

        await SchemaLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_schemaReady)
            {
                return;
            }

            await using var connection = CreateConnection();
            await connection.OpenAsync(ct).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = SchemaSql;
            await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            _schemaReady = true;
        }
        finally
        {
            SchemaLock.Release();
        }
    }

    private async Task<bool> EnsureSeedDataAsync(CancellationToken ct)
    {
        var seed = await LoadSeedAsync(ct).ConfigureAwait(false);
        if (seed == null || string.IsNullOrWhiteSpace(seed.Version))
        {
            return false;
        }

        var currentVersion = await GetMetadataValueAsync(SeedVersionKey, ct).ConfigureAwait(false);
        var hasData = await HasSeedDataAsync(ct).ConfigureAwait(false);
        if (hasData && string.Equals(currentVersion, seed.Version, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        await ApplySeedAsync(seed, ct).ConfigureAwait(false);
        await SetMetadataValueAsync(SeedVersionKey, seed.Version, ct).ConfigureAwait(false);
        await SetMetadataValueAsync(SeedAppliedKey, DateTime.UtcNow.ToString("O"), ct).ConfigureAwait(false);
        await SetMetadataValueAsync("last_update", DateTime.UtcNow.ToString("O"), ct).ConfigureAwait(false);
        return true;
    }

    private async Task<HardwareDatabaseSeed?> LoadSeedAsync(CancellationToken ct)
    {
        var assembly = typeof(HardwareDatabase).Assembly;
        await using var stream = assembly.GetManifestResourceStream(SeedResourceName);
        if (stream == null)
        {
            return null;
        }

        return await JsonSerializer.DeserializeAsync<HardwareDatabaseSeed>(stream, SeedJsonOptions, ct)
            .ConfigureAwait(false);
    }

    private async Task<bool> HasSeedDataAsync(CancellationToken ct)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct).ConfigureAwait(false);

        await using var cpuCommand = connection.CreateCommand();
        cpuCommand.CommandText = "SELECT COUNT(1) FROM cpu_specs";
        var cpuCount = Convert.ToInt32(await cpuCommand.ExecuteScalarAsync(ct).ConfigureAwait(false));
        if (cpuCount > 0)
        {
            return true;
        }

        await using var gpuCommand = connection.CreateCommand();
        gpuCommand.CommandText = "SELECT COUNT(1) FROM gpu_specs";
        var gpuCount = Convert.ToInt32(await gpuCommand.ExecuteScalarAsync(ct).ConfigureAwait(false));
        return gpuCount > 0;
    }

    private async Task<string?> GetMetadataValueAsync(string key, CancellationToken ct)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM db_metadata WHERE key = @key LIMIT 1";
        command.Parameters.Add(new SqliteParameter("@key", key));
        var result = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return result?.ToString();
    }

    private async Task SetMetadataValueAsync(string key, string value, CancellationToken ct)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR REPLACE INTO db_metadata (key, value) VALUES (@key, @value)";
        command.Parameters.Add(new SqliteParameter("@key", key));
        command.Parameters.Add(new SqliteParameter("@value", value));
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private async Task ApplySeedAsync(HardwareDatabaseSeed seed, CancellationToken ct)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct).ConfigureAwait(false);

        using var transaction = connection.BeginTransaction();
        foreach (var cpu in seed.Cpu ?? new List<CpuSpecs>())
        {
            if (string.IsNullOrWhiteSpace(cpu.Cpuid) || string.IsNullOrWhiteSpace(cpu.Brand))
            {
                continue;
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
INSERT OR REPLACE INTO cpu_specs (
    cpuid, brand, codename, cores, threads, base_clock_mhz, boost_clock_mhz, tdp_watts,
    cache_l2_kb, cache_l3_kb, lithography_nm, release_date, socket, architecture, features,
    max_memory_gb, memory_channels, pcie_lanes, integrated_gpu, updated_at
) VALUES (
    @cpuid, @brand, @codename, @cores, @threads, @base_clock_mhz, @boost_clock_mhz, @tdp_watts,
    @cache_l2_kb, @cache_l3_kb, @lithography_nm, @release_date, @socket, @architecture, @features,
    @max_memory_gb, @memory_channels, @pcie_lanes, @integrated_gpu, datetime('now')
)";
            AddParameter(command, "@cpuid", cpu.Cpuid);
            AddParameter(command, "@brand", cpu.Brand);
            AddParameter(command, "@codename", cpu.Codename);
            AddParameter(command, "@cores", cpu.Cores);
            AddParameter(command, "@threads", cpu.Threads);
            AddParameter(command, "@base_clock_mhz", cpu.BaseClockMhz);
            AddParameter(command, "@boost_clock_mhz", cpu.BoostClockMhz);
            AddParameter(command, "@tdp_watts", cpu.TdpWatts);
            AddParameter(command, "@cache_l2_kb", cpu.CacheL2Kb);
            AddParameter(command, "@cache_l3_kb", cpu.CacheL3Kb);
            AddParameter(command, "@lithography_nm", cpu.LithographyNm);
            AddParameter(command, "@release_date", cpu.ReleaseDate);
            AddParameter(command, "@socket", cpu.Socket);
            AddParameter(command, "@architecture", cpu.Architecture);
            AddParameter(command, "@features", cpu.Features);
            AddParameter(command, "@max_memory_gb", cpu.MaxMemoryGb);
            AddParameter(command, "@memory_channels", cpu.MemoryChannels);
            AddParameter(command, "@pcie_lanes", cpu.PcieLanes);
            AddParameter(command, "@integrated_gpu", cpu.IntegratedGpu);
            await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        foreach (var gpu in seed.Gpu ?? new List<GpuSpecs>())
        {
            if (string.IsNullOrWhiteSpace(gpu.DeviceId) || string.IsNullOrWhiteSpace(gpu.Brand))
            {
                continue;
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
INSERT OR REPLACE INTO gpu_specs (
    device_id, brand, codename, cuda_cores, stream_processors, base_clock_mhz, boost_clock_mhz,
    memory_size_mb, memory_type, memory_bus_bits, memory_bandwidth_gbps, tdp_watts, release_date,
    architecture, directx_version, opengl_version, vulkan_version, features, updated_at
) VALUES (
    @device_id, @brand, @codename, @cuda_cores, @stream_processors, @base_clock_mhz, @boost_clock_mhz,
    @memory_size_mb, @memory_type, @memory_bus_bits, @memory_bandwidth_gbps, @tdp_watts, @release_date,
    @architecture, @directx_version, @opengl_version, @vulkan_version, @features, datetime('now')
)";
            AddParameter(command, "@device_id", gpu.DeviceId);
            AddParameter(command, "@brand", gpu.Brand);
            AddParameter(command, "@codename", gpu.Codename);
            AddParameter(command, "@cuda_cores", gpu.CudaCores);
            AddParameter(command, "@stream_processors", gpu.StreamProcessors);
            AddParameter(command, "@base_clock_mhz", gpu.BaseClockMhz);
            AddParameter(command, "@boost_clock_mhz", gpu.BoostClockMhz);
            AddParameter(command, "@memory_size_mb", gpu.MemorySizeMb);
            AddParameter(command, "@memory_type", gpu.MemoryType);
            AddParameter(command, "@memory_bus_bits", gpu.MemoryBusBits);
            AddParameter(command, "@memory_bandwidth_gbps", gpu.MemoryBandwidthGbps);
            AddParameter(command, "@tdp_watts", gpu.TdpWatts);
            AddParameter(command, "@release_date", gpu.ReleaseDate);
            AddParameter(command, "@architecture", gpu.Architecture);
            AddParameter(command, "@directx_version", gpu.DirectxVersion);
            AddParameter(command, "@opengl_version", gpu.OpenglVersion);
            AddParameter(command, "@vulkan_version", gpu.VulkanVersion);
            AddParameter(command, "@features", gpu.Features);
            await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        await transaction.CommitAsync(ct).ConfigureAwait(false);
    }

    private static void AddParameter(SqliteCommand command, string name, object? value)
    {
        command.Parameters.Add(new SqliteParameter(name, value ?? DBNull.Value));
    }

    private async Task<CpuSpecs?> QueryCpuAsync(string sql, CancellationToken ct, params SqliteParameter[] parameters)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            return null;
        }

        return new CpuSpecs
        {
            Cpuid = ReadString(reader, "cpuid"),
            Brand = ReadString(reader, "brand"),
            Codename = ReadNullableString(reader, "codename"),
            Cores = ReadNullableInt(reader, "cores"),
            Threads = ReadNullableInt(reader, "threads"),
            BaseClockMhz = ReadNullableInt(reader, "base_clock_mhz"),
            BoostClockMhz = ReadNullableInt(reader, "boost_clock_mhz"),
            TdpWatts = ReadNullableInt(reader, "tdp_watts"),
            CacheL2Kb = ReadNullableInt(reader, "cache_l2_kb"),
            CacheL3Kb = ReadNullableInt(reader, "cache_l3_kb"),
            LithographyNm = ReadNullableInt(reader, "lithography_nm"),
            ReleaseDate = ReadNullableString(reader, "release_date"),
            Socket = ReadNullableString(reader, "socket"),
            Architecture = ReadNullableString(reader, "architecture"),
            Features = ReadNullableString(reader, "features"),
            MaxMemoryGb = ReadNullableInt(reader, "max_memory_gb"),
            MemoryChannels = ReadNullableInt(reader, "memory_channels"),
            PcieLanes = ReadNullableInt(reader, "pcie_lanes"),
            IntegratedGpu = ReadNullableString(reader, "integrated_gpu"),
            IsFromDatabase = true
        };
    }

    private async Task<GpuSpecs?> QueryGpuAsync(string sql, CancellationToken ct, params SqliteParameter[] parameters)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            return null;
        }

        return new GpuSpecs
        {
            DeviceId = ReadString(reader, "device_id"),
            Brand = ReadString(reader, "brand"),
            Codename = ReadNullableString(reader, "codename"),
            CudaCores = ReadNullableInt(reader, "cuda_cores"),
            StreamProcessors = ReadNullableInt(reader, "stream_processors"),
            BaseClockMhz = ReadNullableInt(reader, "base_clock_mhz"),
            BoostClockMhz = ReadNullableInt(reader, "boost_clock_mhz"),
            MemorySizeMb = ReadNullableInt(reader, "memory_size_mb"),
            MemoryType = ReadNullableString(reader, "memory_type"),
            MemoryBusBits = ReadNullableInt(reader, "memory_bus_bits"),
            MemoryBandwidthGbps = ReadNullableDouble(reader, "memory_bandwidth_gbps"),
            TdpWatts = ReadNullableInt(reader, "tdp_watts"),
            ReleaseDate = ReadNullableString(reader, "release_date"),
            Architecture = ReadNullableString(reader, "architecture"),
            DirectxVersion = ReadNullableString(reader, "directx_version"),
            OpenglVersion = ReadNullableString(reader, "opengl_version"),
            VulkanVersion = ReadNullableString(reader, "vulkan_version"),
            Features = ReadNullableString(reader, "features"),
            IsFromDatabase = true
        };
    }

    private SqliteConnection CreateConnection()
    {
        return new SqliteConnection($"Data Source={_dbPath};Cache=Shared");
    }

    private static string ReadString(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
    }

    private static string? ReadNullableString(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static int? ReadNullableInt(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static double? ReadNullableDouble(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDouble(ordinal);
    }

    private static string NormalizeCpuBrand(string input)
    {
        var normalized = Regex.Replace(input, @"\s+", " ").Trim();
        normalized = Regex.Replace(normalized, @"@\s*[\d.]+\s*GHz", "", RegexOptions.IgnoreCase).Trim();
        normalized = Regex.Replace(normalized, @"\((R|TM|tm)\)", "", RegexOptions.IgnoreCase).Trim();
        normalized = Regex.Replace(normalized, @"\s+CPU\s*$", "", RegexOptions.IgnoreCase).Trim();
        return normalized;
    }

    private static IEnumerable<string> ExtractCpuTokens(string input)
    {
        var patterns = new[]
        {
            @"i[3579]-\d{4,5}[a-zA-Z]*",
            @"ryzen\s+\d\s+\d{4}[a-zA-Z]*",
            @"xeon\s+[a-zA-Z]-?\d{4}[a-zA-Z]*",
            @"pentium\s+[a-zA-Z]\d{4}"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                yield return match.Value;
            }
        }
    }

    private const string SchemaSql = @"
CREATE TABLE IF NOT EXISTS cpu_specs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    cpuid TEXT UNIQUE NOT NULL,
    brand TEXT NOT NULL,
    codename TEXT,
    cores INTEGER,
    threads INTEGER,
    base_clock_mhz INTEGER,
    boost_clock_mhz INTEGER,
    tdp_watts INTEGER,
    cache_l2_kb INTEGER,
    cache_l3_kb INTEGER,
    lithography_nm INTEGER,
    release_date TEXT,
    socket TEXT,
    architecture TEXT,
    features TEXT,
    max_memory_gb INTEGER,
    memory_channels INTEGER,
    pcie_lanes INTEGER,
    integrated_gpu TEXT,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS gpu_specs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    device_id TEXT UNIQUE NOT NULL,
    brand TEXT NOT NULL,
    codename TEXT,
    cuda_cores INTEGER,
    stream_processors INTEGER,
    base_clock_mhz INTEGER,
    boost_clock_mhz INTEGER,
    memory_size_mb INTEGER,
    memory_type TEXT,
    memory_bus_bits INTEGER,
    memory_bandwidth_gbps REAL,
    tdp_watts INTEGER,
    release_date TEXT,
    architecture TEXT,
    directx_version TEXT,
    opengl_version TEXT,
    vulkan_version TEXT,
    features TEXT,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS motherboard_specs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    board_id TEXT UNIQUE NOT NULL,
    manufacturer TEXT NOT NULL,
    model TEXT NOT NULL,
    chipset TEXT,
    socket TEXT,
    form_factor TEXT,
    memory_slots INTEGER,
    max_memory_gb INTEGER,
    memory_type TEXT,
    max_memory_speed_mhz INTEGER,
    pcie_slots TEXT,
    sata_ports INTEGER,
    m2_slots INTEGER,
    usb_ports TEXT,
    audio_codec TEXT,
    network_chip TEXT,
    wifi_chip TEXT,
    bios_type TEXT,
    release_date TEXT,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS storage_specs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    model_id TEXT UNIQUE NOT NULL,
    manufacturer TEXT NOT NULL,
    model TEXT NOT NULL,
    type TEXT NOT NULL,
    capacity_gb INTEGER,
    interface TEXT,
    form_factor TEXT,
    seq_read_mbps INTEGER,
    seq_write_mbps INTEGER,
    random_read_iops INTEGER,
    random_write_iops INTEGER,
    nand_type TEXT,
    controller TEXT,
    dram_cache_mb INTEGER,
    tbw_tb INTEGER,
    release_date TEXT,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS ram_specs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    part_number TEXT UNIQUE NOT NULL,
    manufacturer TEXT NOT NULL,
    model TEXT NOT NULL,
    type TEXT NOT NULL,
    speed_mhz INTEGER,
    capacity_gb INTEGER,
    modules INTEGER,
    cas_latency INTEGER,
    timings TEXT,
    voltage REAL,
    ecc INTEGER DEFAULT 0,
    xmp_profiles TEXT,
    release_date TEXT,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_cpu_cpuid ON cpu_specs(cpuid);
CREATE INDEX IF NOT EXISTS idx_gpu_device ON gpu_specs(device_id);
CREATE INDEX IF NOT EXISTS idx_mb_model ON motherboard_specs(board_id);
CREATE INDEX IF NOT EXISTS idx_storage_model ON storage_specs(model_id);
CREATE INDEX IF NOT EXISTS idx_ram_part ON ram_specs(part_number);

CREATE TABLE IF NOT EXISTS db_metadata (
    key TEXT PRIMARY KEY,
    value TEXT
);

INSERT OR REPLACE INTO db_metadata (key, value) VALUES ('version', '1.0.0');
INSERT OR REPLACE INTO db_metadata (key, value) VALUES ('last_update', datetime('now'));
";

    private sealed class HardwareDatabaseSeed
    {
        public string Version { get; set; } = "";
        public List<CpuSpecs>? Cpu { get; set; }
        public List<GpuSpecs>? Gpu { get; set; }
    }
}
