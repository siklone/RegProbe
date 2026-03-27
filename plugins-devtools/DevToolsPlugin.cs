using System.Collections.Generic;
using RegProbe.Core;
using RegProbe.Core.Plugins;

namespace RegProbe.Plugins.DevTools;

public sealed class DevToolsPlugin : ITweakPlugin
{
    public string PluginName => "Developer Tools Plugin";
    public string Author => "RegProbe Team";
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
