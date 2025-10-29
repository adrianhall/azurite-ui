using Azure.Storage.Blobs.Models;
using AzuriteUI.Web.Extensions;
using AzuriteUI.Web.Services.Azurite.Models;

namespace AzuriteUI.Web.UnitTests.Extensions;

[ExcludeFromCodeCoverage]
public class AzureExtensions_Tests
{
    #region Dequote Tests

    [Fact]
    public void Dequote_WithNull_ShouldReturnEmptyString()
    {
        // Arrange
        string? value = null;

        // Act
        var result = value.Dequote();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Dequote_WithEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var value = "";

        // Act
        var result = value.Dequote();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Dequote_WithWhitespace_ShouldReturnEmptyString()
    {
        // Arrange
        var value = "   ";

        // Act
        var result = value.Dequote();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Dequote_WithQuotedString_ShouldRemoveQuotes()
    {
        // Arrange
        var value = "\"test-value\"";

        // Act
        var result = value.Dequote();

        // Assert
        result.Should().Be("test-value");
    }

    [Fact]
    public void Dequote_WithUnquotedString_ShouldReturnSameString()
    {
        // Arrange
        var value = "test-value";

        // Act
        var result = value.Dequote();

        // Assert
        result.Should().Be("test-value");
    }

    [Fact]
    public void Dequote_WithOnlyLeadingQuote_ShouldReturnSameString()
    {
        // Arrange
        var value = "\"test-value";

        // Act
        var result = value.Dequote();

        // Assert
        result.Should().Be("\"test-value");
    }

    [Fact]
    public void Dequote_WithOnlyTrailingQuote_ShouldReturnSameString()
    {
        // Arrange
        var value = "test-value\"";

        // Act
        var result = value.Dequote();

        // Assert
        result.Should().Be("test-value\"");
    }

    [Fact]
    public void Dequote_WithQuotesInMiddle_ShouldReturnSameString()
    {
        // Arrange
        var value = "test\"quoted\"value";

        // Act
        var result = value.Dequote();

        // Assert
        result.Should().Be("test\"quoted\"value");
    }

    [Fact]
    public void Dequote_WithQuotedEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var value = "\"\"";

        // Act
        var result = value.Dequote();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Dequote_WithQuotedETag_ShouldRemoveQuotes()
    {
        // Arrange
        var value = "\"0x8D9F7B3C2A1B0E5\"";

        // Act
        var result = value.Dequote();

        // Assert
        result.Should().Be("0x8D9F7B3C2A1B0E5");
    }

    [Fact]
    public void Dequote_WithMultipleQuotesAtBothEnds_ShouldRemoveAllQuotesFromEnds()
    {
        // Arrange
        var value = "\"\"test\"\"";

        // Act
        var result = value.Dequote();

        // Assert
        result.Should().Be("test");
    }

    #endregion

    #region ToAzuriteBlobType Tests

    [Fact]
    public void ToAzuriteBlobType_WithNull_ShouldReturnBlock()
    {
        // Arrange
        BlobType? blobType = null;

        // Act
        var result = blobType.ToAzuriteBlobType();

        // Assert
        result.Should().Be(AzuriteBlobType.Block);
    }

    [Fact]
    public void ToAzuriteBlobType_WithBlockBlobType_ShouldReturnBlock()
    {
        // Arrange
        BlobType? blobType = BlobType.Block;

        // Act
        var result = blobType.ToAzuriteBlobType();

        // Assert
        result.Should().Be(AzuriteBlobType.Block);
    }

    [Fact]
    public void ToAzuriteBlobType_WithAppendBlobType_ShouldReturnAppend()
    {
        // Arrange
        BlobType? blobType = BlobType.Append;

        // Act
        var result = blobType.ToAzuriteBlobType();

        // Assert
        result.Should().Be(AzuriteBlobType.Append);
    }

    [Fact]
    public void ToAzuriteBlobType_WithPageBlobType_ShouldReturnPage()
    {
        // Arrange
        BlobType? blobType = BlobType.Page;

        // Act
        var result = blobType.ToAzuriteBlobType();

        // Assert
        result.Should().Be(AzuriteBlobType.Page);
    }

    [Fact]
    public void ToAzuriteBlobType_WithUnsupportedBlobType_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        // Create an invalid BlobType by casting an undefined value
        BlobType? blobType = (BlobType)999;

        // Act
        Action act = () => blobType.ToAzuriteBlobType();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Unsupported blob type*");
    }

    #endregion

    #region ToAzuritePublicAccess Tests

    [Fact]
    public void ToAzuritePublicAccess_WithNonePublicAccessType_ShouldReturnNone()
    {
        // Arrange
        PublicAccessType publicAccess = PublicAccessType.None;

        // Act
        var result = publicAccess.ToAzuritePublicAccess();

        // Assert
        result.Should().Be(AzuritePublicAccess.None);
    }

    [Fact]
    public void ToAzuritePublicAccess_WithBlobPublicAccessType_ShouldReturnBlob()
    {
        // Arrange
        PublicAccessType publicAccess = PublicAccessType.Blob;

        // Act
        var result = publicAccess.ToAzuritePublicAccess();

        // Assert
        result.Should().Be(AzuritePublicAccess.Blob);
    }

    [Fact]
    public void ToAzuritePublicAccess_WithBlobContainerPublicAccessType_ShouldReturnContainer()
    {
        // Arrange
        PublicAccessType publicAccess = PublicAccessType.BlobContainer;

        // Act
        var result = publicAccess.ToAzuritePublicAccess();

        // Assert
        result.Should().Be(AzuritePublicAccess.Container);
    }

    [Fact]
    public void ToAzuritePublicAccess_WithUnsupportedPublicAccessType_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        // Create an invalid PublicAccessType by casting an undefined value
        PublicAccessType publicAccess = (PublicAccessType)999;

        // Act
        Action act = () => publicAccess.ToAzuritePublicAccess();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Unsupported public access type*");
    }

