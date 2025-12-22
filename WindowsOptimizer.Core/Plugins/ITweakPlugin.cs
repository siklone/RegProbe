using System.Collections.Generic;
using WindowsOptimizer.Core.Tweaks;

namespace WindowsOptimizer.Core.Plugins;

public interface ITweakPlugin
{
    string PluginName { get; }
    string Author { get; }
    string Version { get; }
    IEnumerable<ITweak> GetTweaks();
}
