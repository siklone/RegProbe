using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using OpenTraceProject.App.HardwareDb;
using OpenTraceProject.App.ViewModels;

namespace OpenTraceProject.App.Services;

internal static class DisplayDetectionHelpers
{
    internal sealed class MonitorEdidInfo
    {
        public string InstanceName { get; init; } = string.Empty;
        public string UserFriendlyName { get; init; } = string.Empty;
        public DashboardInfoHelpers.EdidInfo EdidInfo { get; init; }
    }

    internal readonly record struct DisplayDeviceInfo(
        string DeviceName,
        string DeviceString,
        string DeviceId,
        string DeviceKey);

    internal sealed class ResolvedDisplayInfo
    {
        public string Name { get; init; } = string.Empty;
        public string DeviceName { get; init; } = string.Empty;
        public string DeviceString { get; init; } = string.Empty;
        public string DeviceId { get; init; } = string.Empty;
        public string InstanceId { get; init; } = string.Empty;
        public string? PrefixId { get; init; }
        public string? MatchedInstance { get; init; }
        public string MatchMode { get; init; } = "Unmatched";
        public string? MatchKey { get; init; }
        public int PrefixMatchCount { get; init; }
        public string? FriendlyName { get; init; }
        public string? Manufacturer { get; init; }
        public string? Model { get; init; }
        public string? ProductCode { get; init; }
        public string? ConnectionType { get; init; }
        public string IconLookupSeed { get; init; } = string.Empty;
        public int Width { get; init; }
        public int Height { get; init; }
        public int RefreshRateHz { get; init; }
        public int BitsPerPixel { get; init; }
        public bool IsPrimary { get; init; }
        public int? PhysicalWidthCm { get; init; }
        public int? PhysicalHeightCm { get; init; }
        public DashboardInfoHelpers.EdidInfo? RegistryEdidInfo { get; init; }
        public MonitorEdidInfo? MatchedEdidInfo { get; init; }
    }

