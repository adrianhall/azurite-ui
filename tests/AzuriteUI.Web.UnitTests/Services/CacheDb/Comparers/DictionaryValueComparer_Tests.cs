using AzuriteUI.Web.Services.CacheDb.Comparers;

namespace AzuriteUI.Web.UnitTests.Services.CacheDb.Comparers;

[ExcludeFromCodeCoverage]
public class DictionaryValueComparer_Tests
{
    #region Constructor Tests

    [Fact(Timeout = 15000)]
    public void Constructor_ShouldCreateComparer()
    {
        // Arrange & Act
        var comparer = new DictionaryValueComparer();

        // Assert
        comparer.Should().NotBeNull();
    }

    #endregion

    #region Equality Comparison Tests

    [Fact(Timeout = 15000)]
    public void Equals_WithBothNull_ShouldReturnTrue()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        IDictionary<string, string>? dict1 = null;
        IDictionary<string, string>? dict2 = null;

        // Act
        var result = comparer.Equals(dict1, dict2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public void Equals_WithFirstNull_ShouldReturnFalse()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        IDictionary<string, string>? dict1 = null;
        var dict2 = new Dictionary<string, string> { { "key1", "value1" } };

        // Act
        var result = comparer.Equals(dict1, dict2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public void Equals_WithSecondNull_ShouldReturnFalse()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        var dict1 = new Dictionary<string, string> { { "key1", "value1" } };
        IDictionary<string, string>? dict2 = null;

        // Act
        var result = comparer.Equals(dict1, dict2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public void Equals_WithDifferentCounts_ShouldReturnFalse()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        var dict1 = new Dictionary<string, string>
        {
            { "key1", "value1" }
        };
        var dict2 = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        // Act
        var result = comparer.Equals(dict1, dict2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public void Equals_WithEmptyDictionaries_ShouldReturnTrue()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        var dict1 = new Dictionary<string, string>();
        var dict2 = new Dictionary<string, string>();

        // Act
        var result = comparer.Equals(dict1, dict2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public void Equals_WithSameContent_ShouldReturnTrue()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        var dict1 = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" },
            { "key3", "value3" }
        };
        var dict2 = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" },
            { "key3", "value3" }
        };

        // Act
        var result = comparer.Equals(dict1, dict2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public void Equals_WithSameContentDifferentOrder_ShouldReturnTrue()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        var dict1 = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" },
            { "key3", "value3" }
        };
        var dict2 = new Dictionary<string, string>
        {
            { "key3", "value3" },
            { "key1", "value1" },
            { "key2", "value2" }
        };

        // Act
        var result = comparer.Equals(dict1, dict2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public void Equals_WithDifferentKeys_ShouldReturnFalse()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        var dict1 = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };
        var dict2 = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key3", "value2" }
        };

        // Act
        var result = comparer.Equals(dict1, dict2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public void Equals_WithSameKeysDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        var dict1 = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };
        var dict2 = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "different" }
        };

        // Act
        var result = comparer.Equals(dict1, dict2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public void Equals_WithSingleItem_ShouldReturnTrue()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        var dict1 = new Dictionary<string, string>
        {
            { "key1", "value1" }
        };
        var dict2 = new Dictionary<string, string>
        {
            { "key1", "value1" }
        };

        // Act
        var result = comparer.Equals(dict1, dict2);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Hash Code Tests

    [Fact(Timeout = 15000)]
    public void GetHashCode_WithNull_ShouldReturnZero()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        IDictionary<string, string>? dictionary = null;

        // Act
        var result = comparer.GetHashCode(dictionary!);

        // Assert
        result.Should().Be(0);
    }

    [Fact(Timeout = 15000)]
    public void GetHashCode_WithEmptyDictionary_ShouldReturnConsistentValue()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        var dict1 = new Dictionary<string, string>();
        var dict2 = new Dictionary<string, string>();

        // Act
        var hash1 = comparer.GetHashCode(dict1);
        var hash2 = comparer.GetHashCode(dict2);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact(Timeout = 15000)]
    public void GetHashCode_WithSameContent_ShouldReturnSameHashCode()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        var dict1 = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" },
            { "key3", "value3" }
        };
        var dict2 = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" },
            { "key3", "value3" }
        };

        // Act
        var hash1 = comparer.GetHashCode(dict1);
        var hash2 = comparer.GetHashCode(dict2);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact(Timeout = 15000)]
    public void GetHashCode_WithSameContentDifferentOrder_ShouldReturnSameHashCode()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        var dict1 = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" },
            { "key3", "value3" }
        };
        var dict2 = new Dictionary<string, string>
        {
            { "key3", "value3" },
            { "key1", "value1" },
            { "key2", "value2" }
        };

        // Act
        var hash1 = comparer.GetHashCode(dict1);
        var hash2 = comparer.GetHashCode(dict2);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact(Timeout = 15000)]
    public void GetHashCode_WithDifferentContent_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        var dict1 = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };
        var dict2 = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "different" }
        };

        // Act
        var hash1 = comparer.GetHashCode(dict1);
        var hash2 = comparer.GetHashCode(dict2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact(Timeout = 15000)]
    public void GetHashCode_WithSingleItem_ShouldReturnConsistentValue()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        var dict1 = new Dictionary<string, string>
        {
            { "key1", "value1" }
        };
        var dict2 = new Dictionary<string, string>
        {
            { "key1", "value1" }
        };

        // Act
        var hash1 = comparer.GetHashCode(dict1);
        var hash2 = comparer.GetHashCode(dict2);

        // Assert
        hash1.Should().Be(hash2);
    }

    #endregion

    #region Snapshot Tests

    [Fact(Timeout = 15000)]
    public void Snapshot_WithNull_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        IDictionary<string, string>? dictionary = null;

        // Act
        var result = comparer.Snapshot(dictionary!);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public void Snapshot_WithEmptyDictionary_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        var dictionary = new Dictionary<string, string>();

        // Act
        var result = comparer.Snapshot(dictionary);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public void Snapshot_WithSingleItem_ShouldReturnCopy()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        var dictionary = new Dictionary<string, string>
        {
            { "key1", "value1" }
        };

        // Act
        var result = comparer.Snapshot(dictionary);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result["key1"].Should().Be("value1");
    }

    [Fact(Timeout = 15000)]
    public void Snapshot_WithMultipleItems_ShouldReturnCopy()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        var dictionary = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" },
            { "key3", "value3" }
        };

        // Act
        var result = comparer.Snapshot(dictionary);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result["key1"].Should().Be("value1");
        result["key2"].Should().Be("value2");
        result["key3"].Should().Be("value3");
    }

    [Fact(Timeout = 15000)]
    public void Snapshot_ShouldCreateIndependentCopy()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        var original = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        // Act
        var snapshot = comparer.Snapshot(original);
        original["key1"] = "modified";
        original["key3"] = "new";

        // Assert
        snapshot.Should().NotBeNull();
        snapshot.Should().HaveCount(2);
        snapshot["key1"].Should().Be("value1");
        snapshot["key2"].Should().Be("value2");
        snapshot.Should().NotContainKey("key3");
    }

    [Fact(Timeout = 15000)]
    public void Snapshot_ShouldNotBeReferenceEqual()
    {
        // Arrange
        var comparer = new DictionaryValueComparer();
        var original = new Dictionary<string, string>
        {
            { "key1", "value1" }
        };

        // Act
        var snapshot = comparer.Snapshot(original);

        // Assert
        snapshot.Should().NotBeSameAs(original);
    }

    #endregion
}
