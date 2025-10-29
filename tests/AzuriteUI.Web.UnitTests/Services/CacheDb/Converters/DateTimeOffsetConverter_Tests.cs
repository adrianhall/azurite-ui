using AzuriteUI.Web.Services.CacheDb.Converters;

namespace AzuriteUI.Web.UnitTests.Services.CacheDb.Converters;

[ExcludeFromCodeCoverage]
public class DateTimeOffsetConverter_Tests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldCreateConverter()
    {
        // Arrange & Act
        var converter = new DateTimeOffsetConverter();

        // Assert
        converter.Should().NotBeNull();
    }

    #endregion

    #region Conversion Tests

    [Fact]
    public void Convert_ShouldConvertDateTimeOffsetToIso8601String()
    {
        // Arrange
        var converter = new DateTimeOffsetConverter();
        var dateTimeOffset = new DateTimeOffset(2025, 10, 29, 14, 30, 45, 123, TimeSpan.Zero);

        // Act
        var result = (string)converter.ConvertToProvider(dateTimeOffset)!;

        // Assert
        result.Should().Be("2025-10-29T14:30:45.123Z");
    }

    [Fact]
    public void Convert_WithNonUtcOffset_ShouldConvertToUtcBeforeFormatting()
    {
        // Arrange
        var converter = new DateTimeOffsetConverter();
        var dateTimeOffset = new DateTimeOffset(2025, 10, 29, 14, 30, 45, 123, TimeSpan.FromHours(5));

        // Act
        var result = (string)converter.ConvertToProvider(dateTimeOffset)!;

        // Assert
        result.Should().Be("2025-10-29T09:30:45.123Z");
    }

    [Fact]
    public void Convert_WithMinValue_ShouldConvertSuccessfully()
    {
        // Arrange
        var converter = new DateTimeOffsetConverter();
        var dateTimeOffset = DateTimeOffset.MinValue;

        // Act
        var result = (string)converter.ConvertToProvider(dateTimeOffset)!;

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().EndWith("Z");
    }

    [Fact]
    public void Convert_WithMaxValue_ShouldConvertSuccessfully()
    {
        // Arrange
        var converter = new DateTimeOffsetConverter();
        var dateTimeOffset = DateTimeOffset.MaxValue;

        // Act
        var result = (string)converter.ConvertToProvider(dateTimeOffset)!;

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().EndWith("Z");
    }

    [Fact]
    public void ConvertBack_ShouldConvertIso8601StringToDateTimeOffset()
    {
        // Arrange
        var converter = new DateTimeOffsetConverter();
        var iso8601String = "2025-10-29T14:30:45.123Z";

        // Act
        var result = (DateTimeOffset)converter.ConvertFromProvider(iso8601String)!;

        // Assert
        result.Year.Should().Be(2025);
        result.Month.Should().Be(10);
        result.Day.Should().Be(29);
        result.Hour.Should().Be(14);
        result.Minute.Should().Be(30);
        result.Second.Should().Be(45);
        result.Millisecond.Should().Be(123);
        result.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ConvertBack_WithDifferentFormat_ShouldParseSuccessfully()
    {
        // Arrange
        var converter = new DateTimeOffsetConverter();
        var dateString = "2025-10-29T14:30:45Z";

        // Act
        var result = (DateTimeOffset)converter.ConvertFromProvider(dateString)!;

        // Assert
        result.Year.Should().Be(2025);
        result.Month.Should().Be(10);
        result.Day.Should().Be(29);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveUtcDateTime()
    {
        // Arrange
        var converter = new DateTimeOffsetConverter();
        var original = new DateTimeOffset(2025, 10, 29, 14, 30, 45, 123, TimeSpan.Zero);

        // Act
        var stringValue = (string)converter.ConvertToProvider(original)!;
        var roundTripped = (DateTimeOffset)converter.ConvertFromProvider(stringValue)!;

        // Assert
        roundTripped.Should().Be(original);
    }

    [Fact]
    public void RoundTrip_WithNonUtcOffset_ShouldPreserveInstantInTime()
    {
        // Arrange
        var converter = new DateTimeOffsetConverter();
        var original = new DateTimeOffset(2025, 10, 29, 14, 30, 45, 123, TimeSpan.FromHours(-5));

        // Act
        var stringValue = (string)converter.ConvertToProvider(original)!;
        var roundTripped = (DateTimeOffset)converter.ConvertFromProvider(stringValue)!;

        // Assert
        roundTripped.UtcDateTime.Should().Be(original.UtcDateTime);
    }

    #endregion

    #region Format Constant Tests

    [Fact]
    public void Format_ShouldBeIso8601WithMilliseconds()
    {
        // Arrange & Act & Assert
        DateTimeOffsetConverter.Format.Should().Be("yyyy-MM-dd'T'HH:mm:ss.fffZ");
    }

    #endregion
}
