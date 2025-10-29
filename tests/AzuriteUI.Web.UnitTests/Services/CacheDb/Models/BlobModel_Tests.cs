using AzuriteUI.Web.Services.Azurite.Models;
using AzuriteUI.Web.Services.CacheDb.Models;

namespace AzuriteUI.Web.UnitTests.Services.CacheDb.Models;

[ExcludeFromCodeCoverage]
public class BlobModel_Tests
{
    #region Equals Tests

    [Fact]
    public void Equals_WithSameContainerNameAndNameAndETag_ShouldReturnTrue()
    {
        // Arrange
        var model1 = new BlobModel
        {
            ContainerName = "test-container",
            Name = "test-blob.txt",
            ETag = "etag123"
        };
        var model2 = new BlobModel
        {
            ContainerName = "test-container",
            Name = "test-blob.txt",
            ETag = "etag123"
        };

        // Act
        var result = model1.Equals(model2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentContainerName_ShouldReturnFalse()
    {
        // Arrange
        var model1 = new BlobModel
        {
            ContainerName = "container-1",
            Name = "test-blob.txt",
            ETag = "etag123"
        };
        var model2 = new BlobModel
        {
            ContainerName = "container-2",
            Name = "test-blob.txt",
            ETag = "etag123"
        };

        // Act
        var result = model1.Equals(model2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentName_ShouldReturnFalse()
    {
        // Arrange
        var model1 = new BlobModel
        {
            ContainerName = "test-container",
            Name = "blob1.txt",
            ETag = "etag123"
        };
        var model2 = new BlobModel
        {
            ContainerName = "test-container",
            Name = "blob2.txt",
            ETag = "etag123"
        };

        // Act
        var result = model1.Equals(model2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentETag_ShouldReturnFalse()
    {
        // Arrange
        var model1 = new BlobModel
        {
            ContainerName = "test-container",
            Name = "test-blob.txt",
            ETag = "etag123"
        };
        var model2 = new BlobModel
        {
            ContainerName = "test-container",
            Name = "test-blob.txt",
            ETag = "etag456"
        };

        // Act
        var result = model1.Equals(model2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var model = new BlobModel
        {
            ContainerName = "test-container",
            Name = "test-blob.txt",
            ETag = "etag123"
        };

        // Act
        var result = model.Equals(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithSelf_ShouldReturnTrue()
    {
        // Arrange
        var model = new BlobModel
        {
            ContainerName = "test-container",
            Name = "test-blob.txt",
            ETag = "etag123"
        };

        // Act
        var result = model.Equals(model);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentOtherProperties_ShouldReturnTrue()
    {
        // Arrange
        var model1 = new BlobModel
        {
            ContainerName = "test-container",
            Name = "test-blob.txt",
            ETag = "etag123",
            BlobType = AzuriteBlobType.Block,
            ContentLength = 100,
            ContentType = "text/plain",
            CreatedOn = DateTimeOffset.UtcNow,
            Tags = new Dictionary<string, string> { { "key1", "value1" } }
        };
        var model2 = new BlobModel
        {
            ContainerName = "test-container",
            Name = "test-blob.txt",
            ETag = "etag123",
            BlobType = AzuriteBlobType.Page,
            ContentLength = 200,
            ContentType = "application/json",
            CreatedOn = DateTimeOffset.MinValue,
            Tags = new Dictionary<string, string> { { "key2", "value2" } }
        };

        // Act
        var result = model1.Equals(model2);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Object.Equals Tests

    [Fact]
    public void ObjectEquals_WithSameContainerNameAndNameAndETag_ShouldReturnTrue()
    {
        // Arrange
        var model1 = new BlobModel
        {
            ContainerName = "test-container",
            Name = "test-blob.txt",
            ETag = "etag123"
        };
        object model2 = new BlobModel
        {
            ContainerName = "test-container",
            Name = "test-blob.txt",
            ETag = "etag123"
        };

        // Act
        var result = model1.Equals(model2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ObjectEquals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var model = new BlobModel
        {
            ContainerName = "test-container",
            Name = "test-blob.txt",
            ETag = "etag123"
        };
        object other = "test-blob.txt";

        // Act
        var result = model.Equals(other);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ObjectEquals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var model = new BlobModel
        {
            ContainerName = "test-container",
            Name = "test-blob.txt",
            ETag = "etag123"
        };
        object? other = null;

        // Act
        var result = model.Equals(other);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ObjectEquals_WithContainerModel_ShouldReturnFalse()
    {
        // Arrange
        var blob = new BlobModel
        {
            ContainerName = "some-container",
            Name = "test-resource",
            ETag = "etag123"
        };
        object container = new ContainerModel
        {
            Name = "test-resource",
            ETag = "etag123"
        };

        // Act
        var result = blob.Equals(container);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_WithSameContainerNameAndNameAndETag_ShouldReturnSameHashCode()
    {
        // Arrange
        var model1 = new BlobModel
        {
            ContainerName = "test-container",
            Name = "test-blob.txt",
            ETag = "etag123"
        };
        var model2 = new BlobModel
        {
            ContainerName = "test-container",
            Name = "test-blob.txt",
            ETag = "etag123"
        };

        // Act
        var hashCode1 = model1.GetHashCode();
        var hashCode2 = model2.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2);
    }

    [Fact]
    public void GetHashCode_WithDifferentContainerName_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var model1 = new BlobModel
        {
            ContainerName = "container-1",
            Name = "test-blob.txt",
            ETag = "etag123"
        };
        var model2 = new BlobModel
        {
            ContainerName = "container-2",
            Name = "test-blob.txt",
            ETag = "etag123"
        };

        // Act
        var hashCode1 = model1.GetHashCode();
        var hashCode2 = model2.GetHashCode();

        // Assert
        hashCode1.Should().NotBe(hashCode2);
    }

    [Fact]
    public void GetHashCode_WithDifferentName_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var model1 = new BlobModel
        {
            ContainerName = "test-container",
            Name = "blob1.txt",
            ETag = "etag123"
        };
        var model2 = new BlobModel
        {
            ContainerName = "test-container",
            Name = "blob2.txt",
            ETag = "etag123"
        };

        // Act
        var hashCode1 = model1.GetHashCode();
        var hashCode2 = model2.GetHashCode();

        // Assert
        hashCode1.Should().NotBe(hashCode2);
    }

    [Fact]
    public void GetHashCode_WithDifferentETag_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var model1 = new BlobModel
        {
            ContainerName = "test-container",
            Name = "test-blob.txt",
            ETag = "etag123"
        };
        var model2 = new BlobModel
        {
            ContainerName = "test-container",
            Name = "test-blob.txt",
            ETag = "etag456"
        };

        // Act
        var hashCode1 = model1.GetHashCode();
        var hashCode2 = model2.GetHashCode();

        // Assert
        hashCode1.Should().NotBe(hashCode2);
    }

    [Fact]
    public void GetHashCode_CalledMultipleTimes_ShouldReturnSameValue()
    {
        // Arrange
        var model = new BlobModel
        {
            ContainerName = "test-container",
            Name = "test-blob.txt",
            ETag = "etag123"
        };

        // Act
        var hashCode1 = model.GetHashCode();
        var hashCode2 = model.GetHashCode();
        var hashCode3 = model.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2);
        hashCode2.Should().Be(hashCode3);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var model = new BlobModel
        {
            Name = "test",
            ContainerName = "container"
        };

        // Assert
        model.BlobType.Should().Be(AzuriteBlobType.Block);
        model.ContentEncoding.Should().Be(string.Empty);
        model.ContentLanguage.Should().Be(string.Empty);
        model.ContentLength.Should().Be(0L);
        model.ContentType.Should().Be("application/octet-stream");
        model.CreatedOn.Should().Be(DateTimeOffset.MinValue);
        model.ExpiresOn.Should().BeNull();
        model.LastAccessedOn.Should().BeNull();
        model.Tags.Should().NotBeNull();
        model.Tags.Should().BeEmpty();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var model = new BlobModel
        {
            Name = "test-blob.txt",
            ContainerName = "test-container"
        };
        var now = DateTimeOffset.UtcNow;
        var tags = new Dictionary<string, string> { { "key", "value" } };

        // Act
        model.BlobType = AzuriteBlobType.Page;
        model.ContentEncoding = "gzip";
        model.ContentLanguage = "en-US";
        model.ContentLength = 1024L;
        model.ContentType = "text/plain";
        model.CreatedOn = now;
        model.ExpiresOn = now.AddDays(7);
        model.LastAccessedOn = now;
        model.Tags = tags;

        // Assert
        model.BlobType.Should().Be(AzuriteBlobType.Page);
        model.ContentEncoding.Should().Be("gzip");
        model.ContentLanguage.Should().Be("en-US");
        model.ContentLength.Should().Be(1024L);
        model.ContentType.Should().Be("text/plain");
        model.CreatedOn.Should().Be(now);
        model.ExpiresOn.Should().Be(now.AddDays(7));
        model.LastAccessedOn.Should().Be(now);
        model.Tags.Should().BeSameAs(tags);
    }

    #endregion
}
