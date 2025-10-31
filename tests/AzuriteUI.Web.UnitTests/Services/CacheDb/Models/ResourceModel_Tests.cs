using AzuriteUI.Web.Services.CacheDb.Models;

namespace AzuriteUI.Web.UnitTests.Services.CacheDb.Models;

[ExcludeFromCodeCoverage]
public class ResourceModel_Tests
{
    // Test concrete implementation of ResourceModel for testing purposes
    private class TestResourceModel : ResourceModel
    {
    }

    #region Equals Tests

    [Fact(Timeout = 15000)]
    public void Equals_WithSameNameAndETag_ShouldReturnTrue()
    {
        // Arrange
        var model1 = new TestResourceModel
        {
            Name = "test-resource",
            ETag = "etag123"
        };
        var model2 = new TestResourceModel
        {
            Name = "test-resource",
            ETag = "etag123"
        };

        // Act
        var result = model1.Equals(model2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public void Equals_WithDifferentName_ShouldReturnFalse()
    {
        // Arrange
        var model1 = new TestResourceModel
        {
            Name = "test-resource-1",
            ETag = "etag123"
        };
        var model2 = new TestResourceModel
        {
            Name = "test-resource-2",
            ETag = "etag123"
        };

        // Act
        var result = model1.Equals(model2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public void Equals_WithDifferentETag_ShouldReturnFalse()
    {
        // Arrange
        var model1 = new TestResourceModel
        {
            Name = "test-resource",
            ETag = "etag123"
        };
        var model2 = new TestResourceModel
        {
            Name = "test-resource",
            ETag = "etag456"
        };

        // Act
        var result = model1.Equals(model2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var model = new TestResourceModel
        {
            Name = "test-resource",
            ETag = "etag123"
        };

        // Act
        var result = model.Equals(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public void Equals_WithSelf_ShouldReturnTrue()
    {
        // Arrange
        var model = new TestResourceModel
        {
            Name = "test-resource",
            ETag = "etag123"
        };

        // Act
        var result = model.Equals(model);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public void Equals_WithDifferentOtherProperties_ShouldReturnTrue()
    {
        // Arrange
        var model1 = new TestResourceModel
        {
            Name = "test-resource",
            ETag = "etag123",
            HasLegalHold = true,
            LastModified = DateTimeOffset.UtcNow,
            RemainingRetentionDays = 30
        };
        var model2 = new TestResourceModel
        {
            Name = "test-resource",
            ETag = "etag123",
            HasLegalHold = false,
            LastModified = DateTimeOffset.MinValue,
            RemainingRetentionDays = null
        };

        // Act
        var result = model1.Equals(model2);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Object.Equals Tests

    [Fact(Timeout = 15000)]
    public void ObjectEquals_WithSameNameAndETag_ShouldReturnTrue()
    {
        // Arrange
        var model1 = new TestResourceModel
        {
            Name = "test-resource",
            ETag = "etag123"
        };
        object model2 = new TestResourceModel
        {
            Name = "test-resource",
            ETag = "etag123"
        };

        // Act
        var result = model1.Equals(model2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public void ObjectEquals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var model = new TestResourceModel
        {
            Name = "test-resource",
            ETag = "etag123"
        };
        object other = "test-resource";

        // Act
        var result = model.Equals(other);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public void ObjectEquals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var model = new TestResourceModel
        {
            Name = "test-resource",
            ETag = "etag123"
        };
        object? other = null;

        // Act
        var result = model.Equals(other);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetHashCode Tests

    [Fact(Timeout = 15000)]
    public void GetHashCode_WithSameNameAndETag_ShouldReturnSameHashCode()
    {
        // Arrange
        var model1 = new TestResourceModel
        {
            Name = "test-resource",
            ETag = "etag123"
        };
        var model2 = new TestResourceModel
        {
            Name = "test-resource",
            ETag = "etag123"
        };

        // Act
        var hashCode1 = model1.GetHashCode();
        var hashCode2 = model2.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2);
    }

    [Fact(Timeout = 15000)]
    public void GetHashCode_WithDifferentName_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var model1 = new TestResourceModel
        {
            Name = "test-resource-1",
            ETag = "etag123"
        };
        var model2 = new TestResourceModel
        {
            Name = "test-resource-2",
            ETag = "etag123"
        };

        // Act
        var hashCode1 = model1.GetHashCode();
        var hashCode2 = model2.GetHashCode();

        // Assert
        hashCode1.Should().NotBe(hashCode2);
    }

    [Fact(Timeout = 15000)]
    public void GetHashCode_WithDifferentETag_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var model1 = new TestResourceModel
        {
            Name = "test-resource",
            ETag = "etag123"
        };
        var model2 = new TestResourceModel
        {
            Name = "test-resource",
            ETag = "etag456"
        };

        // Act
        var hashCode1 = model1.GetHashCode();
        var hashCode2 = model2.GetHashCode();

        // Assert
        hashCode1.Should().NotBe(hashCode2);
    }

    [Fact(Timeout = 15000)]
    public void GetHashCode_CalledMultipleTimes_ShouldReturnSameValue()
    {
        // Arrange
        var model = new TestResourceModel
        {
            Name = "test-resource",
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

    [Fact(Timeout = 15000)]
    public void Properties_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var model = new TestResourceModel
        {
            Name = "test"
        };

        // Assert
        model.CachedCopyId.Should().Be(string.Empty);
        model.ETag.Should().Be(string.Empty);
        model.HasLegalHold.Should().BeFalse();
        model.LastModified.Should().Be(DateTimeOffset.MinValue);
        model.Metadata.Should().NotBeNull();
        model.Metadata.Should().BeEmpty();
        model.RemainingRetentionDays.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var model = new TestResourceModel
        {
            Name = "test-resource"
        };
        var now = DateTimeOffset.UtcNow;
        var metadata = new Dictionary<string, string> { { "key", "value" } };

        // Act
        model.CachedCopyId = "copy123";
        model.ETag = "etag123";
        model.HasLegalHold = true;
        model.LastModified = now;
        model.Metadata = metadata;
        model.RemainingRetentionDays = 30;

        // Assert
        model.CachedCopyId.Should().Be("copy123");
        model.ETag.Should().Be("etag123");
        model.HasLegalHold.Should().BeTrue();
        model.LastModified.Should().Be(now);
        model.Metadata.Should().BeSameAs(metadata);
        model.RemainingRetentionDays.Should().Be(30);
    }

    #endregion
}
