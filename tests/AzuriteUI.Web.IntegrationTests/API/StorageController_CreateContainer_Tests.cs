using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AzuriteUI.Web.Services.CacheSync;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AzuriteUI.Web.IntegrationTests.API;

[ExcludeFromCodeCoverage(Justification = "API Test class")]
public class StorageController_CreateContainer_Tests(ServiceFixture fixture) : IClassFixture<ServiceFixture>
{
    #region Basic POST Tests

    [Fact(Timeout = 60000)]
    public async Task CreateContainer_WithValidRequest_ShouldReturnCreatedContainer()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        using HttpClient client = fixture.CreateClient();
        var dto = new CreateContainerDTO
        {
            ContainerName = "test-container"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/containers", dto);
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>();
        var endTime = DateTimeOffset.UtcNow;

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Should().NotBeNull();
        result!.Name.Should().Be("test-container");
        result.ETag.Should().NotBeNullOrEmpty();
        result.LastModified.Should().BeCloseTo(endTime, TimeSpan.FromSeconds(5));
        result.BlobCount.Should().Be(0);
        result.PublicAccess.Should().Be("none");

        // Verify Location header
        response.Headers.Location.Should().NotBeNull()
            .And.BeOfType<Uri>()
            .Which.AbsolutePath.Should().Be("/api/containers/test-container");

        // Verify ETag header
        var etagHeader = response.Headers.ETag?.ToString();
        etagHeader.Should().NotBeNull().And.BeEquivalentTo($"\"{result.ETag}\"");

        // Verify Last-Modified header
        response.Content.Headers.LastModified.Should().NotBeNull()
            .And.BeCloseTo(result.LastModified, TimeSpan.FromSeconds(1));
    }

    [Fact(Timeout = 60000)]
    public async Task CreateContainer_WithMetadata_ShouldIncludeMetadata()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        using HttpClient client = fixture.CreateClient();
        var dto = new CreateContainerDTO
        {
            ContainerName = "container-with-metadata",
            Metadata = new Dictionary<string, string>
            {
                ["environment"] = "test",
                ["owner"] = "integration-test",
                ["version"] = "1.0"
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/containers", dto);
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Should().NotBeNull();
        result!.Metadata.Should().HaveCount(3);
        result.Metadata.Should().ContainKey("environment").WhoseValue.Should().Be("test");
        result.Metadata.Should().ContainKey("owner").WhoseValue.Should().Be("integration-test");
        result.Metadata.Should().ContainKey("version").WhoseValue.Should().Be("1.0");
    }

    [Fact(Timeout = 60000)]
    public async Task CreateContainer_WithPublicAccessBlob_ShouldSetPublicAccess()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        using HttpClient client = fixture.CreateClient();
        var dto = new CreateContainerDTO
        {
            ContainerName = "public-container",
            PublicAccess = "blob"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/containers", dto);
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Should().NotBeNull();
        result!.PublicAccess.Should().Be("blob");
    }

    [Fact(Timeout = 60000)]
    public async Task CreateContainer_WithEmptyMetadata_ShouldCreateContainer()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        using HttpClient client = fixture.CreateClient();
        var dto = new CreateContainerDTO
        {
            ContainerName = "container-no-metadata",
            Metadata = new Dictionary<string, string>()
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/containers", dto);
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Should().NotBeNull();
        result!.Metadata.Should().BeEmpty();
    }

    #endregion

    #region Error Tests

    [Fact(Timeout = 60000)]
    public async Task CreateContainer_WhenContainerAlreadyExists_ShouldReturn409Conflict()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("existing-container");
        using HttpClient client = fixture.CreateClient();
        var dto = new CreateContainerDTO
        {
            ContainerName = containerName
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/containers", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>();
        problemDetails.Should().NotBeNull();

        var root = problemDetails!.RootElement;
        root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status409Conflict);
        root.GetProperty("title").GetString().Should().Be("Conflict");
    }

    [Fact(Timeout = 60000)]
    public async Task CreateContainer_WithInvalidContainerName_ShouldReturnBadRequest()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        using HttpClient client = fixture.CreateClient();
        var dto = new CreateContainerDTO
        {
            ContainerName = "Invalid_Container_Name!"  // Invalid characters
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/containers", dto);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact(Timeout = 60000)]
    public async Task CreateContainer_WithEmptyContainerName_ShouldReturnBadRequest()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        using HttpClient client = fixture.CreateClient();
        var dto = new CreateContainerDTO
        {
            ContainerName = ""
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/containers", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Timeout = 60000)]
    public async Task CreateContainer_WithInvalidPublicAccess_ShouldReturnBadRequest()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        using HttpClient client = fixture.CreateClient();

        var json = """
        {
            "containerName": "test-container",
            "publicAccess": "invalid-value"
        }
        """;
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/containers", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Timeout = 60000)]
    public async Task CreateContainer_WithNullBody_ShouldReturnBadRequest()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.PostAsync("/api/containers", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    #endregion

    #region Validation Tests

    [Fact(Timeout = 60000)]
    public async Task CreateContainer_WithVeryLongContainerName_ShouldReturnBadRequest()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        using HttpClient client = fixture.CreateClient();
        var dto = new CreateContainerDTO
        {
            ContainerName = new string('a', 64)  // Container names max out at 63 characters
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/containers", dto);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact(Timeout = 60000)]
    public async Task CreateContainer_WithValidMinimalName_ShouldCreateContainer()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        using HttpClient client = fixture.CreateClient();
        var dto = new CreateContainerDTO
        {
            ContainerName = "abc"  // 3 characters is minimum
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/containers", dto);
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Should().NotBeNull();
        result!.Name.Should().Be("abc");
    }

    [Fact(Timeout = 60000)]
    public async Task CreateContainer_WithHyphenatedName_ShouldCreateContainer()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        using HttpClient client = fixture.CreateClient();
        var dto = new CreateContainerDTO
        {
            ContainerName = "test-container-with-hyphens"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/containers", dto);
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Should().NotBeNull();
        result!.Name.Should().Be("test-container-with-hyphens");
    }

    #endregion

    #region Cache Synchronization Tests

    [Fact(Timeout = 60000)]
    public async Task CreateContainer_ShouldBeAccessibleViaGetEndpoint()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        using HttpClient client = fixture.CreateClient();
        var dto = new CreateContainerDTO
        {
            ContainerName = "new-container",
            Metadata = new Dictionary<string, string>
            {
                ["test"] = "value"
            }
        };

        // Act - Create the container
        var createResponse = await client.PostAsJsonAsync("/api/containers", dto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Synchronize cache
        await SynchronizeCacheAsync();

        // Act - Get the container
        var getResponse = await client.GetAsync("/api/containers/new-container");
        var result = await getResponse.Content.ReadFromJsonAsync<ContainerDTO>();

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be("new-container");
        result.Metadata.Should().ContainKey("test").WhoseValue.Should().Be("value");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Synchronizes the cache database with Azurite.
    /// </summary>
    private async Task SynchronizeCacheAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<ICacheSyncService>();
        await syncService.SynchronizeCacheAsync(CancellationToken.None);
    }

    #endregion
}
