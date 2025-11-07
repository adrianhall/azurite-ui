using System.Net;
using System.Net.Http.Json;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace AzuriteUI.Web.IntegrationTests.API;

[ExcludeFromCodeCoverage(Justification = "API Test class")]
public class StorageController_UpdateContainer_Tests(ServiceFixture fixture) : BaseApiTest(fixture)
{
    #region Basic PUT Tests

    [Fact(Timeout = 60000)]
    public async Task UpdateContainer_WithNewMetadata_ShouldReturnUpdatedContainer()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        var dto = new UpdateContainerDTO
        {
            ContainerName = containerName,
            Metadata = new Dictionary<string, string>
            {
                ["environment"] = "production",
                ["version"] = "2.0"
            }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/containers/{containerName}", dto);
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be(containerName);
        result.Metadata.Should().HaveCount(2);
        result.Metadata.Should().ContainKey("environment").WhoseValue.Should().Be("production");
        result.Metadata.Should().ContainKey("version").WhoseValue.Should().Be("2.0");
        result.ETag.Should().NotBeNullOrEmpty();
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateContainer_WithEmptyMetadata_ShouldClearMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container", metadata);
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        var dto = new UpdateContainerDTO
        {
            ContainerName = containerName,
            Metadata = new Dictionary<string, string>()
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/containers/{containerName}", dto);
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Metadata.Should().BeEmpty();
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateContainer_ReplacingExistingMetadata_ShouldUpdateSuccessfully()
    {
        // Arrange
        var originalMetadata = new Dictionary<string, string>
        {
            ["oldkey"] = "oldvalue"
        };
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container", originalMetadata);
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        var dto = new UpdateContainerDTO
        {
            ContainerName = containerName,
            Metadata = new Dictionary<string, string>
            {
                ["newkey"] = "newvalue"
            }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/containers/{containerName}", dto);
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Metadata.Should().HaveCount(1);
        result.Metadata.Should().ContainKey("newkey").WhoseValue.Should().Be("newvalue");
        result.Metadata.Should().NotContainKey("oldkey");
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateContainer_WithoutContainerNameInBody_ShouldUseRouteParameter()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        var dto = new UpdateContainerDTO
        {
            ContainerName = "",  // Empty or will be set from route
            Metadata = new Dictionary<string, string>
            {
                ["test"] = "value"
            }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/containers/{containerName}", dto);
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be(containerName);
        result.Metadata.Should().ContainKey("test").WhoseValue.Should().Be("value");
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateContainer_WithMatchingContainerNameInBody_ShouldUpdateSuccessfully()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        var dto = new UpdateContainerDTO
        {
            ContainerName = containerName,
            Metadata = new Dictionary<string, string>
            {
                ["updated"] = "true"
            }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/containers/{containerName}", dto);
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be(containerName);
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateContainer_MultipleUpdates_ShouldChangeETag()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // First update
        var dto1 = new UpdateContainerDTO
        {
            ContainerName = containerName,
            Metadata = new Dictionary<string, string> { ["version"] = "1" }
        };
        var response1 = await client.PutAsJsonAsync($"/api/containers/{containerName}", dto1);
        var result1 = await response1.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);
        var firstETag = result1!.ETag;

        // Second update
        var dto2 = new UpdateContainerDTO
        {
            ContainerName = containerName,
            Metadata = new Dictionary<string, string> { ["version"] = "2" }
        };
        var response2 = await client.PutAsJsonAsync($"/api/containers/{containerName}", dto2);
        var result2 = await response2.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);
        var secondETag = result2!.ETag;

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        firstETag.Should().NotBe(secondETag);
        result2.Metadata.Should().ContainKey("version").WhoseValue.Should().Be("2");
    }

    #endregion

    #region Error Tests

    [Fact(Timeout = 60000)]
    public async Task UpdateContainer_WithNonExistentContainer_ShouldReturn404()
    {
        // Arrange
        using HttpClient client = Fixture.CreateClient();
        var nonExistentContainer = "container-that-does-not-exist";

        var dto = new UpdateContainerDTO
        {
            ContainerName = nonExistentContainer,
            Metadata = new Dictionary<string, string>
            {
                ["test"] = "value"
            }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/containers/{nonExistentContainer}", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateContainer_WithMismatchedContainerName_ShouldReturn400()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        var dto = new UpdateContainerDTO
        {
            ContainerName = "different-container-name",
            Metadata = new Dictionary<string, string>
            {
                ["test"] = "value"
            }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/containers/{containerName}", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        string? contentType = response.Content.Headers.ContentType?.MediaType;
        contentType.Should().Be("application/problem+json");
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateContainer_WithInvalidContainerName_ShouldReturn400()
    {
        // Arrange
        using HttpClient client = Fixture.CreateClient();

        var dto = new UpdateContainerDTO
        {
            ContainerName = "invalid-container-!@#$",
            Metadata = new Dictionary<string, string>()
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/containers/invalid-container-!@#$", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateContainer_WithNullBody_ShouldReturnBadRequest()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.PutAsync($"/api/containers/{containerName}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    #endregion

    #region Conditional Request Tests - If-Match

    [Fact(Timeout = 60000)]
    public async Task UpdateContainer_WithMatchingIfMatch_ShouldReturnOk()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Get the container first to obtain its ETag
        var getResponse = await client.GetAsync($"/api/containers/{containerName}");
        var container = await getResponse.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);
        var etag = EnsureQuotedETag(container!.ETag);

        var dto = new UpdateContainerDTO
        {
            ContainerName = containerName,
            Metadata = new Dictionary<string, string>
            {
                ["updated"] = "true"
            }
        };

        // Act
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/containers/{containerName}")
        {
            Content = JsonContent.Create(dto)
        };
        request.Headers.Add(HeaderNames.IfMatch, etag);
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Metadata.Should().ContainKey("updated");
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateContainer_WithNonMatchingIfMatch_ShouldReturn412()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        var dto = new UpdateContainerDTO
        {
            ContainerName = containerName,
            Metadata = new Dictionary<string, string>()
        };

        // Act
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/containers/{containerName}")
        {
            Content = JsonContent.Create(dto)
        };
        request.Headers.Add(HeaderNames.IfMatch, "\"non-matching-etag\"");
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        // Verify container metadata was NOT updated
        var verifyResponse = await client.GetAsync($"/api/containers/{containerName}");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var verifyResult = await verifyResponse.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);
        verifyResult!.Metadata.Should().BeEmpty();
    }

    #endregion

    #region Cache Synchronization Tests

    [Fact(Timeout = 60000)]
    public async Task UpdateContainer_ShouldBeReflectedInGetEndpoint()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        var dto = new UpdateContainerDTO
        {
            ContainerName = containerName,
            Metadata = new Dictionary<string, string>
            {
                ["environment"] = "staging",
                ["updated"] = "true"
            }
        };

        // Act - Update the container
        var updateResponse = await client.PutAsJsonAsync($"/api/containers/{containerName}", dto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Synchronize cache
        await Fixture.SynchronizeCacheAsync();

        // Act - Get the container
        var getResponse = await client.GetAsync($"/api/containers/{containerName}");
        var result = await getResponse.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Metadata.Should().ContainKey("environment").WhoseValue.Should().Be("staging");
        result.Metadata.Should().ContainKey("updated").WhoseValue.Should().Be("true");
    }

    #endregion
}
