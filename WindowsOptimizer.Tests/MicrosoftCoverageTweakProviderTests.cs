using System.Linq;
using Moq;
using Microsoft.Win32;
using WindowsOptimizer.App.Services.TweakProviders;
using WindowsOptimizer.Core.Commands;
using WindowsOptimizer.Core.Files;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Core.Tasks;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Engine.Tweaks.Commands.RegistryOps;
using Xunit;

namespace WindowsOptimizer.Tests;

public sealed class MicrosoftCoverageTweakProviderTests
{
    [Fact]
    public void SystemRegistryProvider_Exposes_GameRecording_Policy_Tweak()
    {
        var provider = new SystemRegistryTweakProvider();
        var tweaks = provider.CreateTweaks(default!, BuildContext(), false).ToList();

        var tweak = Assert.IsType<DocumentedTweak>(
            tweaks.Single(item => item.Id == "system.disable-game-recording-broadcasting"));
        var inner = Assert.IsType<RegistryCommandBatchTweak>(GetInnerTweak(tweak));

        Assert.Collection(
            inner.Definitions,
            definition =>
            {
                Assert.Equal(RegistryHive.LocalMachine, definition.Hive);
                Assert.Equal(@"SOFTWARE\Policies\Microsoft\Windows\GameDVR", definition.KeyPath);
                Assert.Equal("AllowGameDVR", definition.ValueName);
            });
    }

    [Fact]
    public void NetworkProvider_Exposes_Smb_Client_Metadata_Cache_Tuning()
    {
        var provider = new NetworkTweakProvider();
        var tweaks = provider.CreateTweaks(default!, BuildContext(), false).ToList();

        var tweak = Assert.IsType<DocumentedTweak>(
            tweaks.Single(item => item.Id == "network.smb-increase-client-metadata-cache"));
        var inner = Assert.IsType<RegistryCommandBatchTweak>(GetInnerTweak(tweak));

        Assert.True(inner.RequiresElevation);
        Assert.Equal("network.smb-increase-client-metadata-cache", inner.Id);
        Assert.Equal("SMB: Increase Client Metadata Cache", inner.Name);
    }

    private static ITweak GetInnerTweak(DocumentedTweak tweak)
    {
        var field = typeof(DocumentedTweak).GetField("_innerTweak", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(field);
        return Assert.IsAssignableFrom<ITweak>(field!.GetValue(tweak));
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
