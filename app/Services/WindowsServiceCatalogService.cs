using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using Microsoft.Win32;
using OpenTraceProject.App.Utilities;
using CoreServiceStartMode = OpenTraceProject.Core.Services.ServiceStartMode;

namespace OpenTraceProject.App.Services;

public sealed class WindowsServiceCatalogService
{
    public IReadOnlyList<WindowsServiceCatalogEntry> Collect()
    {
        var results = new List<WindowsServiceCatalogEntry>();
        var statusLookup = BuildServiceStatusLookup();
        var startLookup = BuildServiceStartModeLookup();

        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
            using var servicesKey = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
            if (servicesKey == null)
            {
                return Array.Empty<WindowsServiceCatalogEntry>();
            }

            foreach (var serviceName in servicesKey.GetSubKeyNames())
            {
                try
                {
                    using var serviceKey = servicesKey.OpenSubKey(serviceName);
                    if (serviceKey == null)
                    {
                        continue;
                    }

                    var displayName = serviceKey.GetValue("DisplayName") as string ?? serviceName;
                    var description = serviceKey.GetValue("Description") as string ?? string.Empty;
                    var imagePath = serviceKey.GetValue("ImagePath") as string ?? string.Empty;
                    var objectName = serviceKey.GetValue("ObjectName") as string ?? string.Empty;
                    var group = serviceKey.GetValue("Group") as string ?? string.Empty;
                    var startValue = TryGetInt(serviceKey.GetValue("Start"));
                    var delayedAuto = TryGetInt(serviceKey.GetValue("DelayedAutoStart"));
                    var typeValue = TryGetInt(serviceKey.GetValue("Type"));
                    var serviceDll = string.Empty;

                    using (var parametersKey = serviceKey.OpenSubKey("Parameters"))
                    {
                        serviceDll = parametersKey?.GetValue("ServiceDll") as string ?? string.Empty;
                    }

                    var binaryPath = !string.IsNullOrWhiteSpace(imagePath) ? imagePath : serviceDll;
                    var startMode = ResolveStartMode(startValue, startLookup, serviceName);
                    var isDriver = IsDriverType(typeValue);
                    var statusText = statusLookup.TryGetValue(serviceName, out var status) ? status : "Unknown";

                    results.Add(new WindowsServiceCatalogEntry(
                        serviceName,
                        displayName,
                        description,
                        DescribeServiceType(typeValue),
                        startMode,
                        DescribeStartType(startMode, delayedAuto),
                        statusText,
                        objectName,
                        group,
                        binaryPath,
                        servicesKey.Name + "\\" + serviceName,
                        isDriver,
                        ResolveServiceDocsLink(serviceName)));
                }
                catch
                {
                    // Skip individual registry read failures.
                }
            }
        }
        catch
        {
            return Array.Empty<WindowsServiceCatalogEntry>();
        }

