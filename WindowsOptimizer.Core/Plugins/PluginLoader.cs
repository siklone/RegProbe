using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Core.Plugins;

/// <summary>
/// Securely loads and manages plugins
/// </summary>
public sealed class PluginLoader
{
    private readonly string _pluginDirectory;
    private readonly Dictionary<string, LoadedPlugin> _loadedPlugins = new();
    private readonly HashSet<string> _trustedPublishers = new();

    public PluginLoader(string pluginDirectory)
    {
        _pluginDirectory = pluginDirectory;
        Directory.CreateDirectory(_pluginDirectory);
    }

    /// <summary>
    /// Discover all available plugins
    /// </summary>
    public async Task<IReadOnlyList<PluginInfo>> DiscoverPluginsAsync(CancellationToken ct)
    {
        var plugins = new List<PluginInfo>();

        var dllFiles = Directory.GetFiles(_pluginDirectory, "*.dll", SearchOption.AllDirectories);

        foreach (var dllPath in dllFiles)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var pluginInfo = await LoadPluginInfoAsync(dllPath, ct);
                if (pluginInfo != null)
                {
                    plugins.Add(pluginInfo);
                }
            }
            catch (Exception ex)
            {
                // Log plugin loading error but continue
                System.Diagnostics.Debug.WriteLine($"Failed to load plugin from {dllPath}: {ex.Message}");
            }
        }

        return plugins;
    }

    /// <summary>
    /// Load plugin info without activating it
    /// </summary>
    private async Task<PluginInfo?> LoadPluginInfoAsync(string dllPath, CancellationToken ct)
    {
        await Task.CompletedTask; // Placeholder for async operations

        // Security: Verify digital signature
        if (!VerifyDigitalSignature(dllPath))
        {
            return null;
        }

        // Load assembly in metadata-only mode first
        var assemblyName = AssemblyName.GetAssemblyName(dllPath);

        return new PluginInfo
        {
            AssemblyPath = dllPath,
            AssemblyName = assemblyName.Name ?? "Unknown",
            Version = assemblyName.Version?.ToString() ?? "0.0.0.0"
        };
    }

    /// <summary>
    /// Load and activate a plugin
    /// </summary>
    public async Task<IPlugin?> LoadPluginAsync(string pluginId, CancellationToken ct)
    {
        if (_loadedPlugins.TryGetValue(pluginId, out var loaded))
        {
            return loaded.Instance;
        }

        var plugins = await DiscoverPluginsAsync(ct);
        var pluginInfo = plugins.FirstOrDefault(p => p.AssemblyName == pluginId);

        if (pluginInfo == null)
        {
            return null;
        }

        try
        {
            // Load assembly
            var assembly = Assembly.LoadFrom(pluginInfo.AssemblyPath);

            // Find IPlugin implementations
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            var pluginType = pluginTypes.FirstOrDefault();
            if (pluginType == null)
            {
                return null;
            }

            // Create instance
            var instance = (IPlugin?)Activator.CreateInstance(pluginType);
            if (instance == null)
            {
                return null;
            }

            _loadedPlugins[pluginId] = new LoadedPlugin
            {
                Info = pluginInfo,
                Instance = instance,
                LoadedAt = DateTime.UtcNow
            };

            return instance;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to activate plugin {pluginId}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Unload a plugin
    /// </summary>
    public void UnloadPlugin(string pluginId)
    {
        if (_loadedPlugins.TryGetValue(pluginId, out var loaded))
        {
            loaded.Instance.Dispose();
            _loadedPlugins.Remove(pluginId);
        }
    }

    /// <summary>
    /// Verify digital signature of plugin DLL
    /// </summary>
    private bool VerifyDigitalSignature(string dllPath)
    {
        // TODO: Implement Authenticode signature verification
        // For now, just check file exists and has .dll extension
        return File.Exists(dllPath) && Path.GetExtension(dllPath).Equals(".dll", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Add a trusted publisher
    /// </summary>
    public void AddTrustedPublisher(string publisherThumbprint)
    {
        _trustedPublishers.Add(publisherThumbprint);
    }

    /// <summary>
    /// Get all loaded plugins
    /// </summary>
    public IReadOnlyList<LoadedPlugin> GetLoadedPlugins()
    {
        return _loadedPlugins.Values.ToList();
    }
}

/// <summary>
/// Information about a discovered plugin
/// </summary>
public sealed class PluginInfo
{
    public required string AssemblyPath { get; init; }
    public required string AssemblyName { get; init; }
    public required string Version { get; init; }
}

/// <summary>
/// A loaded plugin instance
/// </summary>
public sealed class LoadedPlugin
{
    public required PluginInfo Info { get; init; }
    public required IPlugin Instance { get; init; }
    public required DateTime LoadedAt { get; init; }
}
