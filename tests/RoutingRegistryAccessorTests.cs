using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Moq;
using OpenTraceProject.Core.Registry;
using OpenTraceProject.Infrastructure.Registry;

namespace OpenTraceProject.Tests;

public sealed class RoutingRegistryAccessorTests
{
    private static readonly RegistryValueReference CurrentUserPolicyReference = new(
        RegistryHive.CurrentUser,
        RegistryView.Default,
        @"Software\Policies\Microsoft\Windows\Explorer",
        "DisableSearchBoxSuggestions");

    private static readonly RegistryValueReference CurrentUserNonPolicyReference = new(
        RegistryHive.CurrentUser,
        RegistryView.Default,
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
        "Hidden");

    private static readonly RegistryValueReference LocalMachineReference = new(
        RegistryHive.LocalMachine,
        RegistryView.Default,
        @"Software\Policies\Microsoft\Edge",
        "SearchSuggestEnabled");

    [Fact]
    public async Task SetValueAsync_ForCurrentUserPolicy_PrefersLocalAccessor()
    {
        var localAccessor = new Mock<IRegistryAccessor>(MockBehavior.Strict);
        var elevatedAccessor = new Mock<IRegistryAccessor>(MockBehavior.Strict);
        var value = RegistryValueData.FromObject(RegistryValueKind.DWord, 1);

        localAccessor
            .Setup(x => x.SetValueAsync(CurrentUserPolicyReference, value, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var accessor = new RoutingRegistryAccessor(localAccessor.Object, elevatedAccessor.Object);
        await accessor.SetValueAsync(CurrentUserPolicyReference, value, CancellationToken.None);

        localAccessor.Verify(x => x.SetValueAsync(CurrentUserPolicyReference, value, It.IsAny<CancellationToken>()), Times.Once);
        elevatedAccessor.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SetValueAsync_ForCurrentUserPolicy_FallsBackToElevatedWhenLocalIsDenied()
    {
        var localAccessor = new Mock<IRegistryAccessor>(MockBehavior.Strict);
        var elevatedAccessor = new Mock<IRegistryAccessor>(MockBehavior.Strict);
        var value = RegistryValueData.FromObject(RegistryValueKind.DWord, 1);

        localAccessor
            .Setup(x => x.SetValueAsync(CurrentUserPolicyReference, value, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Denied"));

        elevatedAccessor
            .Setup(x => x.SetValueAsync(CurrentUserPolicyReference, value, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var accessor = new RoutingRegistryAccessor(localAccessor.Object, elevatedAccessor.Object);
        await accessor.SetValueAsync(CurrentUserPolicyReference, value, CancellationToken.None);

        localAccessor.Verify(x => x.SetValueAsync(CurrentUserPolicyReference, value, It.IsAny<CancellationToken>()), Times.Once);
        elevatedAccessor.Verify(x => x.SetValueAsync(CurrentUserPolicyReference, value, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetValueAsync_ForCurrentUserNonPolicy_PrefersLocalAccessor()
    {
        var localAccessor = new Mock<IRegistryAccessor>(MockBehavior.Strict);
        var elevatedAccessor = new Mock<IRegistryAccessor>(MockBehavior.Strict);
        var value = RegistryValueData.FromObject(RegistryValueKind.DWord, 1);

        localAccessor
            .Setup(x => x.SetValueAsync(CurrentUserNonPolicyReference, value, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var accessor = new RoutingRegistryAccessor(localAccessor.Object, elevatedAccessor.Object);
        await accessor.SetValueAsync(CurrentUserNonPolicyReference, value, CancellationToken.None);

        localAccessor.Verify(x => x.SetValueAsync(CurrentUserNonPolicyReference, value, It.IsAny<CancellationToken>()), Times.Once);
        elevatedAccessor.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SetValueAsync_ForLocalMachine_UsesElevatedAccessor()
    {
        var localAccessor = new Mock<IRegistryAccessor>(MockBehavior.Strict);
        var elevatedAccessor = new Mock<IRegistryAccessor>(MockBehavior.Strict);
        var value = RegistryValueData.FromObject(RegistryValueKind.DWord, 0);

        elevatedAccessor
            .Setup(x => x.SetValueAsync(LocalMachineReference, value, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var accessor = new RoutingRegistryAccessor(localAccessor.Object, elevatedAccessor.Object);
        await accessor.SetValueAsync(LocalMachineReference, value, CancellationToken.None);

        elevatedAccessor.Verify(x => x.SetValueAsync(LocalMachineReference, value, It.IsAny<CancellationToken>()), Times.Once);
        localAccessor.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReadValueAsync_ForCurrentUser_FallsBackToElevatedWhenLocalIsDenied()
    {
        var localAccessor = new Mock<IRegistryAccessor>(MockBehavior.Strict);
        var elevatedAccessor = new Mock<IRegistryAccessor>(MockBehavior.Strict);
        var expected = new RegistryValueReadResult(true, RegistryValueData.FromObject(RegistryValueKind.DWord, 1));

        localAccessor
            .Setup(x => x.ReadValueAsync(CurrentUserPolicyReference, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Denied"));

        elevatedAccessor
            .Setup(x => x.ReadValueAsync(CurrentUserPolicyReference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var accessor = new RoutingRegistryAccessor(localAccessor.Object, elevatedAccessor.Object);
        var actual = await accessor.ReadValueAsync(CurrentUserPolicyReference, CancellationToken.None);

        Assert.Equal(expected.Exists, actual.Exists);
        Assert.NotNull(actual.Value);
        Assert.Equal(expected.Value!.ToObject(), actual.Value!.ToObject());
    }
}
