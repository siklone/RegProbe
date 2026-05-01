using System.Linq;
using Moq;
using Microsoft.Win32;
using RegProbe.Application.Services;
using RegProbe.Application.Services.TweakProviders;
using RegProbe.Core.Commands;
using RegProbe.Core.Files;
using RegProbe.Core;
using RegProbe.Core.Registry;
using RegProbe.Core.Services;
using RegProbe.Core.Tasks;
using RegProbe.Engine;
using RegProbe.Engine.Tweaks;
using RegProbe.Engine.Tweaks.Commands.RegistryOps;
using Xunit;

namespace RegProbe.Tests;

public sealed class MicrosoftCoverageTweakProviderTests
{
    [Fact]
    public void Catalog_Surfaces_GameRecording_Policy_Tweak()
    {
        var catalog = new TweakCatalogService();
        var tweak = Assert.IsType<RegistryValuePresetBatchTweak>(
            catalog.FindById("system.disable-game-recording-broadcasting"));

        Assert.Equal("system.disable-game-recording-broadcasting", tweak.Id);
        Assert.Equal("Windows Game Recording and Broadcasting", tweak.Name);
        Assert.True(tweak.RequiresElevation);
    }

    [Fact]
    public void Catalog_Surfaces_Smb_Client_Metadata_Cache_Tuning()
    {
        var catalog = new TweakCatalogService();
        var tweak = Assert.IsType<RegistryValueBatchTweak>(
            catalog.FindById("network.smb-increase-client-metadata-cache"));

        Assert.True(tweak.RequiresElevation);
        Assert.Equal("network.smb-increase-client-metadata-cache", tweak.Id);
        Assert.Equal("SMB Client Metadata Cache Size Bundle", tweak.Name);
    }

    private static TweakContext BuildContext()
    {
        return new TweakContext(
            new Mock<IRegistryAccessor>(MockBehavior.Loose).Object,
            new Mock<IRegistryAccessor>(MockBehavior.Loose).Object,
            new Mock<IServiceManager>(MockBehavior.Loose).Object,
            new Mock<IScheduledTaskManager>(MockBehavior.Loose).Object,
            new Mock<IFileSystemAccessor>(MockBehavior.Loose).Object,
            new Mock<ICommandRunner>(MockBehavior.Loose).Object);
    }
}
