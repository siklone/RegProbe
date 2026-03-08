using WindowsOptimizer.App.Services;

public sealed class ConfigurationWorkspaceClassifierTests
{
    private readonly ConfigurationWorkspaceClassifier _classifier = new();

    [Fact]
    public void Classify_PlacesCleanupCommandsInMaintenance()
    {
        var workspace = _classifier.Classify("cleanup.temp-files", "Cleanup");

        Assert.Equal(ConfigurationWorkspaceKind.Maintenance, workspace);
    }

    [Fact]
    public void Classify_KeepsReservedStorageInSettings()
    {
        var workspace = _classifier.Classify("cleanup.disable-reserved-storage", "Cleanup");

        Assert.Equal(ConfigurationWorkspaceKind.Settings, workspace);
    }

    [Fact]
    public void Classify_TreatsNetworkRepairCommandsAsMaintenance()
    {
        Assert.Equal(
            ConfigurationWorkspaceKind.Maintenance,
            _classifier.Classify("network.flush-dns-cache", "Network"));

        Assert.Equal(
            ConfigurationWorkspaceKind.Maintenance,
            _classifier.Classify("network.reset-winsock", "Network"));
    }

    [Fact]
    public void Classify_LeavesRegularWindowsSettingsInSettings()
    {
        var workspace = _classifier.Classify("privacy.disable-telemetry", "Privacy");

        Assert.Equal(ConfigurationWorkspaceKind.Settings, workspace);
    }
}
