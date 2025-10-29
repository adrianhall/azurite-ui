using AzuriteUI.Web.Services.Azurite.Models;
using AzuriteUI.Web.Services.CacheDb.Models;

namespace AzuriteUI.Web.UnitTests.Services.CacheDb.Models;

[ExcludeFromCodeCoverage]
public class ContainerModel_Tests
{
    #region Equals Tests

    [Fact]
    public void Equals_WithSameNameAndETag_ShouldReturnTrue()
    {
        // Arrange
        var model1 = new ContainerModel
        {
            Name = "test-container",
            ETag = "etag123"
        };
        var model2 = new ContainerModel
        {
            Name = "test-container",
            ETag = "etag123"
        };

        // Act
        var result = model1.Equals(model2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentName_ShouldReturnFalse()
    {
        // Arrange
        var model1 = new ContainerModel
        {
            Name = "test-container-1",
            ETag = "etag123"
        };
        var model2 = new ContainerModel
        {
            Name = "test-container-2",
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
        var model1 = new ContainerModel
        {
            Name = "test-container",
            ETag = "etag123"
        };
        var model2 = new ContainerModel
        {
            Name = "test-container",
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
        var model = new ContainerModel
        {
            Name = "test-container",
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
        var model = new ContainerModel
        {
            Name = "test-container",
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
        var model1 = new ContainerModel
        {
            Name = "test-container",
            ETag = "etag123",
            DefaultEncryptionScope = "scope1",
            HasImmutabilityPolicy = true,
            PublicAccess = AzuritePublicAccess.Blob,
            TotalSize = 1024,
            BlobCount = 5
        };
        var model2 = new ContainerModel
        {
            Name = "test-container",
            ETag = "etag123",
            DefaultEncryptionScope = "scope2",
            HasImmutabilityPolicy = false,
            PublicAccess = AzuritePublicAccess.Container,
            TotalSize = 2048,
            BlobCount = 10
        };

        // Act
        var result = model1.Equals(model2);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Object.Equals Tests

    [Fact]
    public void ObjectEquals_WithSameNameAndETag_ShouldReturnTrue()
    {
        // Arrange
        var model1 = new ContainerModel
        {
            Name = "test-container",
            ETag = "etag123"
        };
        object model2 = new ContainerModel
        {
            Name = "test-container",
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
        var model = new ContainerModel
        {
            Name = "test-container",
            ETag = "etag123"
        };
        object other = "test-container";

        // Act
        var result = model.Equals(other);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ObjectEquals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var model = new ContainerModel
        {
            Name = "test-container",
            ETag = "etag123"
        };
        object? other = null;

        // Act
        var result = model.Equals(other);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ObjectEquals_WithBlobModel_ShouldReturnFalse()
    {
        // Arrange
        var container = new ContainerModel
        {
            Name = "test-resource",
            ETag = "etag123"
        };
        object blob = new BlobModel
        {
            Name = "test-resource",
            ContainerName = "some-container",
            ETag = "etag123"
        };

        // Act
        var result = container.Equals(blob);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_WithSameNameAndETag_ShouldReturnSameHashCode()
    {
        // Arrange
        var model1 = new ContainerModel
        {
            Name = "test-container",
            ETag = "etag123"
        };
        var model2 = new ContainerModel
        {
            Name = "test-container",
            ETag = "etag123"
        };

        // Act
        var hashCode1 = model1.GetHashCode();
        var hashCode2 = model2.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2);
    }

    [Fact]
    public void GetHashCode_WithDifferentName_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var model1 = new ContainerModel
        {
            Name = "test-container-1",
            ETag = "etag123"
        };
        var model2 = new ContainerModel
        {
            Name = "test-container-2",
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
        var model1 = new ContainerModel
        {
            Name = "test-container",
            ETag = "etag123"
        };
        var model2 = new ContainerModel
        {
            Name = "test-container",
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
        var model = new ContainerModel
        {
            Name = "test-container",
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
        var model = new ContainerModel
        {
            Name = "test"
        };

        // Assert
        model.DefaultEncryptionScope.Should().Be(string.Empty);
        model.HasImmutabilityPolicy.Should().BeFalse();
        model.HasImmutableStorageWithVersioning.Should().BeFalse();
        model.PublicAccess.Should().Be(AzuritePublicAccess.None);
        model.PreventEncryptionScopeOverride.Should().BeFalse();
        model.TotalSize.Should().Be(0L);
        model.BlobCount.Should().Be(0);
        model.Blobs.Should().NotBeNull();
        model.Blobs.Should().BeEmpty();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var model = new ContainerModel
        {
            Name = "test-container"
        };

        // Act
        model.DefaultEncryptionScope = "my-scope";
        model.HasImmutabilityPolicy = true;
        model.HasImmutableStorageWithVersioning = true;
        model.PublicAccess = AzuritePublicAccess.Container;
        model.PreventEncryptionScopeOverride = true;
        model.TotalSize = 1024L;
        model.BlobCount = 5;

        // Assert
        model.DefaultEncryptionScope.Should().Be("my-scope");
        model.HasImmutabilityPolicy.Should().BeTrue();
        model.HasImmutableStorageWithVersioning.Should().BeTrue();
        model.PublicAccess.Should().Be(AzuritePublicAccess.Container);
        model.PreventEncryptionScopeOverride.Should().BeTrue();
        model.TotalSize.Should().Be(1024L);
        model.BlobCount.Should().Be(5);
    }

    #endregion
}
