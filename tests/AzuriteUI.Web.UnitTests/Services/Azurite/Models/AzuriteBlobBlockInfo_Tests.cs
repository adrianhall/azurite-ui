using AzuriteUI.Web.Services.Azurite.Models;

namespace AzuriteUI.Web.UnitTests.Services.Azurite.Models;

[ExcludeFromCodeCoverage]
public class AzuriteBlobBlockInfo_Tests
{
    #region IsSuccess Property

    [Fact(Timeout = 15000)]
    public void IsSuccess_WithStatusCode200_ShouldReturnTrue()
    {
        // Arrange
        var blockInfo = new AzuriteBlobBlockInfo
        {
            BlockId = "test-block-id",
            StatusCode = 200
        };

        // Act
        var result = blockInfo.IsSuccess;

        // Assert
        result.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public void IsSuccess_WithStatusCode201_ShouldReturnTrue()
    {
        // Arrange
        var blockInfo = new AzuriteBlobBlockInfo
        {
            BlockId = "test-block-id",
            StatusCode = 201
        };

        // Act
        var result = blockInfo.IsSuccess;

        // Assert
        result.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public void IsSuccess_WithStatusCode299_ShouldReturnTrue()
    {
        // Arrange
        var blockInfo = new AzuriteBlobBlockInfo
        {
            BlockId = "test-block-id",
            StatusCode = 299
        };

        // Act
        var result = blockInfo.IsSuccess;

        // Assert
        result.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public void IsSuccess_WithStatusCode199_ShouldReturnFalse()
    {
        // Arrange
        var blockInfo = new AzuriteBlobBlockInfo
        {
            BlockId = "test-block-id",
            StatusCode = 199
        };

        // Act
        var result = blockInfo.IsSuccess;

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public void IsSuccess_WithStatusCode300_ShouldReturnFalse()
    {
        // Arrange
        var blockInfo = new AzuriteBlobBlockInfo
        {
            BlockId = "test-block-id",
            StatusCode = 300
        };

        // Act
        var result = blockInfo.IsSuccess;

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public void IsSuccess_WithStatusCode400_ShouldReturnFalse()
    {
        // Arrange
        var blockInfo = new AzuriteBlobBlockInfo
        {
            BlockId = "test-block-id",
            StatusCode = 400
        };

        // Act
        var result = blockInfo.IsSuccess;

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public void IsSuccess_WithStatusCode500_ShouldReturnFalse()
    {
        // Arrange
        var blockInfo = new AzuriteBlobBlockInfo
        {
            BlockId = "test-block-id",
            StatusCode = 500
        };

        // Act
        var result = blockInfo.IsSuccess;

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Property Tests

    [Fact(Timeout = 15000)]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var blockId = "block-123";
        var contentMD5 = "abc123";
        var statusCode = 201;

        // Act
        var blockInfo = new AzuriteBlobBlockInfo
        {
            BlockId = blockId,
            ContentMD5 = contentMD5,
            StatusCode = statusCode
        };

        // Assert
        blockInfo.BlockId.Should().Be(blockId);
        blockInfo.ContentMD5.Should().Be(contentMD5);
        blockInfo.StatusCode.Should().Be(statusCode);
    }

    [Fact(Timeout = 15000)]
    public void ContentMD5_CanBeNull()
    {
        // Arrange & Act
        var blockInfo = new AzuriteBlobBlockInfo
        {
            BlockId = "test-block-id",
            ContentMD5 = null,
            StatusCode = 200
        };

        // Assert
        blockInfo.ContentMD5.Should().BeNull();
    }

    #endregion
}