    internal static IReadOnlyList<ResolvedDisplayInfo> ResolveConnectedDisplays()
    {
        var resolvedDisplays = new List<ResolvedDisplayInfo>();

        try
        {
            var monitorEdidByInstance = LoadMonitorEdidInfoByInstance();
            var monitorNamesByInstance = monitorEdidByInstance
                .Select(kvp => new
                {
                    kvp.Key,
                    Name = !string.IsNullOrWhiteSpace(kvp.Value.UserFriendlyName)
                        ? kvp.Value.UserFriendlyName
                        : kvp.Value.EdidInfo.MonitorName
                })
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
                .ToDictionary(entry => entry.Key, entry => entry.Name!, StringComparer.OrdinalIgnoreCase);
            var connectionTypesByInstance = LoadMonitorConnectionTypesByInstance();
            var instanceNames = monitorEdidByInstance.Keys
                .Concat(connectionTypesByInstance.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var uniqueInstanceMap = BuildUniqueMonitorInstanceMap(instanceNames);
            var prefixIndex = BuildMonitorPrefixIndex(instanceNames);
            var uniquePrefixMap = BuildUniqueMonitorPrefixMap(prefixIndex);
            var edidMatchIndex = BuildEdidMatchIndex(monitorEdidByInstance.Values);
            var uniqueEdidMatchMap = BuildUniqueEdidMatchMap(edidMatchIndex);

            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                var deviceName = screen.DeviceName;
                var settings = GetDisplaySettings(deviceName, screen);
                var displayDevice = GetMonitorDisplayDevice(deviceName);

                var instanceId = DashboardInfoHelpers.NormalizeMonitorInstanceId(displayDevice?.DeviceId);
                var prefixId = DashboardInfoHelpers.GetMonitorInstancePrefix(instanceId);
                var prefixMatchCount = !string.IsNullOrEmpty(prefixId) &&
                                       prefixIndex.TryGetValue(prefixId, out var prefixMatches)
                    ? prefixMatches.Count
                    : 0;

                DashboardInfoHelpers.EdidInfo? registryEdidInfo = null;
                if (TryGetRegistryEdidInfo(displayDevice?.DeviceId, out var edidInfo))
                {
                    registryEdidInfo = edidInfo;
                }

                string matchMode = "Unmatched";
                string? matchKey = null;
                string? matchedInstance = null;

                if (TryResolveMonitorInstance(instanceId, uniqueInstanceMap, out var resolvedInstance, out var resolvedMode, out var resolvedKey))
                {
                    matchedInstance = resolvedInstance;
                    matchMode = resolvedMode;
                    matchKey = resolvedKey;
                }
                else if (registryEdidInfo.HasValue &&
                         TryResolveMonitorInstanceByEdid(registryEdidInfo.Value, uniqueEdidMatchMap, out var edidInstance, out var edidMode, out var edidKey))
                {
                    matchedInstance = edidInstance;
                    matchMode = edidMode;
                    matchKey = edidKey;
                }
                else if (!string.IsNullOrEmpty(prefixId) &&
                         uniquePrefixMap.TryGetValue(prefixId, out var prefixInstance))
                {
                    matchedInstance = prefixInstance;
                    matchMode = "PrefixUnique";
                    matchKey = prefixId;
                }
                else if (prefixMatchCount > 1)
                {
                    matchMode = "PrefixAmbiguous";
                }

                MonitorEdidInfo? matchedMonitor = null;
                if (!string.IsNullOrWhiteSpace(matchedInstance) &&
                    monitorEdidByInstance.TryGetValue(matchedInstance, out var resolvedMonitor))
                {
                    matchedMonitor = resolvedMonitor;
                }
                else if (!string.IsNullOrWhiteSpace(instanceId) &&
                         monitorEdidByInstance.TryGetValue(instanceId, out var directMonitor))
                {
                    matchedMonitor = directMonitor;
                }

                string? friendlyName = null;
                if (!string.IsNullOrWhiteSpace(matchedInstance) &&
                    monitorNamesByInstance.TryGetValue(matchedInstance, out var resolvedFriendlyName))
                {
                    friendlyName = resolvedFriendlyName;
                }
                else if (!string.IsNullOrWhiteSpace(instanceId) &&
                         monitorNamesByInstance.TryGetValue(instanceId, out var directFriendlyName))
                {
                    friendlyName = directFriendlyName;
                }

                var manufacturer = FirstNotEmpty(
                    matchedMonitor?.EdidInfo.ManufacturerId,
                    registryEdidInfo?.ManufacturerId);
                var productCode = FirstNotEmpty(
                    matchedMonitor?.EdidInfo.ProductCodeHex,
                    registryEdidInfo?.ProductCodeHex);
                var model = FirstNotEmpty(
                    matchedMonitor?.EdidInfo.MonitorName,
                    registryEdidInfo?.MonitorName);
                var monitorName = FirstNotEmpty(
                    friendlyName,
                    FirstNotEmpty(
                        model,
                        FirstNotEmpty(
                            displayDevice?.DeviceString,
                            deviceName.Replace(@"\\.\", "")))) ?? deviceName.Replace(@"\\.\", "");

                string connectionType = "Unknown";
                if (!string.IsNullOrWhiteSpace(matchedInstance) &&
                    connectionTypesByInstance.TryGetValue(matchedInstance, out var resolvedVideoType))
                {
                    connectionType = DashboardInfoHelpers.MapVideoOutputTechnology(resolvedVideoType);
                }
                else if (!string.IsNullOrWhiteSpace(instanceId) &&
                         connectionTypesByInstance.TryGetValue(instanceId, out var directVideoType))
                {
                    connectionType = DashboardInfoHelpers.MapVideoOutputTechnology(directVideoType);
                }

                var bitsPerPixel = settings.BitsPerPixel > 0 ? settings.BitsPerPixel : screen.BitsPerPixel;
                var lookupSeed = HardwareIconService.BuildDisplayLookupSeed(
                    monitorName,
                    friendlyName,
                    manufacturer,
                    model,
                    productCode,
                    registryEdidInfo?.ManufacturerId,
                    registryEdidInfo?.MonitorName,
                    registryEdidInfo?.ProductCodeHex);

                resolvedDisplays.Add(new ResolvedDisplayInfo
                {
                    Name = monitorName,
                    DeviceName = deviceName,
                    DeviceString = displayDevice?.DeviceString ?? string.Empty,
                    DeviceId = displayDevice?.DeviceId ?? string.Empty,
                    InstanceId = instanceId,
                    PrefixId = prefixId,
                    MatchedInstance = matchedInstance,
                    MatchMode = matchMode,
                    MatchKey = matchKey,
                    PrefixMatchCount = prefixMatchCount,
                    FriendlyName = friendlyName,
                    Manufacturer = manufacturer,
                    Model = model,
                    ProductCode = productCode,
                    ConnectionType = connectionType,
                    IconLookupSeed = lookupSeed,
                    Width = settings.Width,
                    Height = settings.Height,
                    RefreshRateHz = settings.RefreshRate,
                    BitsPerPixel = bitsPerPixel,
                    IsPrimary = screen.Primary,
                    PhysicalWidthCm = matchedMonitor?.EdidInfo.HorizontalSizeCm ?? registryEdidInfo?.HorizontalSizeCm,
                    PhysicalHeightCm = matchedMonitor?.EdidInfo.VerticalSizeCm ?? registryEdidInfo?.VerticalSizeCm,
                    RegistryEdidInfo = registryEdidInfo,
                    MatchedEdidInfo = matchedMonitor
                });
            }
        }
        catch
        {
        }

        if (resolvedDisplays.Count == 0)
        {
            try
            {
                foreach (var screen in System.Windows.Forms.Screen.AllScreens)
                {
                    var name = screen.DeviceName.Replace(@"\\.\", "");
                    resolvedDisplays.Add(new ResolvedDisplayInfo
                    {
                        Name = name,
                        DeviceName = screen.DeviceName,
                        MatchMode = "Fallback",
                        ConnectionType = "Unknown",
                        IconLookupSeed = HardwareIconService.BuildDisplayLookupSeed(name),
                        Width = screen.Bounds.Width,
                        Height = screen.Bounds.Height,
                        BitsPerPixel = screen.BitsPerPixel,
                        IsPrimary = screen.Primary
                    });
                }
            }
            catch
            {
            }
        }

        return resolvedDisplays;
    }

    internal static (int Width, int Height, int RefreshRate, int BitsPerPixel) GetDisplaySettings(
        string deviceName,
        System.Windows.Forms.Screen screen)
    {
        var devMode = new DevMode
        {
            dmSize = (short)Marshal.SizeOf<DevMode>()
        };

        if (EnumDisplaySettings(deviceName, EnumCurrentSettings, ref devMode))
        {
            return (devMode.dmPelsWidth, devMode.dmPelsHeight, devMode.dmDisplayFrequency, devMode.dmBitsPerPel);
        }

        return (screen.Bounds.Width, screen.Bounds.Height, 0, screen.BitsPerPixel);
    }

    internal static DisplayDeviceInfo? GetMonitorDisplayDevice(string deviceName)
    {
        DisplayDeviceInfo? first = null;
        var display = new DisplayDevice
        {
            cb = Marshal.SizeOf<DisplayDevice>()
        };

        for (uint i = 0; EnumDisplayDevices(deviceName, i, ref display, 0); i++)
        {
            first ??= new DisplayDeviceInfo(display.DeviceName, display.DeviceString, display.DeviceID, display.DeviceKey);
            if ((display.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0)
            {
                return new DisplayDeviceInfo(display.DeviceName, display.DeviceString, display.DeviceID, display.DeviceKey);
            }

            display = new DisplayDevice
            {
                cb = Marshal.SizeOf<DisplayDevice>()
            };
        }

        return first;
    }

    internal static Dictionary<string, MonitorEdidInfo> LoadMonitorEdidInfoByInstance()
    {
        var infos = new Dictionary<string, MonitorEdidInfo>(StringComparer.OrdinalIgnoreCase);
        var wmiInfo = new Dictionary<string, (string UserFriendly, string Manufacturer, string Product, string Serial)>(StringComparer.OrdinalIgnoreCase);
        var edidInfoByInstance = new Dictionary<string, DashboardInfoHelpers.EdidInfo>(StringComparer.OrdinalIgnoreCase);
        var sizeInfoByInstance = new Dictionary<string, (int? Horizontal, int? Vertical)>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var monitorIdSearcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM WmiMonitorID");
            foreach (ManagementObject obj in monitorIdSearcher.Get())
            {
                var instanceName = obj["InstanceName"]?.ToString();
                if (string.IsNullOrWhiteSpace(instanceName))
                {
                    continue;
                }

                var userFriendlyName = DashboardInfoHelpers.DecodeWmiString(obj["UserFriendlyName"] as ushort[]);
                var manufacturer = DashboardInfoHelpers.DecodeWmiString(obj["ManufacturerName"] as ushort[]);
                var productCode = DashboardInfoHelpers.DecodeWmiProductCode(obj["ProductCodeID"] as ushort[]);
                var serialNumber = DashboardInfoHelpers.DecodeWmiString(obj["SerialNumberID"] as ushort[]);
                wmiInfo[instanceName] = (userFriendlyName, manufacturer, productCode, serialNumber);
            }
        }
        catch
        {
        }

        try
        {
            using var paramsSearcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM WmiMonitorBasicDisplayParams");
            foreach (ManagementObject obj in paramsSearcher.Get())
            {
                var instanceName = obj["InstanceName"]?.ToString();
                if (string.IsNullOrWhiteSpace(instanceName))
                {
                    continue;
                }

                var horizontal = Convert.ToInt32(obj["MaxHorizontalImageSize"] ?? 0);
                var vertical = Convert.ToInt32(obj["MaxVerticalImageSize"] ?? 0);
                sizeInfoByInstance[instanceName] = (
                    horizontal > 0 ? horizontal : null,
                    vertical > 0 ? vertical : null);
            }
        }
        catch
        {
        }

        try
        {
            using var descriptorSearcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM WmiMonitorDescriptorMethods");
            foreach (ManagementObject obj in descriptorSearcher.Get())
            {
                var instanceName = obj["InstanceName"]?.ToString();
                if (string.IsNullOrWhiteSpace(instanceName))
                {
                    continue;
                }

                if (TryGetEdidFromDescriptorMethods(obj, out var edidBytes))
                {
                    edidInfoByInstance[instanceName] = DashboardInfoHelpers.ParseEdid(edidBytes);
                }
            }
        }
        catch
        {
        }

        var allInstanceNames = wmiInfo.Keys
            .Concat(edidInfoByInstance.Keys)
            .Concat(sizeInfoByInstance.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var instanceName in allInstanceNames)
        {
            wmiInfo.TryGetValue(instanceName, out var wmi);
            edidInfoByInstance.TryGetValue(instanceName, out var edidInfo);
            sizeInfoByInstance.TryGetValue(instanceName, out var sizeInfo);

            infos[instanceName] = new MonitorEdidInfo
            {
                InstanceName = instanceName,
                UserFriendlyName = wmi.UserFriendly ?? string.Empty,
                EdidInfo = MergeEdidInfo(edidInfo, wmi.Manufacturer, wmi.Product, wmi.Serial, wmi.UserFriendly, sizeInfo)
            };
        }

        return infos;
    }

    internal static Dictionary<string, int> LoadMonitorConnectionTypesByInstance()
    {
        var types = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var connectionSearcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM WmiMonitorConnectionParams");
            foreach (ManagementObject obj in connectionSearcher.Get())
            {
                var instanceName = obj["InstanceName"]?.ToString();
                if (string.IsNullOrWhiteSpace(instanceName))
                {
                    continue;
                }

                types[instanceName] = Convert.ToInt32(obj["VideoOutputTechnology"] ?? -1);
            }
        }
        catch
        {
        }

        return types;
    }

    internal static bool TryGetRegistryEdidInfo(string? deviceId, out DashboardInfoHelpers.EdidInfo info)
    {
        info = default;
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return false;
        }

        var normalized = DashboardInfoHelpers.NormalizeMonitorInstanceId(deviceId);
        var paths = new List<string> { normalized };
        if (!string.Equals(normalized, deviceId.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            paths.Add(deviceId.Trim());
        }

        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            try
            {
                using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\{path}\Device Parameters");
                var edidBytes = key?.GetValue("EDID") as byte[];
                if (edidBytes == null || edidBytes.Length < 128)
                {
                    continue;
                }

                info = DashboardInfoHelpers.ParseEdid(edidBytes);
                return !string.IsNullOrWhiteSpace(info.ManufacturerId) ||
                       !string.IsNullOrWhiteSpace(info.MonitorName) ||
                       !string.IsNullOrWhiteSpace(info.SerialNumber);
            }
            catch
            {
            }
        }

        return false;
    }

    private static string? FirstNotEmpty(string? primary, string? fallback)
    {
        return string.IsNullOrWhiteSpace(primary) ? fallback : primary;
    }

    internal static Dictionary<string, List<string>> BuildEdidMatchIndex(IEnumerable<MonitorEdidInfo> infos)
    {
        var index = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var info in infos)
        {
            foreach (var candidate in DashboardInfoHelpers.GetEdidMatchCandidates(info.EdidInfo))
            {
                if (!index.TryGetValue(candidate.Key, out var list))
                {
                    list = new List<string>();
                    index[candidate.Key] = list;
                }

                if (!list.Contains(info.InstanceName, StringComparer.OrdinalIgnoreCase))
                {
                    list.Add(info.InstanceName);
                }
            }
        }

        return index;
    }

    internal static Dictionary<string, string> BuildUniqueEdidMatchMap(Dictionary<string, List<string>> edidIndex)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in edidIndex)
        {
            if (kvp.Value.Count == 1)
            {
                map[kvp.Key] = kvp.Value[0];
            }
        }

        return map;
    }

