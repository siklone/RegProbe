using Moq;
using RegProbe.Core.Commands;
using RegProbe.Engine.Tweaks.Commands.Security;

namespace RegProbe.Tests;

public sealed class DisableSystemMitigationsTweakTests
{
    [Fact]
    public void DisableSystemMitigationsTweak_CanBeConstructed()
    {
        var mockRunner = new Mock<ICommandRunner>();

        var tweak = new DisableSystemMitigationsTweak(mockRunner.Object);

        Assert.Equal("security.disable-system-mitigations", tweak.Id);
        Assert.Equal("Disable System Mitigations", tweak.Name);
    }
}