        return results
            .OrderBy(entry => entry.IsDriver)
            .ThenBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Dictionary<string, string> BuildServiceStatusLookup()
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var service in ServiceController.GetServices())
            {
                lookup[service.ServiceName] = service.Status.ToString();
            }
        }
        catch
        {
            // Ignore lookup failures.
        }

        try
        {
            foreach (var device in ServiceController.GetDevices())
            {
                lookup[device.ServiceName] = device.Status.ToString();
            }
        }
        catch
        {
            // Ignore lookup failures.
        }

        return lookup;
    }

    private static Dictionary<string, CoreServiceStartMode> BuildServiceStartModeLookup()
    {
        var lookup = new Dictionary<string, CoreServiceStartMode>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var service in ServiceController.GetServices())
            {
                try
                {
                    lookup[service.ServiceName] = MapServiceStartMode(service.StartType);
                }
                catch
                {
                    // Ignore individual service errors.
                }
            }
        }
        catch
        {
            // Ignore lookup failures.
        }

        return lookup;
    }

    private static CoreServiceStartMode MapServiceStartMode(ServiceStartMode startMode)
    {
        return startMode switch
        {
            ServiceStartMode.Boot => CoreServiceStartMode.Boot,
            ServiceStartMode.System => CoreServiceStartMode.System,
            ServiceStartMode.Automatic => CoreServiceStartMode.Automatic,
            ServiceStartMode.Manual => CoreServiceStartMode.Manual,
            ServiceStartMode.Disabled => CoreServiceStartMode.Disabled,
            _ => CoreServiceStartMode.Unknown
        };
    }

    private static CoreServiceStartMode ResolveStartMode(int? startValue, IReadOnlyDictionary<string, CoreServiceStartMode> startLookup, string serviceName)
    {
        if (startValue.HasValue)
        {
            var mapped = startValue.Value switch
            {
                0 => CoreServiceStartMode.Boot,
                1 => CoreServiceStartMode.System,
                2 => CoreServiceStartMode.Automatic,
                3 => CoreServiceStartMode.Manual,
                4 => CoreServiceStartMode.Disabled,
                _ => CoreServiceStartMode.Unknown
            };

            if (mapped != CoreServiceStartMode.Unknown)
            {
                return mapped;
            }
        }

        return startLookup.TryGetValue(serviceName, out var fallback)
            ? fallback
            : CoreServiceStartMode.Unknown;
    }

    private static string DescribeStartType(CoreServiceStartMode startMode, int? delayedAuto)
    {
        if (startMode == CoreServiceStartMode.Automatic && delayedAuto.GetValueOrDefault() == 1)
        {
            return "Automatic (Delayed)";
        }

        return startMode switch
        {
            CoreServiceStartMode.Boot => "Boot",
            CoreServiceStartMode.System => "System",
            CoreServiceStartMode.Automatic => "Automatic",
            CoreServiceStartMode.Manual => "Manual",
            CoreServiceStartMode.Disabled => "Disabled",
            _ => "Unknown"
        };
    }

    private static string DescribeServiceType(int? typeValue)
    {
        if (!typeValue.HasValue)
        {
            return "Unknown";
        }

        var type = typeValue.Value;
        var labels = new List<string>();
        if ((type & 0x1) != 0) labels.Add("Kernel Driver");
        if ((type & 0x2) != 0) labels.Add("File System Driver");
        if ((type & 0x10) != 0) labels.Add("Win32 Own Process");
        if ((type & 0x20) != 0) labels.Add("Win32 Shared Process");
        if ((type & 0x100) != 0) labels.Add("Interactive");

        return labels.Count > 0 ? string.Join(", ", labels) : $"0x{type:X}";
    }

    private static bool IsDriverType(int? typeValue)
    {
        if (!typeValue.HasValue)
        {
            return false;
        }

        var type = typeValue.Value;
        return (type & 0x1) != 0 || (type & 0x2) != 0;
    }

    private static string ResolveServiceDocsLink(string serviceName)
    {
        var docsRoot = DocsLocator.TryFindDocsRoot();
        if (!string.IsNullOrWhiteSpace(docsRoot))
        {
            var docsDir = Path.Combine(docsRoot, "services");
            var localMd = Path.Combine(docsDir, $"{serviceName}.md");
            var localHtml = Path.Combine(docsDir, $"{serviceName}.html");

            if (File.Exists(localMd)) return localMd;
            if (File.Exists(localHtml)) return localHtml;
        }

        var query = Uri.EscapeDataString($"{serviceName} windows service");
        return $"https://learn.microsoft.com/en-us/search/?terms={query}";
    }

    private static int? TryGetInt(object? value)
    {
        if (value == null)
        {
            return null;
        }

        try
        {
            return Convert.ToInt32(value);
        }
        catch
        {
            return null;
        }
    }
}

public sealed record WindowsServiceCatalogEntry(
    string Name,
    string DisplayName,
    string Description,
    string ServiceType,
    CoreServiceStartMode StartMode,
    string StartType,
    string Status,
    string Account,
    string Group,
    string BinaryPath,
    string RegistryPath,
    bool IsDriver,
    string DocsLink)
{
    public string KindText => IsDriver ? "Driver" : "Service";

    public string DescriptionOrFallback => string.IsNullOrWhiteSpace(Description)
        ? "Windows does not expose a built-in description for this service."
        : Description;
}
