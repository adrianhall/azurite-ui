using AzuriteUI.Web.Extensions;
using Microsoft.Net.Http.Headers;

namespace AzuriteUI.Web.UnitTests.Extensions;

[ExcludeFromCodeCoverage]
public class SystemExtensions_Tests
{
    #region OrDefault Tests

    [Fact(Timeout = 15000)]
    public void OrDefault_WithNull_ShouldReturnDefault()
    {
        // Arrange
        string? value = null;
        var defaultValue = "default";

        // Act
        var result = value.OrDefault(defaultValue);

        // Assert
        result.Should().Be("default");
    }

    [Fact(Timeout = 15000)]
    public void OrDefault_WithEmptyString_ShouldReturnDefault()
    {
        // Arrange
        var value = "";
        var defaultValue = "default";

        // Act
        var result = value.OrDefault(defaultValue);

        // Assert
        result.Should().Be("default");
    }

    [Fact(Timeout = 15000)]
    public void OrDefault_WithWhitespaceSpaces_ShouldReturnDefault()
    {
        // Arrange
        var value = "   ";
        var defaultValue = "default";

        // Act
        var result = value.OrDefault(defaultValue);

        // Assert
        result.Should().Be("default");
    }

    [Fact(Timeout = 15000)]
    public void OrDefault_WithWhitespaceTabs_ShouldReturnDefault()
    {
        // Arrange
        var value = "\t\t\t";
        var defaultValue = "default";

        // Act
        var result = value.OrDefault(defaultValue);

        // Assert
        result.Should().Be("default");
    }

    [Fact(Timeout = 15000)]
    public void OrDefault_WithWhitespaceNewlines_ShouldReturnDefault()
    {
        // Arrange
        var value = "\r\n\r\n";
        var defaultValue = "default";

        // Act
        var result = value.OrDefault(defaultValue);

        // Assert
        result.Should().Be("default");
    }

    [Fact(Timeout = 15000)]
    public void OrDefault_WithMixedWhitespace_ShouldReturnDefault()
    {
        // Arrange
        var value = " \t\r\n ";
        var defaultValue = "default";

        // Act
        var result = value.OrDefault(defaultValue);

        // Assert
        result.Should().Be("default");
    }

    [Fact(Timeout = 15000)]
    public void OrDefault_WithValidString_ShouldReturnOriginalValue()
    {
        // Arrange
        var value = "valid-value";
        var defaultValue = "default";

        // Act
        var result = value.OrDefault(defaultValue);

        // Assert
        result.Should().Be("valid-value");
    }

    [Fact(Timeout = 15000)]
    public void OrDefault_WithStringContainingWhitespace_ShouldReturnOriginalValue()
    {
        // Arrange
        var value = " valid value ";
        var defaultValue = "default";

        // Act
        var result = value.OrDefault(defaultValue);

        // Assert
        result.Should().Be(" valid value ");
    }

    [Fact(Timeout = 15000)]
    public void OrDefault_WithDifferentDefaultValue_ShouldReturnThatDefault()
    {
        // Arrange
        string? value = null;
        var defaultValue = "custom-default";

        // Act
        var result = value.OrDefault(defaultValue);

        // Assert
        result.Should().Be("custom-default");
    }

    [Fact(Timeout = 15000)]
    public void OrDefault_WithEmptyStringAsDefault_ShouldReturnEmptyString()
    {
        // Arrange
        string? value = null;
        var defaultValue = "";

        // Act
        var result = value.OrDefault(defaultValue);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public void OrDefault_WithSingleCharacter_ShouldReturnOriginalValue()
    {
        // Arrange
        var value = "a";
        var defaultValue = "default";

        // Act
        var result = value.OrDefault(defaultValue);

        // Assert
        result.Should().Be("a");
    }

    #endregion

    #region Matches Tests

    [Fact(Timeout = 15000)]
    public void Matches_WithWildcardETag_ShouldReturnTrue()
    {
        // Arrange
        var etag = EntityTagHeaderValue.Any;
        var etagValue = "any-value";

        // Act
        var result = etag.Matches(etagValue);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public void Matches_WithMatchingETag_ShouldReturnTrue()
    {
        // Arrange
        var etag = new EntityTagHeaderValue("\"test-etag\"");
        var etagValue = "test-etag";

        // Act
        var result = etag.Matches(etagValue);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public void Matches_WithMatchingETagWithQuotes_ShouldReturnTrue()
    {
        // Arrange
        var etag = new EntityTagHeaderValue("\"test-etag\"");
        var etagValue = "\"test-etag\"";

        // Act
        var result = etag.Matches(etagValue);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public void Matches_WithNonMatchingETag_ShouldReturnFalse()
    {
        // Arrange
        var etag = new EntityTagHeaderValue("\"test-etag\"");
        var etagValue = "different-etag";

        // Act
        var result = etag.Matches(etagValue);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public void Matches_WithWeakETag_ShouldReturnFalse()
    {
        // Arrange
        var etag = new EntityTagHeaderValue("\"test-etag\"", isWeak: true);
        var etagValue = "test-etag";

        // Act
        var result = etag.Matches(etagValue);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public void Matches_WithEmptyETag_ShouldReturnFalse()
    {
        // Arrange
        var etag = new EntityTagHeaderValue("\"\"");
        var etagValue = "test-etag";

        // Act
        var result = etag.Matches(etagValue);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public void Matches_WithEmptyETagValue_ShouldReturnFalse()
    {
        // Arrange
        var etag = new EntityTagHeaderValue("\"test-etag\"");
        var etagValue = "";

        // Act
        var result = etag.Matches(etagValue);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public void Matches_WithCaseSensitiveETag_ShouldReturnFalse()
    {
        // Arrange
        var etag = new EntityTagHeaderValue("\"Test-ETag\"");
        var etagValue = "test-etag";

        // Act
        var result = etag.Matches(etagValue);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public void Matches_WithExactCaseMatch_ShouldReturnTrue()
    {
        // Arrange
        var etag = new EntityTagHeaderValue("\"Test-ETag\"");
        var etagValue = "Test-ETag";

        // Act
        var result = etag.Matches(etagValue);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public void Matches_WithAzureStyleETag_ShouldReturnTrue()
    {
        // Arrange
        var etag = new EntityTagHeaderValue("\"0x8D9F7B3C2A1B0E5\"");
        var etagValue = "0x8D9F7B3C2A1B0E5";

        // Act
        var result = etag.Matches(etagValue);

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}
