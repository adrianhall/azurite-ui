using AzuriteUI.Web.Filters;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Testing;

namespace AzuriteUI.Web.UnitTests.Filters;

/// <summary>
/// Unit tests for the DtoHeaderFilter class.
/// </summary>
[ExcludeFromCodeCoverage]
public class DtoHeaderFilter_Tests
{
    /// <summary>
    /// A fake logger to capture log entries for verification.
    /// </summary>
    private readonly FakeLogger<DtoHeaderFilter> _logger = new();

    /// <summary>
    /// A default HTTP context for testing header additions.
    /// </summary>
    private readonly DefaultHttpContext _httpContext = new();

    #region AddETagHeader Tests

    [Fact(Timeout = 15000)]
    public void AddETagHeader_ValidETagWithoutQuotes_AddsQuotedETagHeader()
    {
        // Arrange
        var filter = new DtoHeaderFilter(_logger);
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = "abc123",
            LastModified = DateTimeOffset.UtcNow
        };

        // Act
        filter.AddETagHeader(_httpContext, dto);

        // Assert
        _httpContext.Response.Headers.ETag.ToString().Should().Be("\"abc123\"");
    }

    [Fact(Timeout = 15000)]
    public void AddETagHeader_ValidETagAlreadyQuoted_RemovesAndReQuotesETag()
    {
        // Arrange
        var filter = new DtoHeaderFilter(_logger);
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = "\"abc123\"",
            LastModified = DateTimeOffset.UtcNow
        };

        // Act
        filter.AddETagHeader(_httpContext, dto);

        // Assert
        _httpContext.Response.Headers.ETag.ToString().Should().Be("\"abc123\"");
    }

    [Fact(Timeout = 15000)]
    public void AddETagHeader_ValidETagWithSpecialCharacters_AddsQuotedETagHeader()
    {
        // Arrange
        var filter = new DtoHeaderFilter(_logger);
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = "0x8D9F1234567890",
            LastModified = DateTimeOffset.UtcNow
        };

        // Act
        filter.AddETagHeader(_httpContext, dto);

        // Assert
        _httpContext.Response.Headers.ETag.ToString().Should().Be("\"0x8D9F1234567890\"");
    }

    [Fact(Timeout = 15000)]
    public void AddETagHeader_NullETag_LogsWarningAndDoesNotAddHeader()
    {
        // Arrange
        var filter = new DtoHeaderFilter(_logger);
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = null!,
            LastModified = DateTimeOffset.UtcNow
        };

        // Act
        filter.AddETagHeader(_httpContext, dto);

        // Assert
        _httpContext.Response.Headers.Should().NotContainKey("ETag");
    }

    [Fact(Timeout = 15000)]
    public void AddETagHeader_EmptyETag_LogsWarningAndDoesNotAddHeader()
    {
        // Arrange
        var filter = new DtoHeaderFilter(_logger);
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = string.Empty,
            LastModified = DateTimeOffset.UtcNow
        };

        // Act
        filter.AddETagHeader(_httpContext, dto);

        // Assert
        _httpContext.Response.Headers.Should().NotContainKey("ETag");
    }

    [Fact(Timeout = 15000)]
    public void AddETagHeader_WhitespaceETag_AddsQuotedEmptyETag()
    {
        // Arrange
        var filter = new DtoHeaderFilter(_logger);
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = "   ",
            LastModified = DateTimeOffset.UtcNow
        };

        // Act
        filter.AddETagHeader(_httpContext, dto);

        // Assert
        _httpContext.Response.Headers.ETag.ToString().Should().Be("\"\"");
    }

    #endregion

    #region AddLastModifiedHeader Tests

    [Fact(Timeout = 15000)]
    public void AddLastModifiedHeader_ValidLastModified_AddsLastModifiedHeaderInRFC1123Format()
    {
        // Arrange
        var filter = new DtoHeaderFilter(_logger);
        var lastModified = new DateTimeOffset(2025, 1, 15, 10, 30, 45, TimeSpan.Zero);
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = "abc123",
            LastModified = lastModified
        };

        // Act
        filter.AddLastModifiedHeader(_httpContext, dto);

        // Assert
        _httpContext.Response.Headers.LastModified.ToString().Should().Be("Wed, 15 Jan 2025 10:30:45 GMT");
    }

    [Fact(Timeout = 15000)]
    public void AddLastModifiedHeader_MinValueLastModified_LogsWarningAndDoesNotAddHeader()
    {
        // Arrange
        var filter = new DtoHeaderFilter(_logger);
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = "abc123",
            LastModified = DateTimeOffset.MinValue
        };

        // Act
        filter.AddLastModifiedHeader(_httpContext, dto);

        // Assert
        _httpContext.Response.Headers.Should().NotContainKey("Last-Modified");
    }

    [Fact(Timeout = 15000)]
    public void AddLastModifiedHeader_SpecificDate_FormatsCorrectlyInRFC1123()
    {
        // Arrange
        var filter = new DtoHeaderFilter(_logger);
        var lastModified = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero);
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = "abc123",
            LastModified = lastModified
        };

        // Act
        filter.AddLastModifiedHeader(_httpContext, dto);

        // Assert
        var expectedFormat = lastModified.ToString("R");
        _httpContext.Response.Headers.LastModified.ToString().Should().Be(expectedFormat);
        _httpContext.Response.Headers.LastModified.ToString().Should().Be("Tue, 31 Dec 2024 23:59:59 GMT");
    }

    [Fact(Timeout = 15000)]
    public void AddLastModifiedHeader_DateTimeWithOffset_FormatsCorrectlyInRFC1123()
    {
        // Arrange
        var filter = new DtoHeaderFilter(_logger);
        // Create a date with UTC-5 offset
        var lastModified = new DateTimeOffset(2025, 1, 15, 10, 30, 45, TimeSpan.FromHours(-5));
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = "abc123",
            LastModified = lastModified
        };

        // Act
        filter.AddLastModifiedHeader(_httpContext, dto);

        // Assert
        // RFC 1123 format should convert to UTC
        var expectedFormat = lastModified.ToString("R");
        _httpContext.Response.Headers.LastModified.ToString().Should().Be(expectedFormat);
        _httpContext.Response.Headers.LastModified.ToString().Should().Be("Wed, 15 Jan 2025 15:30:45 GMT");
    }

    #endregion

    #region GetDisplayName Tests

    [Fact(Timeout = 15000)]
    public void GetDisplayName_WithValidEndpoint_ReturnsDisplayName()
    {
        // Arrange
        var filter = new DtoHeaderFilter(_logger);
        var endpoint = new Endpoint(
            requestDelegate: null,
            metadata: new EndpointMetadataCollection([]),
            displayName: "TestController.TestAction (TestNamespace)");

        // Act
        var result = filter.GetDisplayName(endpoint);

        // Assert
        result.Should().Be("TestController.TestAction (TestNamespace)");
    }

    [Fact(Timeout = 15000)]
    public void GetDisplayName_WithNullEndpoint_ReturnsNullPlaceholder()
    {
        // Arrange
        var filter = new DtoHeaderFilter(_logger);

        // Act
        var result = filter.GetDisplayName(null);

        // Assert
        result.Should().Be("<null>");
    }

    [Fact(Timeout = 15000)]
    public void GetDisplayName_WithEndpointWithNullDisplayName_ReturnsNullPlaceholder()
    {
        // Arrange
        var filter = new DtoHeaderFilter(_logger);
        var endpoint = new Endpoint(
            requestDelegate: null,
            metadata: new EndpointMetadataCollection(Array.Empty<object>()),
            displayName: null);

        // Act
        var result = filter.GetDisplayName(endpoint);

        // Assert
        result.Should().Be("<null>");
    }

    [Fact(Timeout = 15000)]
    public void GetDisplayName_WithEndpointWithEmptyDisplayName_ReturnsNullPlaceholder()
    {
        // Arrange
        var filter = new DtoHeaderFilter(_logger);
        var endpoint = new Endpoint(
            requestDelegate: null,
            metadata: new EndpointMetadataCollection(Array.Empty<object>()),
            displayName: string.Empty);

        // Act
        var result = filter.GetDisplayName(endpoint);

        // Assert
        result.Should().Be("<null>");
    }

    [Fact(Timeout = 15000)]
    public void GetDisplayName_WithEndpointWithWhitespaceDisplayName_ReturnsWhitespace()
    {
        // Arrange
        var filter = new DtoHeaderFilter(_logger);
        var endpoint = new Endpoint(
            requestDelegate: null,
            metadata: new EndpointMetadataCollection(Array.Empty<object>()),
            displayName: "   ");

        // Act
        var result = filter.GetDisplayName(endpoint);

        // Assert
        result.Should().Be("   ");
    }

    #endregion
}
