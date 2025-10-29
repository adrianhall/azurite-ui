using AzuriteUI.Web.Services.Azurite.Models;

namespace AzuriteUI.Web.UnitTests.Services.Azurite.Models;

[ExcludeFromCodeCoverage]
public class AzuriteBlobDownloadResult_Tests
{
    #region IsSuccess Property

    [Fact]
    public void IsSuccess_WithStatusCode200_ShouldReturnTrue()
    {
        // Arrange
        var downloadResult = new AzuriteBlobDownloadResult
        {
            StatusCode = 200
        };

        // Act
        var result = downloadResult.IsSuccess;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSuccess_WithStatusCode206_ShouldReturnTrue()
    {
        // Arrange
        var downloadResult = new AzuriteBlobDownloadResult
        {
            StatusCode = 206
        };

        // Act
        var result = downloadResult.IsSuccess;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSuccess_WithStatusCode299_ShouldReturnTrue()
    {
        // Arrange
        var downloadResult = new AzuriteBlobDownloadResult
        {
            StatusCode = 299
        };

        // Act
        var result = downloadResult.IsSuccess;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSuccess_WithStatusCode199_ShouldReturnFalse()
    {
        // Arrange
        var downloadResult = new AzuriteBlobDownloadResult
        {
            StatusCode = 199
        };

        // Act
        var result = downloadResult.IsSuccess;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSuccess_WithStatusCode300_ShouldReturnFalse()
    {
        // Arrange
        var downloadResult = new AzuriteBlobDownloadResult
        {
            StatusCode = 300
        };

        // Act
        var result = downloadResult.IsSuccess;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSuccess_WithStatusCode404_ShouldReturnFalse()
    {
        // Arrange
        var downloadResult = new AzuriteBlobDownloadResult
        {
            StatusCode = 404
        };

        // Act
        var result = downloadResult.IsSuccess;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSuccess_WithStatusCode500_ShouldReturnFalse()
    {
        // Arrange
        var downloadResult = new AzuriteBlobDownloadResult
        {
            StatusCode = 500
        };

        // Act
        var result = downloadResult.IsSuccess;

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var content = new MemoryStream();
        var contentLength = 1024L;
        var contentRange = "bytes 0-1023/1024";
        var contentType = "application/octet-stream";
        var statusCode = 200;

        // Act
        var downloadResult = new AzuriteBlobDownloadResult
        {
            Content = content,
            ContentLength = contentLength,
            ContentRange = contentRange,
            ContentType = contentType,
            StatusCode = statusCode
        };

        // Assert
        downloadResult.Content.Should().BeSameAs(content);
        downloadResult.ContentLength.Should().Be(contentLength);
        downloadResult.ContentRange.Should().Be(contentRange);
        downloadResult.ContentType.Should().Be(contentType);
        downloadResult.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public void Properties_CanBeNull()
    {
        // Arrange & Act
        var downloadResult = new AzuriteBlobDownloadResult
        {
            Content = null,
            ContentLength = null,
            ContentRange = null,
            ContentType = null,
            StatusCode = 200
        };

        // Assert
        downloadResult.Content.Should().BeNull();
        downloadResult.ContentLength.Should().BeNull();
        downloadResult.ContentRange.Should().BeNull();
        downloadResult.ContentType.Should().BeNull();
    }

    [Fact]
    public void ContentLength_SupportsLargeValues()
    {
        // Arrange
        var largeContentLength = long.MaxValue;

        // Act
        var downloadResult = new AzuriteBlobDownloadResult
        {
            ContentLength = largeContentLength,
            StatusCode = 200
        };

        // Assert
        downloadResult.ContentLength.Should().Be(largeContentLength);
    }

    #endregion
}
