using OpenTraceProject.App.Services;

namespace OpenTraceProject.Tests;

public sealed class WinConfigCatalogParserTests
{
    [Fact]
    public void ExtractLeadParagraph_SkipsHeadingsAndCodeBlocks()
    {
        const string markdown = """
        # Encrypted DNS

        The DNS server gets applied via registry and the category mixes privacy and transport notes.

        ```powershell
        reg add HKLM\Foo
        ```

        # SMB Configuration
        """;

        var summary = WinConfigCatalogParser.ExtractLeadParagraph(markdown);

        Assert.Equal("The DNS server gets applied via registry and the category mixes privacy and transport notes.", summary);
    }

    [Fact]
    public void ExtractTopLevelTopics_ReturnsH1Topics()
    {
        const string markdown = """
        # Encrypted DNS
        text

        ## Nested
        text

        # SMB Configuration
        text

        # QoS Policy
        """;

        var topics = WinConfigCatalogParser.ExtractTopLevelTopics(markdown);

        Assert.Equal(new[] { "Encrypted DNS", "SMB Configuration", "QoS Policy" }, topics);
    }

    [Theory]
    [InlineData("network/desc.md", WinConfigCatalogFileKind.Documentation)]
    [InlineData("network/assets/QoS-Policy.ps1", WinConfigCatalogFileKind.Script)]
    [InlineData("network/images/qosvalues.png", WinConfigCatalogFileKind.Asset)]
    [InlineData("network/assets/notes.txt", WinConfigCatalogFileKind.Data)]
    public void ClassifyFile_UsesExtensionBasedBuckets(string path, WinConfigCatalogFileKind expected)
    {
        var kind = WinConfigCatalogParser.ClassifyFile(path);

        Assert.Equal(expected, kind);
    }
}
