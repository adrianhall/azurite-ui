using AzuriteUI.Web.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AzuriteUI.Web.IntegrationTests.API;

/// <summary>
/// Integration tests for the AzuriteExceptionFilter using the TestExceptionController
/// to simulate various exception scenarios.
/// </summary>
[ExcludeFromCodeCoverage]
public class TestExceptionController_Tests(ServiceFixture fixture) : BaseApiTest(fixture)
{
    #region 404 Not Found Tests

    [Fact(Timeout = 60000)]
    public async Task TestException_404_Returns404WithProblemDetails()
    {
        // Arrange
        var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/exceptions/not-found");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>(ServiceFixture.JsonOptions);
        problemDetails.Should().NotBeNull();

        var root = problemDetails!.RootElement;
        root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status404NotFound);
        root.GetProperty("title").GetString().Should().Be("Not Found");
        root.GetProperty("detail").GetString().Should().Contain("requested resource");
        root.GetProperty("detail").GetString().Should().Contain("not found");
        root.GetProperty("instance").GetString().Should().Be("/api/test/exceptions/not-found");
    }

    [Fact(Timeout = 60000)]
    public async Task TestException_404_IncludesResourceNameInExtensions()
    {
        // Arrange
        var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/exceptions/not-found");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>(ServiceFixture.JsonOptions);
        var root = problemDetails!.RootElement;

        root.TryGetProperty("resourceName", out var resourceName).Should().BeTrue();
        resourceName.GetString().Should().Be("test-resource");
    }

    [Fact(Timeout = 60000)]
    public async Task TestException_404_ReturnsValidProblemDetailsStructure()
    {
        // Arrange
        var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/exceptions/not-found");

        // Assert
        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>(ServiceFixture.JsonOptions);
        var root = problemDetails!.RootElement;

        // Verify all required ProblemDetails fields are present
        root.TryGetProperty("status", out _).Should().BeTrue();
        root.TryGetProperty("title", out _).Should().BeTrue();
        root.TryGetProperty("detail", out _).Should().BeTrue();
        root.TryGetProperty("instance", out _).Should().BeTrue();
    }

    #endregion

    #region 409 Conflict Tests

    [Fact(Timeout = 60000)]
    public async Task TestException_409_Returns409WithProblemDetails()
    {
        // Arrange
        var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/exceptions/conflict");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>(ServiceFixture.JsonOptions);
        problemDetails.Should().NotBeNull();

        var root = problemDetails!.RootElement;
        root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status409Conflict);
        root.GetProperty("title").GetString().Should().Be("Conflict");
        root.GetProperty("detail").GetString().Should().Contain("resource");
        root.GetProperty("detail").GetString().Should().Contain("already exists");
        root.GetProperty("instance").GetString().Should().Be("/api/test/exceptions/conflict");
    }

    [Fact(Timeout = 60000)]
    public async Task TestException_409_IncludesResourceNameInExtensions()
    {
        // Arrange
        var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/exceptions/conflict");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>(ServiceFixture.JsonOptions);
        var root = problemDetails!.RootElement;

        root.TryGetProperty("resourceName", out var resourceName).Should().BeTrue();
        resourceName.GetString().Should().Be("test-resource");
    }

    [Fact(Timeout = 60000)]
    public async Task TestException_409_ReturnsValidProblemDetailsStructure()
    {
        // Arrange
        var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/exceptions/conflict");

        // Assert
        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>(ServiceFixture.JsonOptions);
        var root = problemDetails!.RootElement;

        // Verify all required ProblemDetails fields are present
        root.TryGetProperty("status", out _).Should().BeTrue();
        root.TryGetProperty("title", out _).Should().BeTrue();
        root.TryGetProperty("detail", out _).Should().BeTrue();
        root.TryGetProperty("instance", out _).Should().BeTrue();
    }

    #endregion

    #region 416 Range Not Satisfiable Tests

    [Fact(Timeout = 60000)]
    public async Task TestException_416_Returns416WithProblemDetails()
    {
        // Arrange
        var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/exceptions/range-not-satisfiable");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.RequestedRangeNotSatisfiable);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>(ServiceFixture.JsonOptions);
        problemDetails.Should().NotBeNull();

        var root = problemDetails!.RootElement;
        root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status416RangeNotSatisfiable);
        root.GetProperty("title").GetString().Should().Be("Range Not Satisfiable");
        root.GetProperty("detail").GetString().Should().Contain("not satisfiable");
        root.GetProperty("instance").GetString().Should().Be("/api/test/exceptions/range-not-satisfiable");
    }

    [Fact(Timeout = 60000)]
    public async Task TestException_416_DoesNotIncludeResourceName()
    {
        // Arrange
        var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/exceptions/range-not-satisfiable");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.RequestedRangeNotSatisfiable);

        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>(ServiceFixture.JsonOptions);
        var root = problemDetails!.RootElement;

        // RangeNotSatisfiableException doesn't have ResourceName property
        root.TryGetProperty("resourceName", out _).Should().BeFalse();
    }

    [Fact(Timeout = 60000)]
    public async Task TestException_416_ReturnsValidProblemDetailsStructure()
    {
        // Arrange
        var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/exceptions/range-not-satisfiable");

        // Assert
        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>(ServiceFixture.JsonOptions);
        var root = problemDetails!.RootElement;

        // Verify all required ProblemDetails fields are present
        root.TryGetProperty("status", out _).Should().BeTrue();
        root.TryGetProperty("title", out _).Should().BeTrue();
        root.TryGetProperty("detail", out _).Should().BeTrue();
        root.TryGetProperty("instance", out _).Should().BeTrue();
    }

    #endregion

    #region 503 Service Unavailable Tests

    [Fact(Timeout = 60000)]
    public async Task TestException_503_Returns503WithProblemDetails()
    {
        // Arrange
        var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/exceptions/service-unavailable");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>(ServiceFixture.JsonOptions);
        problemDetails.Should().NotBeNull();

        var root = problemDetails!.RootElement;
        root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status503ServiceUnavailable);
        root.GetProperty("title").GetString().Should().Be("Service Unavailable");
        root.GetProperty("detail").GetString().Should().ContainAny("unavailable", "service");
        root.GetProperty("instance").GetString().Should().Be("/api/test/exceptions/service-unavailable");
    }

    [Fact(Timeout = 60000)]
    public async Task TestException_503_ReturnsValidProblemDetailsStructure()
    {
        // Arrange
        var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/exceptions/service-unavailable");

        // Assert
        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>(ServiceFixture.JsonOptions);
        var root = problemDetails!.RootElement;

        // Verify all required ProblemDetails fields are present
        root.TryGetProperty("status", out _).Should().BeTrue();
        root.TryGetProperty("title", out _).Should().BeTrue();
        root.TryGetProperty("detail", out _).Should().BeTrue();
        root.TryGetProperty("instance", out _).Should().BeTrue();
    }

    #endregion

    #region 502 Bad Gateway Tests

    [Fact(Timeout = 60000)]
    public async Task TestException_502_Returns502WithProblemDetails()
    {
        // Arrange
        var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/exceptions/bad-gateway");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>(ServiceFixture.JsonOptions);
        problemDetails.Should().NotBeNull();

        var root = problemDetails!.RootElement;
        root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status502BadGateway);
        root.GetProperty("title").GetString().Should().Be("Bad Gateway");
        root.GetProperty("detail").GetString().Should().ContainAny("bad gateway", "Bad gateway");
        root.GetProperty("instance").GetString().Should().Be("/api/test/exceptions/bad-gateway");
    }

    [Fact(Timeout = 60000)]
    public async Task TestException_502_ReturnsValidProblemDetailsStructure()
    {
        // Arrange
        var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/exceptions/bad-gateway");

        // Assert
        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>(ServiceFixture.JsonOptions);
        var root = problemDetails!.RootElement;

        // Verify all required ProblemDetails fields are present
        root.TryGetProperty("status", out _).Should().BeTrue();
        root.TryGetProperty("title", out _).Should().BeTrue();
        root.TryGetProperty("detail", out _).Should().BeTrue();
        root.TryGetProperty("instance", out _).Should().BeTrue();
    }

    #endregion

    #region Cross-Cutting Tests

    [Theory(Timeout = 60000)]
    [InlineData("/api/test/exceptions/not-found", HttpStatusCode.NotFound)]
    [InlineData("/api/test/exceptions/conflict", HttpStatusCode.Conflict)]
    [InlineData("/api/test/exceptions/range-not-satisfiable", HttpStatusCode.RequestedRangeNotSatisfiable)]
    [InlineData("/api/test/exceptions/bad-gateway", HttpStatusCode.BadGateway)]
    [InlineData("/api/test/exceptions/service-unavailable", HttpStatusCode.ServiceUnavailable)]
    public async Task TestExceptions_AllEndpoints_ReturnCorrectStatusCodes(string endpoint, HttpStatusCode expectedStatus)
    {
        // Arrange
        var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(expectedStatus);
    }

    [Theory(Timeout = 60000)]
    [InlineData("/api/test/exceptions/not-found")]
    [InlineData("/api/test/exceptions/conflict")]
    [InlineData("/api/test/exceptions/range-not-satisfiable")]
    [InlineData("/api/test/exceptions/bad-gateway")]
    [InlineData("/api/test/exceptions/service-unavailable")]
    public async Task TestExceptions_AllEndpoints_ReturnProblemDetailsContentType(string endpoint)
    {
        // Arrange
        var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync(endpoint);

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Theory(Timeout = 60000)]
    [InlineData("/api/test/exceptions/not-found")]
    [InlineData("/api/test/exceptions/conflict")]
    [InlineData("/api/test/exceptions/range-not-satisfiable")]
    [InlineData("/api/test/exceptions/bad-gateway")]
    [InlineData("/api/test/exceptions/service-unavailable")]
    public async Task TestExceptions_AllEndpoints_ReturnValidJson(string endpoint)
    {
        // Arrange
        var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync(endpoint);

        // Assert
        var act = async () => await response.Content.ReadFromJsonAsync<JsonDocument>(ServiceFixture.JsonOptions);
        await act.Should().NotThrowAsync();
    }

    [Theory(Timeout = 60000)]
    [InlineData("/api/test/exceptions/not-found", "resourceName", true)]
    [InlineData("/api/test/exceptions/conflict", "resourceName", true)]
    [InlineData("/api/test/exceptions/range-not-satisfiable", "resourceName", false)]
    [InlineData("/api/test/exceptions/bad-gateway", "resourceName", false)]
    [InlineData("/api/test/exceptions/service-unavailable", "resourceName", false)]
    public async Task TestExceptions_ResourceNameExtension_IncludedOnlyFor404And409(
        string endpoint,
        string propertyName,
        bool shouldExist)
    {
        // Arrange
        var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync(endpoint);

        // Assert
        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>(ServiceFixture.JsonOptions);
        var root = problemDetails!.RootElement;

        root.TryGetProperty(propertyName, out _).Should().Be(shouldExist);
    }

    #endregion
}
