using Moq;
using RegProbe.Core;
using RegProbe.Core.Registry;
using RegProbe.Engine.Tweaks.Peripheral;

namespace RegProbe.Tests;

public sealed class AudioEnhancementsTweakTests
{
    [Fact]
    public async Task DetectAsync_WhenEndpointTargetsExist_ReturnsProtectedAclNotApplicable()
    {
        var tweak = new AudioEnhancementsTweak(
            Mock.Of<IRegistryAccessor>(),
            () => new[] { @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render\{device}\Properties\{b3f8fa53-0004-438e-9003-51a46e139bfc},3" });

        var result = await tweak.DetectAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.NotApplicable, result.Status);
        Assert.Contains("MMDevices audio endpoint enhancement keys are protected", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1 protected device-scoped target values", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyVerifyRollback_DoNotWriteRegistry_WhenMmDevicesTargetsAreProtected()
    {
        var registry = new Mock<IRegistryAccessor>(MockBehavior.Strict);
        var tweak = new AudioEnhancementsTweak(
            registry.Object,
            () => new[] { @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Capture\{device}\FxProperties\{1da5d803-d492-4edd-8c23-e0c0ffee7f0e},5" });

        var apply = await tweak.ApplyAsync(CancellationToken.None);
        var verify = await tweak.VerifyAsync(CancellationToken.None);
        var rollback = await tweak.RollbackAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.NotApplicable, apply.Status);
        Assert.Equal(TweakStatus.NotApplicable, verify.Status);
        Assert.Equal(TweakStatus.NotApplicable, rollback.Status);
        registry.VerifyNoOtherCalls();
    }

    [Fact]
    public void CreateDisableAudioEnhancementsTweak_ReturnsGuardedNotApplicableTweak()
    {
        var tweak = AudioTweaks.CreateDisableAudioEnhancementsTweak(Mock.Of<IRegistryAccessor>());

        Assert.IsType<AudioEnhancementsTweak>(tweak);
        Assert.Equal("peripheral.audio-disable-enhancements", tweak.Id);
        Assert.True(tweak.RequiresElevation);
        Assert.Contains("skips mutation", tweak.Description, StringComparison.OrdinalIgnoreCase);
    }
}
