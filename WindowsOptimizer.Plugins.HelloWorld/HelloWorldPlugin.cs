using System.Collections.Generic;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Plugins;

namespace WindowsOptimizer.Plugins.HelloWorld;

public sealed class HelloWorldPlugin : ITweakPlugin
{
    public string PluginName => "Hello World Plugin";
    public string Author => "Windows Optimizer Team";
    public string Version => "1.0.0";

    public IEnumerable<ITweak> GetTweaks()
    {
        return new List<ITweak>
        {
            new HelloWorldTweak()
        };
    }
}
