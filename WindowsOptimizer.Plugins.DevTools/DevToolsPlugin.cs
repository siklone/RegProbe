using System.Collections.Generic;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Plugins;

namespace WindowsOptimizer.Plugins.DevTools;

public sealed class DevToolsPlugin : ITweakPlugin
{
    public string PluginName => "Developer Tools Plugin";
    public string Author => "Windows Optimizer Team";
    public string Version => "1.0.0";

    public IEnumerable<ITweak> GetTweaks()
    {
        return new List<ITweak>
        {
            new GitConfigTweak(),
            new GitCredentialHelperTweak(),
            new SshKeyCheckTweak()
        };
    }
}
