using System.Collections.Generic;
using OpenTraceProject.Core;
using OpenTraceProject.Core.Plugins;

namespace OpenTraceProject.Plugins.DevTools;

public sealed class DevToolsPlugin : ITweakPlugin
{
    public string PluginName => "Developer Tools Plugin";
    public string Author => "Open Trace Project Team";
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
