using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using WindowsOptimizer.Core.Plugins;

namespace WindowsOptimizer.Infrastructure.Services;

public sealed class PluginLoader
{
    public IEnumerable<ITweakPlugin> LoadPlugins(string pluginsDirectory)
    {
        var plugins = new List<ITweakPlugin>();

        if (!Directory.Exists(pluginsDirectory))
        {
            return plugins;
        }

        var dllFiles = Directory.GetFiles(pluginsDirectory, "*.dll");

        foreach (var file in dllFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(file);
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(ITweakPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var type in pluginTypes)
                {
                    if (Activator.CreateInstance(type) is ITweakPlugin plugin)
                    {
                        plugins.Add(plugin);
                    }
                }
            }
            catch (Exception ex)
            {
                // In a real app, log this
                Console.WriteLine($"Failed to load plugin from {file}: {ex.Message}");
            }
        }

        return plugins;
    }
}
