using AzuriteUI.Web.Filters;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;

namespace AzuriteUI.Web.UnitTests.Filters;

/// <summary>
/// Unit tests for the DtoHeaderFilter class.
/// </summary>
[ExcludeFromCodeCoverage]
public class DtoHeaderFilter_Tests
{
    #region Helpers
    private static LinkGenerator MockLinkGenerator(string endpointName, string expectedUrl)
    {
        // this method mocks the LinkGenerator.GetUriByName() method when it uses endpoint name.
        var linkGenerator = Substitute.For<LinkGenerator>();
        linkGenerator.GetUriByAddress<string>(
            Arg.Any<HttpContext>(),             // httpContext
            Arg.Is<string>(endpointName),       // address
            Arg.Any<RouteValueDictionary>(),    // values
            Arg.Any<RouteValueDictionary?>(),   // ambientValues
            Arg.Any<string?>(),                 // scheme
            Arg.Any<HostString?>(),             // host
            Arg.Any<PathString?>(),             // pathBase
            Arg.Any<FragmentString>(),         // fragment
            Arg.Any<LinkOptions?>()
        ).Returns(expectedUrl);
        return linkGenerator;
    }

    #endregion

    #region AddETagHeader Tests

    [Fact(Timeout = 15000)]
    public void AddETagHeader_ValidETagWithoutQuotes_AddsQuotedETagHeader()
    {
        // Arrange
        var logger = new FakeLogger<DtoHeaderFilter>();
        var linkGenerator = Substitute.For<LinkGenerator>();
        var filter = new DtoHeaderFilter(linkGenerator, logger);
        var httpContext = new DefaultHttpContext();
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = "abc123",
            LastModified = DateTimeOffset.UtcNow
        };

        // Act
        filter.AddETagHeader(httpContext, dto);