    internal static bool TryResolveMonitorInstanceByEdid(
        DashboardInfoHelpers.EdidInfo edidInfo,
        IReadOnlyDictionary<string, string> uniqueEdidMap,
        out string matchedInstance,
        out string matchMode,
        out string? matchKey)
    {
        matchedInstance = string.Empty;
        matchMode = "Unmatched";
        matchKey = null;

        foreach (var candidate in DashboardInfoHelpers.GetEdidMatchCandidates(edidInfo))
        {
            if (uniqueEdidMap.TryGetValue(candidate.Key, out var instance))
            {
                matchedInstance = instance;
                matchMode = candidate.Mode;
                matchKey = candidate.Key;
                return true;
            }
        }

        return false;
    }

    internal static Dictionary<string, List<string>> BuildMonitorPrefixIndex(IEnumerable<string> instances)
    {
        var index = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var instance in instances)
        {
            var prefix = DashboardInfoHelpers.GetMonitorInstancePrefix(instance);
            if (string.IsNullOrEmpty(prefix))
            {
                continue;
            }

            if (!index.TryGetValue(prefix, out var list))
            {
                list = new List<string>();
                index[prefix] = list;
            }

            list.Add(instance);
        }

        return index;
    }

    internal static Dictionary<string, string> BuildUniqueMonitorPrefixMap(Dictionary<string, List<string>> prefixIndex)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in prefixIndex)
        {
            if (kvp.Value.Count == 1)
            {
                map[kvp.Key] = kvp.Value[0];
            }
        }

        return map;
    }

    internal static Dictionary<string, string> BuildUniqueMonitorInstanceMap(IEnumerable<string> instances)
    {
        var index = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var instance in instances)
        {
            foreach (var candidate in DashboardInfoHelpers.GetMonitorMatchCandidates(instance))
            {
                if (!index.TryGetValue(candidate.Key, out var list))
                {
                    list = new List<string>();
                    index[candidate.Key] = list;
                }

                if (!list.Contains(instance, StringComparer.OrdinalIgnoreCase))
                {
                    list.Add(instance);
                }
            }
        }

        var unique = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in index)
        {
            if (kvp.Value.Count == 1)
            {
                unique[kvp.Key] = kvp.Value[0];
            }
        }

        return unique;
    }

    internal static bool TryResolveMonitorInstance(
        string? instanceId,
        IReadOnlyDictionary<string, string> uniqueInstanceMap,
        out string matchedInstance,
        out string matchMode,
        out string? matchKey)
    {
        matchedInstance = string.Empty;
        matchMode = "Unmatched";
        matchKey = null;

        foreach (var candidate in DashboardInfoHelpers.GetMonitorMatchCandidates(instanceId))
        {
            if (uniqueInstanceMap.TryGetValue(candidate.Key, out var instance))
            {
                matchedInstance = instance;
                matchMode = candidate.Mode;
                matchKey = candidate.Key;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetEdidFromDescriptorMethods(ManagementObject obj, out byte[] edidBytes)
    {
        edidBytes = Array.Empty<byte>();
        try
        {
            using var inParams = obj.GetMethodParameters("WmiGetMonitorRawEEdidV1Block");
            inParams["BlockId"] = 0;
            using var outParams = obj.InvokeMethod("WmiGetMonitorRawEEdidV1Block", inParams, null);
            if (outParams != null &&
                outParams["BlockContent"] is byte[] content &&
                content.Length >= 128)
            {
                edidBytes = content;
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private static DashboardInfoHelpers.EdidInfo MergeEdidInfo(
        DashboardInfoHelpers.EdidInfo edidInfo,
        string? manufacturer,
        string? product,
        string? serial,
        string? monitorName,
        (int? Horizontal, int? Vertical) sizeInfo)
    {
        var merged = edidInfo;

        if (string.IsNullOrWhiteSpace(merged.ManufacturerId) && !string.IsNullOrWhiteSpace(manufacturer))
        {
            merged = merged with { ManufacturerId = manufacturer };
        }

        if (string.IsNullOrWhiteSpace(merged.ProductCodeHex) && !string.IsNullOrWhiteSpace(product))
        {
            merged = merged with { ProductCodeHex = product };
        }

        if (string.IsNullOrWhiteSpace(merged.SerialNumber) && !string.IsNullOrWhiteSpace(serial))
        {
            merged = merged with { SerialNumber = serial };
        }

        if (string.IsNullOrWhiteSpace(merged.MonitorName) && !string.IsNullOrWhiteSpace(monitorName))
        {
            merged = merged with { MonitorName = monitorName };
        }

        if (!merged.HorizontalSizeCm.HasValue && sizeInfo.Horizontal.HasValue)
        {
            merged = merged with { HorizontalSizeCm = sizeInfo.Horizontal };
        }

        if (!merged.VerticalSizeCm.HasValue && sizeInfo.Vertical.HasValue)
        {
            merged = merged with { VerticalSizeCm = sizeInfo.Vertical };
        }

        return merged;
    }

    private const int EnumCurrentSettings = -1;

    [Flags]
    private enum DisplayDeviceStateFlags : int
    {
        AttachedToDesktop = 0x1,
        MultiDriver = 0x2,
        PrimaryDevice = 0x4,
        MirroringDriver = 0x8,
        VgaCompatible = 0x10,
        Removable = 0x20,
        ModesPruned = 0x08000000,
        Remote = 0x04000000,
        Disconnect = 0x02000000
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayDevice
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        public DisplayDeviceStateFlags StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DevMode
    {
        private const int CchDeviceName = 32;
        private const int CchFormName = 32;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CchDeviceName)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CchFormName)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DevMode lpDevMode);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DisplayDevice lpDisplayDevice, uint dwFlags);
}