    #endregion

    #region AsOptionalBase64 Tests

    [Fact]
    public void AsOptionalBase64_WithNull_ShouldReturnNull()
    {
        // Arrange
        byte[]? contentHash = null;

        // Act
        var result = contentHash.AsOptionalBase64();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void AsOptionalBase64_WithEmptyByteArray_ShouldReturnEmptyBase64String()
    {
        // Arrange
        var contentHash = Array.Empty<byte>();

        // Act
        var result = contentHash.AsOptionalBase64();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void AsOptionalBase64_WithValidByteArray_ShouldReturnBase64String()
    {
        // Arrange
        var contentHash = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

        // Act
        var result = contentHash.AsOptionalBase64();

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("AQIDBAU=");
    }

    [Fact]
    public void AsOptionalBase64_WithMD5Hash_ShouldReturnValidBase64String()
    {
        // Arrange
        // Example MD5 hash (16 bytes)
        var contentHash = new byte[]
        {
            0xd4, 0x1d, 0x8c, 0xd9, 0x8f, 0x00, 0xb2, 0x04,
            0xe9, 0x80, 0x09, 0x98, 0xec, 0xf8, 0x42, 0x7e
        };

        // Act
        var result = contentHash.AsOptionalBase64();

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("1B2M2Y8AsgTpgAmY7PhCfg==");
    }

    [Fact]
    public void AsOptionalBase64_WithSingleByte_ShouldReturnBase64String()
    {
        // Arrange
        var contentHash = new byte[] { 0xFF };

        // Act
        var result = contentHash.AsOptionalBase64();

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("/w==");
    }

    [Fact]
    public void AsOptionalBase64_WithAllZeros_ShouldReturnValidBase64String()
    {
        // Arrange
        var contentHash = new byte[] { 0x00, 0x00, 0x00, 0x00 };

        // Act
        var result = contentHash.AsOptionalBase64();

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("AAAAAA==");
    }

    [Fact]
    public void AsOptionalBase64_WithContentHashFromAzure_ShouldReturnProperlyFormattedString()
    {
        // Arrange
        // Simulate a typical content hash that might come from Azure
        var contentHash = new byte[]
        {
            0x5d, 0x41, 0x40, 0x2a, 0xbc, 0x4b, 0x2a, 0x76,
            0xb9, 0x71, 0x9d, 0x91, 0x10, 0x17, 0xc5, 0x92
        };

        // Act
        var result = contentHash.AsOptionalBase64();

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        // Verify it's valid Base64 (no exception when converting back)
        var decoded = Convert.FromBase64String(result!);
        decoded.Should().BeEquivalentTo(contentHash);
    }

    #endregion
}
