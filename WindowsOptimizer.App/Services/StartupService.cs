using System.IO;
using Microsoft.Win32;
using WindowsOptimizer.App.Models;

namespace WindowsOptimizer.App.Services;

/// <summary>
/// Service for managing Windows startup items.
/// </summary>
public class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunKeyPathLM = @"Software\Microsoft\Windows\CurrentVersion\Run";
    
    /// <summary>
    /// Gets all startup items from registry and startup folders.
    /// </summary>
    public async Task<List<StartupItem>> GetAllStartupItemsAsync()
    {
        return await Task.Run(() =>
        {
            var items = new List<StartupItem>();
            
            // Add registry items (HKCU)
            items.AddRange(GetRegistryStartupItems(Registry.CurrentUser, RunKeyPath, StartupLocation.RegistryCurrentUser));
            
            // Add registry items (HKLM)
            items.AddRange(GetRegistryStartupItems(Registry.LocalMachine, RunKeyPathLM, StartupLocation.RegistryLocalMachine));
            
            // Add startup folder items (User)
            items.AddRange(GetFolderStartupItems(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                StartupLocation.StartupFolderUser));
            
            // Add startup folder items (Common/All Users)
            items.AddRange(GetFolderStartupItems(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
                StartupLocation.StartupFolderCommon));
            
            return items;
        });
    }

    /// <summary>
    /// Disables a startup item by moving it or renaming registry value.
    /// </summary>
    public async Task<bool> DisableStartupItemAsync(StartupItem item)
    {
        return await Task.Run(() =>
        {
            try
            {
                switch (item.Location)
                {
                    case StartupLocation.RegistryCurrentUser:
                        return DisableRegistryItem(Registry.CurrentUser, RunKeyPath, item.Name);
                    
                    case StartupLocation.RegistryLocalMachine:
                        return DisableRegistryItem(Registry.LocalMachine, RunKeyPathLM, item.Name);
                    
                    case StartupLocation.StartupFolderUser:
                    case StartupLocation.StartupFolderCommon:
                        return DisableFolderItem(item.Command);
                    
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Enables a previously disabled startup item.
    /// </summary>
    public async Task<bool> EnableStartupItemAsync(StartupItem item)
    {
        return await Task.Run(() =>
        {
            try
            {
                switch (item.Location)
                {
                    case StartupLocation.RegistryCurrentUser:
                        return EnableRegistryItem(Registry.CurrentUser, RunKeyPath, item.Name);
                    
                    case StartupLocation.RegistryLocalMachine:
                        return EnableRegistryItem(Registry.LocalMachine, RunKeyPathLM, item.Name);
                    
                    case StartupLocation.StartupFolderUser:
                    case StartupLocation.StartupFolderCommon:
                        return EnableFolderItem(item.Command);
                    
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        });
    }

    private List<StartupItem> GetRegistryStartupItems(RegistryKey root, string path, StartupLocation location)
    {
        var items = new List<StartupItem>();
        
        try
        {
            using var key = root.OpenSubKey(path);
            if (key == null) return items;

            foreach (var valueName in key.GetValueNames())
            {
                if (string.IsNullOrWhiteSpace(valueName)) continue;

                var command = key.GetValue(valueName)?.ToString() ?? "";
                var publisher = ExtractPublisher(command);

                items.Add(new StartupItem(
                    Id: $"{location}_{valueName}",
                    Name: valueName,
                    Command: command,
                    Publisher: publisher,
                    Location: location,
                    Impact: CalculateImpact(command),
                    IsEnabled: true,
                    LastModified: null
                ));
            }

            // Check for disabled items (-Disabled suffix key)
            using var disabledKey = root.OpenSubKey(path + "-Disabled");
            if (disabledKey != null)
            {
                foreach (var valueName in disabledKey.GetValueNames())
                {
                    if (string.IsNullOrWhiteSpace(valueName)) continue;

                    var command = disabledKey.GetValue(valueName)?.ToString() ?? "";
                    var publisher = ExtractPublisher(command);

                    items.Add(new StartupItem(
                        Id: $"{location}_disabled_{valueName}",
                        Name: valueName,
                        Command: command,
                        Publisher: publisher,
                        Location: location,
                        Impact: CalculateImpact(command),
                        IsEnabled: false,
                        LastModified: null
                    ));
                }
            }
        }
        catch
        {
            // Ignore errors reading registry
        }

        return items;
    }

    private List<StartupItem> GetFolderStartupItems(string folderPath, StartupLocation location)
    {
        var items = new List<StartupItem>();

        try
        {
            if (!Directory.Exists(folderPath)) return items;

            foreach (var file in Directory.GetFiles(folderPath))
            {
                var fileName = Path.GetFileName(file);
                var isDisabled = fileName.EndsWith(".disabled");
                var displayName = isDisabled ? fileName.Replace(".disabled", "") : fileName;

                items.Add(new StartupItem(
                    Id: $"{location}_{fileName}",
                    Name: displayName,
                    Command: file,
                    Publisher: "Unknown",
                    Location: location,
                    Impact: StartupImpact.Low,
                    IsEnabled: !isDisabled,
                    LastModified: File.GetLastWriteTime(file)
                ));
            }
        }
        catch
        {
            // Ignore folder read errors
        }

        return items;
    }

    private bool DisableRegistryItem(RegistryKey root, string path, string valueName)
    {
        try
        {
            using var sourceKey = root.OpenSubKey(path, writable: true);
            if (sourceKey == null) return false;

            var value = sourceKey.GetValue(valueName);
            if (value == null) return false;

            // Create -Disabled key if doesn't exist
            using var disabledKey = root.CreateSubKey(path + "-Disabled");
            disabledKey.SetValue(valueName, value);

            // Remove from active key
            sourceKey.DeleteValue(valueName);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool EnableRegistryItem(RegistryKey root, string path, string valueName)
    {
        try
        {
            using var disabledKey = root.OpenSubKey(path + "-Disabled", writable: true);
            if (disabledKey == null) return false;

            var value = disabledKey.GetValue(valueName);
            if (value == null) return false;

            // Restore to active key
            using var activeKey = root.CreateSubKey(path);
            activeKey.SetValue(valueName, value);

            // Remove from disabled key
            disabledKey.DeleteValue(valueName);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool DisableFolderItem(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return false;

            var disabledPath = filePath + ".disabled";
            File.Move(filePath, disabledPath);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool EnableFolderItem(string filePath)
    {
        try
        {
            // Remove .disabled extension
            var originalPath = filePath.Replace(".disabled", "");
            
            if (!File.Exists(filePath)) return false;

            File.Move(filePath, originalPath);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private string ExtractPublisher(string command)
    {
        // Try to extract executable name
        var parts = command.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0)
        {
            var exeName = parts[^1].Split(' ')[0].Replace(".exe", "");
            return exeName;
        }

        return "Unknown";
    }

    private StartupImpact CalculateImpact(string command)
    {
        // Simple heuristic based on common heavy startup apps
        var heavyApps = new[] { "chrome", "discord", "steam", "epic", "adobe", "office" };
        var mediumApps = new[] { "dropbox", "onedrive", "spotify", "zoom" };

        var lowerCommand = command.ToLower();

        if (heavyApps.Any(app => lowerCommand.Contains(app)))
            return StartupImpact.High;

        if (mediumApps.Any(app => lowerCommand.Contains(app)))
            return StartupImpact.Medium;

        return StartupImpact.Low;
    }
}
