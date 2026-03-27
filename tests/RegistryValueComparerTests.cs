using Microsoft.Win32;
using RegProbe.Core.Registry;

public sealed class RegistryValueComparerTests
{
    [Fact]
    public void ValuesEqual_ForDword_SignedAndUnsignedRepresentations_Match()
    {
        var equal = RegistryValueComparer.ValuesEqual(RegistryValueKind.DWord, -1, 0xFFFFFFFFL);

        Assert.True(equal);
    }

    [Fact]
    public void ToObject_ForUnsignedDword_RoundTripsToSignedInt32()
    {
        var value = new RegistryValueData(RegistryValueKind.DWord, NumericValue: 0xFFFFFFFFL);

        var result = value.ToObject();

        Assert.IsType<int>(result);
        Assert.Equal(-1, (int)result);
    }
}
