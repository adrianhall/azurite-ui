using AzuriteUI.Web.Services.CacheDb.Converters;

namespace AzuriteUI.Web.UnitTests.Services.CacheDb.Converters;

[ExcludeFromCodeCoverage]
public class DictionaryJsonConverter_Tests
{
    #region Constructor Tests

    [Fact(Timeout = 15000)]
    public void Constructor_ShouldCreateConverter()
    {
        // Arrange & Act
        var converter = new DictionaryJsonConverter();

        // Assert
        converter.Should().NotBeNull();
    }

    #endregion

    #region Conversion Tests

    [Fact(Timeout = 15000)]
    public void Convert_ShouldConvertEmptyDictionaryToJson()
    {
        // Arrange
        var converter = new DictionaryJsonConverter();
        var dictionary = new Dictionary<string, string>();

        // Act
        var result = (string)converter.ConvertToProvider(dictionary)!;

        // Assert
        result.Should().Be("{}");
    }

    [Fact(Timeout = 15000)]
    public void Convert_ShouldConvertDictionaryWithSingleItemToJson()
    {
        // Arrange
        var converter = new DictionaryJsonConverter();
        var dictionary = new Dictionary<string, string>
        {
            { "key1", "value1" }
        };

        // Act
        var result = (string)converter.ConvertToProvider(dictionary)!;

        // Assert
        result.Should().Contain("key1");
        result.Should().Contain("value1");
    }

    [Fact(Timeout = 15000)]
    public void Convert_ShouldConvertDictionaryWithMultipleItemsToJson()
    {
        // Arrange
        var converter = new DictionaryJsonConverter();
        var dictionary = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" },
            { "key3", "value3" }
        };

        // Act
        var result = (string)converter.ConvertToProvider(dictionary)!;

        // Assert
        result.Should().Contain("key1");
        result.Should().Contain("value1");
        result.Should().Contain("key2");
        result.Should().Contain("value2");
        result.Should().Contain("key3");
        result.Should().Contain("value3");
    }

    [Fact(Timeout = 15000)]
    public void Convert_ShouldHandleSpecialCharactersInValues()
    {
        // Arrange
        var converter = new DictionaryJsonConverter();
        var dictionary = new Dictionary<string, string>
        {
            { "key1", "value with spaces" },
            { "key2", "value\"with\"quotes" },
            { "key3", "value\nwith\nnewlines" }
        };

        // Act
        var result = (string)converter.ConvertToProvider(dictionary)!;

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("key1");
        result.Should().Contain("key2");
        result.Should().Contain("key3");
    }

    [Fact(Timeout = 15000)]
    public void ConvertBack_ShouldConvertEmptyJsonToDictionary()
    {
        // Arrange
        var converter = new DictionaryJsonConverter();
        var json = "{}";

        // Act
        var result = (IDictionary<string, string>)converter.ConvertFromProvider(json)!;

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public void ConvertBack_ShouldConvertJsonWithSingleItemToDictionary()
    {
        // Arrange
        var converter = new DictionaryJsonConverter();
        var json = "{\"key1\":\"value1\"}";

        // Act
        var result = (IDictionary<string, string>)converter.ConvertFromProvider(json)!;

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result["key1"].Should().Be("value1");
    }

    [Fact(Timeout = 15000)]
    public void ConvertBack_ShouldConvertJsonWithMultipleItemsToDictionary()
    {
        // Arrange
        var converter = new DictionaryJsonConverter();
        var json = "{\"key1\":\"value1\",\"key2\":\"value2\",\"key3\":\"value3\"}";

        // Act
        var result = (IDictionary<string, string>)converter.ConvertFromProvider(json)!;

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result["key1"].Should().Be("value1");
        result["key2"].Should().Be("value2");
        result["key3"].Should().Be("value3");
    }

    [Fact(Timeout = 15000)]
    public void ConvertBack_ShouldBeCaseInsensitive()
    {
        // Arrange
        var converter = new DictionaryJsonConverter();
        var json = "{\"Key1\":\"value1\"}";

        // Act
        var result = (IDictionary<string, string>)converter.ConvertFromProvider(json)!;

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("Key1");
    }

    [Fact(Timeout = 15000)]
    public void ConvertBack_WithNullJson_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var converter = new DictionaryJsonConverter();
        string json = "null";

        // Act
        var result = (IDictionary<string, string>)converter.ConvertFromProvider(json)!;

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public void RoundTrip_ShouldPreserveDictionaryContents()
    {
        // Arrange
        var converter = new DictionaryJsonConverter();
        var original = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" },
            { "key3", "value3" }
        };

        // Act
        var json = (string)converter.ConvertToProvider(original)!;
        var roundTripped = (IDictionary<string, string>)converter.ConvertFromProvider(json)!;

        // Assert
        roundTripped.Should().NotBeNull();
        roundTripped.Should().HaveCount(3);
        roundTripped["key1"].Should().Be("value1");
        roundTripped["key2"].Should().Be("value2");
        roundTripped["key3"].Should().Be("value3");
    }

    [Fact(Timeout = 15000)]
    public void RoundTrip_WithEmptyDictionary_ShouldPreserveEmptyState()
    {
        // Arrange
        var converter = new DictionaryJsonConverter();
        var original = new Dictionary<string, string>();

        // Act
        var json = (string)converter.ConvertToProvider(original)!;
        var roundTripped = (IDictionary<string, string>)converter.ConvertFromProvider(json)!;

        // Assert
        roundTripped.Should().NotBeNull();
        roundTripped.Should().BeEmpty();
    }

    #endregion
}