        // Assert
        httpContext.Response.Headers.ETag.ToString().Should().Be("\"abc123\"");
    }

    [Fact(Timeout = 15000)]
    public void AddETagHeader_ValidETagAlreadyQuoted_RemovesAndReQuotesETag()
    {
        // Arrange
        var logger = new FakeLogger<DtoHeaderFilter>();
        var linkGenerator = Substitute.For<LinkGenerator>();
        var filter = new DtoHeaderFilter(linkGenerator, logger);
        var httpContext = new DefaultHttpContext();
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = "\"abc123\"",
            LastModified = DateTimeOffset.UtcNow
        };

        // Act
        filter.AddETagHeader(httpContext, dto);

        // Assert
        httpContext.Response.Headers.ETag.ToString().Should().Be("\"abc123\"");
    }

    [Fact(Timeout = 15000)]
    public void AddETagHeader_ValidETagWithSpecialCharacters_AddsQuotedETagHeader()
    {
        // Arrange
        var logger = new FakeLogger<DtoHeaderFilter>();
        var linkGenerator = Substitute.For<LinkGenerator>();
        var filter = new DtoHeaderFilter(linkGenerator, logger);
        var httpContext = new DefaultHttpContext();
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = "0x8D9F1234567890",
            LastModified = DateTimeOffset.UtcNow
        };

        // Act
        filter.AddETagHeader(httpContext, dto);

        // Assert
        httpContext.Response.Headers.ETag.ToString().Should().Be("\"0x8D9F1234567890\"");
    }

    [Fact(Timeout = 15000)]
    public void AddETagHeader_NullETag_LogsWarningAndDoesNotAddHeader()
    {
        // Arrange
        var logger = new FakeLogger<DtoHeaderFilter>();
        var linkGenerator = Substitute.For<LinkGenerator>();
        var filter = new DtoHeaderFilter(linkGenerator, logger);
        var httpContext = new DefaultHttpContext();
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = null!,
            LastModified = DateTimeOffset.UtcNow
        };

        // Act
        filter.AddETagHeader(httpContext, dto);

        // Assert
        httpContext.Response.Headers.ContainsKey("ETag").Should().BeFalse();
        var logEntry = logger.LatestRecord;
        logEntry.Level.Should().Be(LogLevel.Warning);
        logEntry.Message.Should().Contain("DTO ETag is null or empty");
    }

    [Fact(Timeout = 15000)]
    public void AddETagHeader_EmptyETag_LogsWarningAndDoesNotAddHeader()
    {
        // Arrange
        var logger = new FakeLogger<DtoHeaderFilter>();
        var linkGenerator = Substitute.For<LinkGenerator>();
        var filter = new DtoHeaderFilter(linkGenerator, logger);
        var httpContext = new DefaultHttpContext();
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = string.Empty,
            LastModified = DateTimeOffset.UtcNow
        };

        // Act
        filter.AddETagHeader(httpContext, dto);

        // Assert
        httpContext.Response.Headers.ContainsKey("ETag").Should().BeFalse();
        var logEntry = logger.LatestRecord;
        logEntry.Level.Should().Be(LogLevel.Warning);
        logEntry.Message.Should().Contain("DTO ETag is null or empty");
    }

    [Fact(Timeout = 15000)]
    public void AddETagHeader_WhitespaceETag_AddsQuotedEmptyETag()
    {
        // Arrange
        var logger = new FakeLogger<DtoHeaderFilter>();
        var linkGenerator = Substitute.For<LinkGenerator>();
        var filter = new DtoHeaderFilter(linkGenerator, logger);
        var httpContext = new DefaultHttpContext();
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = "   ",
            LastModified = DateTimeOffset.UtcNow
        };

        // Act
        filter.AddETagHeader(httpContext, dto);

        // Assert
        httpContext.Response.Headers.ETag.ToString().Should().Be("\"\"");
    }

    #endregion

    #region AddLastModifiedHeader Tests

    [Fact(Timeout = 15000)]
    public void AddLastModifiedHeader_ValidLastModified_AddsLastModifiedHeaderInRFC1123Format()
    {
        // Arrange
        var logger = new FakeLogger<DtoHeaderFilter>();
        var linkGenerator = Substitute.For<LinkGenerator>();
        var filter = new DtoHeaderFilter(linkGenerator, logger);
        var httpContext = new DefaultHttpContext();
        var lastModified = new DateTimeOffset(2025, 1, 15, 10, 30, 45, TimeSpan.Zero);
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = "abc123",
            LastModified = lastModified
        };

        // Act
        filter.AddLastModifiedHeader(httpContext, dto);

        // Assert
        httpContext.Response.Headers.LastModified.ToString().Should().Be("Wed, 15 Jan 2025 10:30:45 GMT");
    }

    [Fact(Timeout = 15000)]
    public void AddLastModifiedHeader_MinValueLastModified_LogsWarningAndDoesNotAddHeader()
    {
        // Arrange
        var logger = new FakeLogger<DtoHeaderFilter>();
        var linkGenerator = Substitute.For<LinkGenerator>();
        var filter = new DtoHeaderFilter(linkGenerator, logger);
        var httpContext = new DefaultHttpContext();
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = "abc123",
            LastModified = DateTimeOffset.MinValue
        };

        // Act
        filter.AddLastModifiedHeader(httpContext, dto);

        // Assert
        httpContext.Response.Headers.ContainsKey("Last-Modified").Should().BeFalse();
        var logEntry = logger.LatestRecord;
        logEntry.Level.Should().Be(LogLevel.Warning);
        logEntry.Message.Should().Contain("DTO LastModified is MinValue");
    }

    [Fact(Timeout = 15000)]
    public void AddLastModifiedHeader_SpecificDate_FormatsCorrectlyInRFC1123()
    {
        // Arrange
        var logger = new FakeLogger<DtoHeaderFilter>();
        var linkGenerator = Substitute.For<LinkGenerator>();
        var filter = new DtoHeaderFilter(linkGenerator, logger);
        var httpContext = new DefaultHttpContext();
        var lastModified = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero);
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = "abc123",
            LastModified = lastModified
        };

        // Act
        filter.AddLastModifiedHeader(httpContext, dto);

        // Assert
        var expectedFormat = lastModified.ToString("R");
        httpContext.Response.Headers.LastModified.ToString().Should().Be(expectedFormat);
        httpContext.Response.Headers.LastModified.ToString().Should().Be("Tue, 31 Dec 2024 23:59:59 GMT");
    }

    [Fact(Timeout = 15000)]
    public void AddLastModifiedHeader_DateTimeWithOffset_FormatsCorrectlyInRFC1123()
    {
        // Arrange
        var logger = new FakeLogger<DtoHeaderFilter>();
        var linkGenerator = Substitute.For<LinkGenerator>();
        var filter = new DtoHeaderFilter(linkGenerator, logger);
        var httpContext = new DefaultHttpContext();
        // Create a date with UTC-5 offset
        var lastModified = new DateTimeOffset(2025, 1, 15, 10, 30, 45, TimeSpan.FromHours(-5));
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = "abc123",
            LastModified = lastModified
        };

        // Act
        filter.AddLastModifiedHeader(httpContext, dto);

        // Assert
        // RFC 1123 format should convert to UTC
        var expectedFormat = lastModified.ToString("R");
        httpContext.Response.Headers.LastModified.ToString().Should().Be(expectedFormat);
        httpContext.Response.Headers.LastModified.ToString().Should().Be("Wed, 15 Jan 2025 15:30:45 GMT");
    }

    #endregion

    #region GetEndpointName Tests

    [Fact(Timeout = 15000)]
    public void GetEndpointName_ContainerDTO_ReturnsGetContainerByName()
    {
        // Arrange
        var dto = new ContainerDTO { Name = "test-container", ETag = "etag", LastModified = DateTimeOffset.UtcNow };

        // Act
        var result = DtoHeaderFilter.GetEndpointName(dto);

        // Assert
        result.Should().NotBeNull().And.Be("GetContainerByName");
    }

    [Fact(Timeout = 15000)]
    public void GetEndpointName_BlobDTO_ReturnsGetBlobByName()
    {
        // Arrange
        var dto = new BlobDTO { Name = "test-blob", ContainerName = "test-container", ETag = "etag", LastModified = DateTimeOffset.UtcNow };

        // Act
        var result = DtoHeaderFilter.GetEndpointName(dto);

        // Assert
        result.Should().NotBeNull().And.Be("GetBlobByName");
    }

    [Fact(Timeout = 15000)]
    public void GetEndpointName_UnknownDTOType_ReturnsNull()
    {
        // Arrange
        var dto = Substitute.For<IBaseDTO>();
        dto.Name.Returns("unknown-dto");
        dto.ETag.Returns("etag");
        dto.LastModified.Returns(DateTimeOffset.UtcNow);

        // Act
        var result = DtoHeaderFilter.GetEndpointName(dto);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region AddLinkHeader Tests

    [Fact(Timeout = 15000)]
    public void AddLinkHeader_ContainerDTO_AddsLinkHeaderWithSelfRelation()
    {
        // Arrange
        var logger = new FakeLogger<DtoHeaderFilter>();
        var expectedUrl = "https://localhost:5001/api/containers/test-container";
        var linkGenerator = MockLinkGenerator("GetContainerByName", expectedUrl);
        var filter = new DtoHeaderFilter(linkGenerator, logger);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost:5001");
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = "abc123",
            LastModified = DateTimeOffset.UtcNow
        };

        // Act
        filter.AddLinkHeader(httpContext, dto);

        // Assert
        httpContext.Response.Headers.Link.ToString().Should().Be($"<{expectedUrl}>; rel=\"self\"");
    }

    [Fact(Timeout = 15000)]
    public void AddLinkHeader_BlobDTO_AddsLinkHeaderWithSelfRelation()
    {
        // Arrange
        var logger = new FakeLogger<DtoHeaderFilter>();
        var expectedUrl = "https://localhost:5001/api/containers/test-container/blobs/test-blob";
        var linkGenerator = MockLinkGenerator("GetBlobByName", expectedUrl);
        var filter = new DtoHeaderFilter(linkGenerator, logger);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost:5001");
        var dto = new BlobDTO
        {
            Name = "test-blob",
            ContainerName = "test-container",
            ETag = "abc123",
            LastModified = DateTimeOffset.UtcNow
        };

        // Act
        filter.AddLinkHeader(httpContext, dto);

        // Assert
        httpContext.Response.Headers.Link.ToString().Should().Be($"<{expectedUrl}>; rel=\"self\"");
    }

    [Fact(Timeout = 15000)]
    public void AddLinkHeader_UnknownDTOType_LogsWarningAndDoesNotAddHeader()
    {
        // Arrange
        var logger = new FakeLogger<DtoHeaderFilter>();
        var linkGenerator = Substitute.For<LinkGenerator>();
        var filter = new DtoHeaderFilter(linkGenerator, logger);
        var httpContext = new DefaultHttpContext();
        var dto = Substitute.For<IBaseDTO>();
        dto.Name.Returns("unknown-dto");
        dto.ETag.Returns("etag");
        dto.LastModified.Returns(DateTimeOffset.UtcNow);

        // Act
        filter.AddLinkHeader(httpContext, dto);

        // Assert
        httpContext.Response.Headers.Should().NotContainKey("Link");
        var logEntry = logger.LatestRecord;
        logEntry.Level.Should().Be(LogLevel.Warning);
        logEntry.Message.Should().Contain("Could not determine endpoint name for DTO Link header");
    }

    [Fact(Timeout = 15000)]
    public void AddLinkHeader_LinkGeneratorReturnsNull_LogsWarningAndDoesNotAddHeader()
    {
        // Arrange
        var logger = new FakeLogger<DtoHeaderFilter>();
        var linkGenerator = MockLinkGenerator("GetContainerByName", null!);
        var filter = new DtoHeaderFilter(linkGenerator, logger);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost:5001");
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = "abc123",
            LastModified = DateTimeOffset.UtcNow
        };

        // Act
        filter.AddLinkHeader(httpContext, dto);

        // Assert
        httpContext.Response.Headers.Should().NotContainKey("Link");
        var logEntry = logger.LatestRecord;
        logEntry.Level.Should().Be(LogLevel.Warning);
        logEntry.Message.Should().Contain("Could not generate self link URL for DTO Link header");
    }

    [Fact(Timeout = 15000)]
    public void AddLinkHeader_LinkGeneratorReturnsEmptyString_LogsWarningAndDoesNotAddHeader()
    {
        // Arrange
        var logger = new FakeLogger<DtoHeaderFilter>();
        var linkGenerator = MockLinkGenerator("GetContainerByName", string.Empty);
        var filter = new DtoHeaderFilter(linkGenerator, logger);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost:5001");
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = "abc123",
            LastModified = DateTimeOffset.UtcNow
        };

        // Act
        filter.AddLinkHeader(httpContext, dto);

        // Assert
        httpContext.Response.Headers.Should().NotContainKey("Link");
        var logEntry = logger.LatestRecord;
        logEntry.Level.Should().Be(LogLevel.Warning);
        logEntry.Message.Should().Contain("Could not generate self link URL for DTO Link header");
    }

    #endregion
}
