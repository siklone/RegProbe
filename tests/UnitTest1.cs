using OpenTraceProject.Infrastructure;

namespace OpenTraceProject.Tests;

public class UnitTest1
{
    [Fact]
    public void AppPathsBuildsExpectedLocations()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "OpenTraceProjectTests", Guid.NewGuid().ToString("N"));
        var paths = new AppPaths(basePath);

        Assert.EndsWith(Path.Combine(AppPaths.AppFolderName, "settings.json"), paths.SettingsFilePath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(Path.Combine(AppPaths.AppFolderName, "logs", "app.log"), paths.LogFilePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SettingsStorePersistsSettings()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "OpenTraceProjectTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(basePath);

        try
        {
            var paths = new AppPaths(basePath);
            var store = new SettingsStore(paths);
            var settings = new AppSettings { SchemaVersion = 2 };

            await store.SaveAsync(settings, CancellationToken.None);
            var loaded = await store.LoadAsync(CancellationToken.None);

            Assert.Equal(2, loaded.SchemaVersion);
            Assert.True(File.Exists(paths.SettingsFilePath));
        }
        finally
        {
            if (Directory.Exists(basePath))
            {
                Directory.Delete(basePath, true);
            }
        }
    }
}
