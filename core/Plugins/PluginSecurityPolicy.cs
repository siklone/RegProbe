namespace RegProbe.Core.Plugins;

public static class PluginSecurityPolicy
{
    // Dynamic plugin loading stays disabled until real Authenticode verification
    // and stronger isolation are implemented.
    public static bool DynamicLoadingEnabled =>
        AppContext.TryGetSwitch("RegProbe.Plugins.EnableDynamicLoading", out var enabled) && enabled;
}
