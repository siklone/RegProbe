using System.Threading;
using System.Threading.Tasks;
using Moq;
using Microsoft.Win32;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Infrastructure.Registry;

namespace WindowsOptimizer.Tests;

public sealed class HybridRegistryAccessorTests
{
    private static readonly RegistryValueReference SampleReference = new(
        RegistryHive.LocalMachine,
        RegistryView.Registry64,
        @"Software\WindowsOptimizer\Tests",
        "Sample");

    [Fact]
    public async Task ReadValueAsync_UsesReadAccessor()
    {
        var readAccessor = new Mock<IRegistryAccessor>(MockBehavior.Strict);
        var writeAccessor = new Mock<IRegistryAccessor>(MockBehavior.Strict);

        var expected = new RegistryValueReadResult(false, null);
        readAccessor
            .Setup(x => x.ReadValueAsync(SampleReference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var hybrid = new HybridRegistryAccessor(readAccessor.Object, writeAccessor.Object);

        var result = await hybrid.ReadValueAsync(SampleReference, CancellationToken.None);

        Assert.Equal(expected.Exists, result.Exists);
        readAccessor.Verify(x => x.ReadValueAsync(SampleReference, It.IsAny<CancellationToken>()), Times.Once);
        writeAccessor.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SetValueAsync_UsesWriteAccessor()
    {
        var readAccessor = new Mock<IRegistryAccessor>(MockBehavior.Strict);
        var writeAccessor = new Mock<IRegistryAccessor>(MockBehavior.Strict);

        var value = RegistryValueData.FromObject(RegistryValueKind.DWord, 1);
        writeAccessor
            .Setup(x => x.SetValueAsync(SampleReference, value, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hybrid = new HybridRegistryAccessor(readAccessor.Object, writeAccessor.Object);
        await hybrid.SetValueAsync(SampleReference, value, CancellationToken.None);

        writeAccessor.Verify(x => x.SetValueAsync(SampleReference, value, It.IsAny<CancellationToken>()), Times.Once);
        readAccessor.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteValueAsync_UsesWriteAccessor()
    {
        var readAccessor = new Mock<IRegistryAccessor>(MockBehavior.Strict);
        var writeAccessor = new Mock<IRegistryAccessor>(MockBehavior.Strict);

        writeAccessor
            .Setup(x => x.DeleteValueAsync(SampleReference, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hybrid = new HybridRegistryAccessor(readAccessor.Object, writeAccessor.Object);
        await hybrid.DeleteValueAsync(SampleReference, CancellationToken.None);

        writeAccessor.Verify(x => x.DeleteValueAsync(SampleReference, It.IsAny<CancellationToken>()), Times.Once);
        readAccessor.VerifyNoOtherCalls();
    }
}
