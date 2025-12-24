using System.Collections.Generic;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Plugins;

namespace WindowsOptimizer.Plugins.HelloWorld;

public sealed class HelloWorldPlugin : ITweakPlugin
{
    public string PluginName => "Hello World Demo Plugin";
    public string Author => "Antigravity AI";
    public string Version => "1.0.0";

    public IEnumerable<ITweak> GetTweaks()
    {
        yield return new HelloWorldTweak();
    }
}
