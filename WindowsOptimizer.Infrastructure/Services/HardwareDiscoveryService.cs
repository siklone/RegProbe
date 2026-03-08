using System;
using System.Management;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using WindowsOptimizer.Core.Intelligence;
using WindowsOptimizer.Core.Services;

namespace WindowsOptimizer.Infrastructure.Services;

[SupportedOSPlatform("windows")]
public sealed class HardwareDiscoveryService : IHardwareDiscoveryService
{
    public Task<HardwareProfile> GetHardwareProfileAsync()
    {
        return Task.Run(() =>
        {
            var processorName = GetWmiValue("Win32_Processor", "Name") ?? "Unknown Processor";
            var coreCount = Convert.ToInt32(GetWmiValue("Win32_Processor", "NumberOfCores") ?? "0");
            var totalMemory = Convert.ToInt64(GetWmiValue("Win32_ComputerSystem", "TotalPhysicalMemory") ?? "0");
            var gpuName = GetWmiValue("Win32_VideoController", "Name") ?? "Unknown GPU";
            
            var storageType = DetectPrimaryStorageType();
            
            // Note: Simple hyper-threading check: NumberOfLogicalProcessors > NumberOfCores
            var logicalCount = Convert.ToInt32(GetWmiValue("Win32_Processor", "NumberOfLogicalProcessors") ?? "0");
            var isHyperThreading = logicalCount > coreCount;

            return new HardwareProfile(
                processorName,
                coreCount,
                isHyperThreading,
                totalMemory,
                storageType,
                gpuName,
                false // AVX512 check would usually require P/Invoke, setting false for now or placeholder
            );
        });
    }

    private static string? GetWmiValue(string className, string propertyName)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {className}");
            foreach (var obj in searcher.Get())
            {
                return obj[propertyName]?.ToString();
            }
        }
        catch
        {
            // Fallback
        }
        return null;
    }

    private static StorageType DetectPrimaryStorageType()
    {
        try
        {
            // We use MSFT_PhysicalDisk which provides MediaDescription
            // Note: MSFT_ classes are in root\Microsoft\Windows\Storage
            var scope = new ManagementScope(@"root\Microsoft\Windows\Storage");
            scope.Connect();
            
            var query = new ObjectQuery("SELECT MediaType, BusType FROM MSFT_PhysicalDisk WHERE IsBoot = True");
            using var searcher = new ManagementObjectSearcher(scope, query);
            
            foreach (var obj in searcher.Get())
            {
                var mediaType = Convert.ToInt32(obj["MediaType"] ?? 0);
                var busType = Convert.ToInt32(obj["BusType"] ?? 0);
                
                if (mediaType == 4) // SSD
                {
                    // BusType 17 is NVMe
                    return busType == 17 ? StorageType.NVMe : StorageType.SSD;
                }
                if (mediaType == 3) // HDD
                {
                    return StorageType.HDD;
                }
            }
        }
        catch
        {
            // Fallback to basic SSD/HDD check if MSFT_ isn't available
        }
        
        return StorageType.Unknown;
    }
}
