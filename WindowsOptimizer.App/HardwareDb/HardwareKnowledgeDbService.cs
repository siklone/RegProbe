using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.App.HardwareDb.Models;

namespace WindowsOptimizer.App.HardwareDb;

public sealed class HardwareKnowledgeDbService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private bool _isLoaded;

    private IReadOnlyDictionary<string, CpuModel> _cpuIndex = new Dictionary<string, CpuModel>();
    private IReadOnlyDictionary<string, CpuModel> _cpuAliasIndex = new Dictionary<string, CpuModel>();
    private IReadOnlyDictionary<string, GpuModel> _gpuIndex = new Dictionary<string, GpuModel>();
    private IReadOnlyDictionary<string, GpuModel> _gpuAliasIndex = new Dictionary<string, GpuModel>();
    private IReadOnlyDictionary<string, MotherboardModel> _motherboardIndex = new Dictionary<string, MotherboardModel>();
    private IReadOnlyDictionary<string, MotherboardModel> _motherboardAliasIndex = new Dictionary<string, MotherboardModel>();
    private IReadOnlyDictionary<string, DisplayModel> _displayIndex = new Dictionary<string, DisplayModel>();
    private IReadOnlyDictionary<string, DisplayModel> _displayAliasIndex = new Dictionary<string, DisplayModel>();
    private IReadOnlyDictionary<string, ChipsetModel> _chipsetIndex = new Dictionary<string, ChipsetModel>();
    private IReadOnlyDictionary<string, ChipsetModel> _chipsetAliasIndex = new Dictionary<string, ChipsetModel>();
    private IReadOnlyDictionary<string, MemoryModel> _memoryIndex = new Dictionary<string, MemoryModel>();
    private IReadOnlyDictionary<string, MemoryModel> _memoryAliasIndex = new Dictionary<string, MemoryModel>();
    private IReadOnlyDictionary<string, MemoryChipModel> _memoryChipIndex = new Dictionary<string, MemoryChipModel>();
    private IReadOnlyDictionary<string, MemoryChipModel> _memoryChipAliasIndex = new Dictionary<string, MemoryChipModel>();
    private IReadOnlyDictionary<string, StorageControllerModel> _storageIndex = new Dictionary<string, StorageControllerModel>();
    private IReadOnlyDictionary<string, StorageControllerModel> _storageAliasIndex = new Dictionary<string, StorageControllerModel>();
    private IReadOnlyDictionary<string, UsbControllerModel> _usbIndex = new Dictionary<string, UsbControllerModel>();
    private IReadOnlyDictionary<string, UsbControllerModel> _usbAliasIndex = new Dictionary<string, UsbControllerModel>();
    private IReadOnlyDictionary<string, NetworkAdapterModel> _networkIndex = new Dictionary<string, NetworkAdapterModel>();
    private IReadOnlyDictionary<string, NetworkAdapterModel> _networkAliasIndex = new Dictionary<string, NetworkAdapterModel>();

    private HardwareKnowledgeDbService()
    {
    }

    public static HardwareKnowledgeDbService Instance { get; } = new();

    public async Task InitializeAsync(CancellationToken ct)
    {
        if (_isLoaded)
        {
            return;
        }

        await _loadGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_isLoaded)
            {
                return;
            }

            var basePath = Path.Combine(AppContext.BaseDirectory, "Assets", "HardwareDb");
            var cpuTask = LoadAsync<CpuModel>(Path.Combine(basePath, "hardware_db_cpu.json"), ct);
            var gpuTask = LoadAsync<GpuModel>(Path.Combine(basePath, "hardware_db_gpu.json"), ct);
            var motherboardTask = LoadAsync<MotherboardModel>(Path.Combine(basePath, "hardware_db_motherboards.json"), ct);
            var displayTask = LoadAsync<DisplayModel>(Path.Combine(basePath, "hardware_db_displays.json"), ct);
            var chipsetTask = LoadAsync<ChipsetModel>(Path.Combine(basePath, "hardware_db_chipsets.json"), ct);
            var memoryTask = LoadAsync<MemoryModel>(Path.Combine(basePath, "hardware_db_memory_modules.json"), ct);
            var memoryChipTask = LoadAsync<MemoryChipModel>(Path.Combine(basePath, "hardware_db_memory_chips.json"), ct);
            var storageTask = LoadAsync<StorageControllerModel>(Path.Combine(basePath, "hardware_db_storage_controllers.json"), ct);
            var usbTask = LoadAsync<UsbControllerModel>(Path.Combine(basePath, "hardware_db_usb_controllers.json"), ct);
            var networkTask = LoadAsync<NetworkAdapterModel>(Path.Combine(basePath, "hardware_db_network_adapters.json"), ct);

            await Task.WhenAll(cpuTask, gpuTask, motherboardTask, displayTask, chipsetTask, memoryTask, memoryChipTask, storageTask, usbTask, networkTask).ConfigureAwait(false);

            _cpuIndex = BuildIndex(cpuTask.Result.Items);
            _cpuAliasIndex = BuildAliasIndex(cpuTask.Result.Items);
            _gpuIndex = BuildIndex(gpuTask.Result.Items);
            _gpuAliasIndex = BuildAliasIndex(gpuTask.Result.Items);
            _motherboardIndex = BuildIndex(motherboardTask.Result.Items);
            _motherboardAliasIndex = BuildAliasIndex(motherboardTask.Result.Items);
            _displayIndex = BuildIndex(displayTask.Result.Items);
            _displayAliasIndex = BuildAliasIndex(displayTask.Result.Items);
            _chipsetIndex = BuildIndex(chipsetTask.Result.Items);
            _chipsetAliasIndex = BuildAliasIndex(chipsetTask.Result.Items);
            _memoryIndex = BuildIndex(memoryTask.Result.Items);
            _memoryAliasIndex = BuildAliasIndex(memoryTask.Result.Items);
            _memoryChipIndex = BuildIndex(memoryChipTask.Result.Items);
            _memoryChipAliasIndex = BuildAliasIndex(memoryChipTask.Result.Items);
            _storageIndex = BuildIndex(storageTask.Result.Items);
            _storageAliasIndex = BuildAliasIndex(storageTask.Result.Items);
            _usbIndex = BuildIndex(usbTask.Result.Items);
            _usbAliasIndex = BuildAliasIndex(usbTask.Result.Items);
            _networkIndex = BuildIndex(networkTask.Result.Items);
            _networkAliasIndex = BuildAliasIndex(networkTask.Result.Items);

            _isLoaded = true;
        }
        finally
        {
            _loadGate.Release();
        }
    }

    public CpuModel? MatchCpu(string rawCpuName)
    {
        return MatchCpuDetailed(rawCpuName).Model;
    }

    public HardwareMatchResult<CpuModel> MatchCpuDetailed(string rawCpuName)
    {
        return HardwareMatcher.MatchDetailed(rawCpuName, _cpuIndex, _cpuAliasIndex);
    }

    public GpuModel? MatchGpu(string rawGpuName)
    {
        return MatchGpuDetailed(rawGpuName).Model;
    }

    public HardwareMatchResult<GpuModel> MatchGpuDetailed(string rawGpuName)
    {
        return HardwareMatcher.MatchDetailed(rawGpuName, _gpuIndex, _gpuAliasIndex);
    }

    public MotherboardModel? MatchMotherboard(string rawBoardName)
    {
        return MatchMotherboardDetailed(rawBoardName).Model;
    }

    public HardwareMatchResult<MotherboardModel> MatchMotherboardDetailed(string rawBoardName)
    {
        return HardwareMatcher.MatchDetailed(rawBoardName, _motherboardIndex, _motherboardAliasIndex);
    }

    public DisplayModel? MatchDisplay(string rawDisplayName)
    {
        return MatchDisplayDetailed(rawDisplayName).Model;
    }

    public HardwareMatchResult<DisplayModel> MatchDisplayDetailed(string rawDisplayName)
    {
        return HardwareMatcher.MatchDetailed(rawDisplayName, _displayIndex, _displayAliasIndex);
    }

    public MemoryModel? MatchMemory(string rawMemoryName)
    {
        return MatchMemoryDetailed(rawMemoryName).Model;
    }

    public HardwareMatchResult<MemoryModel> MatchMemoryDetailed(string rawMemoryName)
    {
        return HardwareMatcher.MatchDetailed(rawMemoryName, _memoryIndex, _memoryAliasIndex);
    }

    public StorageControllerModel? MatchStorage(string rawStorageName)
    {
        return MatchStorageDetailed(rawStorageName).Model;
    }

    public HardwareMatchResult<StorageControllerModel> MatchStorageDetailed(string rawStorageName)
    {
        return HardwareMatcher.MatchDetailed(rawStorageName, _storageIndex, _storageAliasIndex);
    }

    public ChipsetModel? MatchChipset(string rawChipsetName)
    {
        return MatchChipsetDetailed(rawChipsetName).Model;
    }

    public HardwareMatchResult<ChipsetModel> MatchChipsetDetailed(string rawChipsetName)
    {
        return HardwareMatcher.MatchDetailed(rawChipsetName, _chipsetIndex, _chipsetAliasIndex);
    }

    public MemoryChipModel? MatchMemoryChip(string rawMemoryChipName)
    {
        return MatchMemoryChipDetailed(rawMemoryChipName).Model;
    }

    public HardwareMatchResult<MemoryChipModel> MatchMemoryChipDetailed(string rawMemoryChipName)
    {
        return HardwareMatcher.MatchDetailed(rawMemoryChipName, _memoryChipIndex, _memoryChipAliasIndex);
    }

    public UsbControllerModel? MatchUsb(string rawUsbName)
    {
        return MatchUsbDetailed(rawUsbName).Model;
    }

    public HardwareMatchResult<UsbControllerModel> MatchUsbDetailed(string rawUsbName)
    {
        return HardwareMatcher.MatchDetailed(rawUsbName, _usbIndex, _usbAliasIndex);
    }

    public NetworkAdapterModel? MatchNetworkAdapter(string rawAdapterName)
    {
        return MatchNetworkAdapterDetailed(rawAdapterName).Model;
    }

    public HardwareMatchResult<NetworkAdapterModel> MatchNetworkAdapterDetailed(string rawAdapterName)
    {
        return HardwareMatcher.MatchDetailed(rawAdapterName, _networkIndex, _networkAliasIndex);
    }

    private static async Task<HardwareDbDocument<TModel>> LoadAsync<TModel>(string path, CancellationToken ct)
        where TModel : HardwareModelBase
    {
        if (!File.Exists(path))
        {
            return new HardwareDbDocument<TModel>();
        }

        await using var stream = File.OpenRead(path);
        var document = await JsonSerializer.DeserializeAsync<HardwareDbDocument<TModel>>(stream, JsonOptions, ct).ConfigureAwait(false);
        return document ?? new HardwareDbDocument<TModel>();
    }

    private static IReadOnlyDictionary<string, TModel> BuildIndex<TModel>(IEnumerable<TModel> items)
        where TModel : HardwareModelBase
    {
        var map = new Dictionary<string, TModel>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var key = HardwareNameNormalizer.Normalize(item.NormalizedName);
            if (!string.IsNullOrWhiteSpace(key))
            {
                map[key] = item;
            }
        }

        return map;
    }

    private static IReadOnlyDictionary<string, TModel> BuildAliasIndex<TModel>(IEnumerable<TModel> items)
        where TModel : HardwareModelBase
    {
        var map = new Dictionary<string, TModel>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            foreach (var alias in item.Aliases)
            {
                var key = HardwareNameNormalizer.Normalize(alias);
                if (!string.IsNullOrWhiteSpace(key))
                {
                    map[key] = item;
                }
            }
        }

        return map;
    }
}
