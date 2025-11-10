using AzuriteUI.Web.Services.Display;

namespace AzuriteUI.Web.UnitTests.Services.Display;

[ExcludeFromCodeCoverage]
public class DisplayHelper_Tests
{
    private readonly DisplayHelper _sut = new();

    #region ConvertToBootstrapIcon

    [Theory(Timeout = 15000)]
    [InlineData("image/png", "file-image")]
    [InlineData("image/jpeg", "file-image")]
    [InlineData("image/gif", "file-image")]
    [InlineData("image/svg+xml", "file-image")]
    [InlineData("IMAGE/PNG", "file-image")] // Test case insensitivity
    public void ConvertToBootstrapIcon_WithImageContentType_ReturnsFileImage(string contentType, string expected)
    {
        // Act
        var result = _sut.ConvertToBootstrapIcon(contentType);

        // Assert
        result.Should().Be(expected);
    }

    [Theory(Timeout = 15000)]
    [InlineData("video/mp4", "file-play")]
    [InlineData("video/mpeg", "file-play")]
    [InlineData("video/quicktime", "file-play")]
    [InlineData("video/x-msvideo", "file-play")]
    [InlineData("VIDEO/MP4", "file-play")] // Test case insensitivity
    public void ConvertToBootstrapIcon_WithVideoContentType_ReturnsFilePlay(string contentType, string expected)
    {
        // Act
        var result = _sut.ConvertToBootstrapIcon(contentType);

        // Assert
        result.Should().Be(expected);
    }

    [Theory(Timeout = 15000)]
    [InlineData("audio/mpeg", "file-music")]
    [InlineData("audio/wav", "file-music")]
    [InlineData("audio/ogg", "file-music")]
    [InlineData("audio/mp3", "file-music")]
    [InlineData("AUDIO/MPEG", "file-music")] // Test case insensitivity
    public void ConvertToBootstrapIcon_WithAudioContentType_ReturnsFileMusic(string contentType, string expected)
    {
        // Act
        var result = _sut.ConvertToBootstrapIcon(contentType);

        // Assert
        result.Should().Be(expected);
    }

    [Theory(Timeout = 15000)]
    [InlineData("text/plain", "file-text")]
    [InlineData("text/html", "file-text")]
    [InlineData("text/css", "file-text")]
    [InlineData("text/csv", "file-text")]
    [InlineData("TEXT/PLAIN", "file-text")] // Test case insensitivity
    public void ConvertToBootstrapIcon_WithTextContentType_ReturnsFileText(string contentType, string expected)
    {
        // Act
        var result = _sut.ConvertToBootstrapIcon(contentType);

        // Assert
        result.Should().Be(expected);
    }

    [Theory(Timeout = 15000)]
    [InlineData("application/pdf", "file-pdf")]
    [InlineData("application/x-pdf", "file-pdf")]
    [InlineData("APPLICATION/PDF", "file-pdf")] // Test case insensitivity
    public void ConvertToBootstrapIcon_WithPdfContentType_ReturnsFilePdf(string contentType, string expected)
    {
        // Act
        var result = _sut.ConvertToBootstrapIcon(contentType);

        // Assert
        result.Should().Be(expected);
    }

    [Theory(Timeout = 15000)]
    [InlineData("application/zip", "file-zip")]
    [InlineData("application/x-zip-compressed", "file-zip")]
    [InlineData("application/x-compressed", "file-zip")]
    [InlineData("application/gzip", "file-zip")]
    [InlineData("APPLICATION/ZIP", "file-zip")] // Test case insensitivity
    public void ConvertToBootstrapIcon_WithZipContentType_ReturnsFileZip(string contentType, string expected)
    {
        // Act
        var result = _sut.ConvertToBootstrapIcon(contentType);

        // Assert
        result.Should().Be(expected);
    }

    [Theory(Timeout = 15000)]
    [InlineData("application/json", "file-code")]
    [InlineData("application/xml", "file-code")]
    [InlineData("text/xml", "file-text")]
    [InlineData("APPLICATION/JSON", "file-code")] // Test case insensitivity
    public void ConvertToBootstrapIcon_WithJsonOrXmlContentType_ReturnsFileCode(string contentType, string expected)
    {
        // Act
        var result = _sut.ConvertToBootstrapIcon(contentType);

        // Assert
        result.Should().Be(expected);
    }

    [Theory(Timeout = 15000)]
    [InlineData("application/octet-stream", "file-earmark")]
    [InlineData("application/unknown", "file-earmark")]
    [InlineData("unknown/type", "file-earmark")]
    [InlineData("", "file-earmark")]
    public void ConvertToBootstrapIcon_WithUnknownContentType_ReturnsFileEarmark(string contentType, string expected)
    {
        // Act
        var result = _sut.ConvertToBootstrapIcon(contentType);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region ConvertToFileCount

    [Theory(Timeout = 15000)]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    [InlineData(100, "100")]
    [InlineData(1000, "1,000")]
    [InlineData(1000000, "1,000,000")]
    public void ConvertToFileCount_WithVariousValues_ReturnsFormattedString(int fileCount, string expected)
    {
        // Act
        var result = _sut.ConvertToFileCount(fileCount);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region ConvertToFileSize

    [Theory(Timeout = 15000)]
    [InlineData(0L, "0 b")]
    [InlineData(1L, "1 B")]
    [InlineData(1024L, "1 KB")]
    [InlineData(1048576L, "1 MB")]
    [InlineData(1073741824L, "1 GB")]
    public void ConvertToFileSize_WithVariousValues_ReturnsHumanizedString(long byteCount, string expected)
    {
        // Act
        var result = _sut.ConvertToFileSize(byteCount);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region ConvertToRelativeTime

    [Theory(Timeout = 15000)]
    [InlineData(0, "now")]
    [InlineData(-1, "a minute ago")]
    [InlineData(-60, "an hour ago")]
    [InlineData(-1440, "yesterday")]
    [InlineData(1441, "tomorrow")]
    public void ConvertToRelativeTime_WithVariousOffsets_ReturnsHumanizedString(int minutesOffset, string expected)
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.AddMinutes(minutesOffset);

        // Act
        var result = _sut.ConvertToRelativeTime(timestamp);

        // Assert
        result.Should().Be(expected);
    }

    #endregion
}
