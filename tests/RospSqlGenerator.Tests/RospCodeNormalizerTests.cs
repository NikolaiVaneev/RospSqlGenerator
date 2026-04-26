using RospSqlGenerator.Services;

namespace RospSqlGenerator.Tests;

public sealed class RospCodeNormalizerTests
{
    [Theory]
    [InlineData(77, "1", "77001")]
    [InlineData(77, "001", "77001")]
    [InlineData(77, "77001", "77001")]
    [InlineData(1, "7", "01007")]
    [InlineData(5, "123", "05123")]
    public void Normalize_ReturnsFiveDigitCode(short regionCode, string agencyCode, string expected)
    {
        var result = RospCodeNormalizer.Normalize(regionCode, agencyCode);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_ThrowsWhenAgencyCodeIsEmpty(string agencyCode)
    {
        Assert.Throws<ArgumentException>(() => RospCodeNormalizer.Normalize(77, agencyCode));
    }

    [Fact]
    public void Normalize_ThrowsWhenAgencyCodeIsNotNumeric()
    {
        var exception = Assert.Throws<ArgumentException>(() => RospCodeNormalizer.Normalize(77, "77A01"));

        Assert.Contains("не является числом", exception.Message);
    }
}
