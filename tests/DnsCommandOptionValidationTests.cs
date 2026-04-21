using RegProbe.CLI;
using RegProbe.Application.Models;

namespace RegProbe.Tests;

public sealed class DnsCommandOptionValidationTests
{
    private static readonly DnsProvider[] Providers =
    [
        new("Cloudflare", "desc", "1.1.1.1", "1.0.0.1", "CF"),
        new("Google", "desc", "8.8.8.8", "8.8.4.4", "GO"),
        new("Automatic", "desc", "", "", "DH")
    ];

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ValidateDnsSetOptions_AllowsSupportedCombinations(bool apply, bool flush)
    {
        var error = Program.ValidateDnsSetOptions(apply, flush);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateDnsSetOptions_RejectsFlushWithoutApply()
    {
        var error = Program.ValidateDnsSetOptions(apply: false, flush: true);

        Assert.Equal("--flush requires --apply.", error);
    }

    [Fact]
    public void ValidateKnownDnsProvider_AllowsKnownProviderCaseInsensitively()
    {
        var error = Program.ValidateKnownDnsProvider("  cloudflare  ", Providers);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateKnownDnsProvider_RejectsUnknownProviderWithExpectedList()
    {
        var error = Program.ValidateKnownDnsProvider("quad9", Providers);

        Assert.Equal(
            "Unknown DNS provider: quad9. Expected one of: Automatic, Cloudflare, Google.",
            error);
    }

    [Fact]
    public void FindDnsProviderByName_ResolvesKnownProviderCaseInsensitively()
    {
        var provider = Program.FindDnsProviderByName(Providers, "  GOOGLE ");

        Assert.NotNull(provider);
        Assert.Equal("Google", provider.Name);
    }
}
