using System.Net;
using System.Net.Http.Json;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace AzuriteUI.Web.IntegrationTests.API;

[ExcludeFromCodeCoverage(Justification = "API Test class")]
public class StorageController_DeleteContainer_Tests(ServiceFixture fixture) : BaseApiTest(fixture)
{
    #region Basic DELETE Tests

    [Fact(Timeout = 60000)]
    public async Task DeleteContainer_WithExistingContainer_ShouldReturnNoContent()
    {
        // Arrange        
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/containers/{containerName}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().BeEmpty();
    }

    [Fact(Timeout = 60000)]
    public async Task DeleteContainer_WithContainerContainingBlobs_ShouldDeleteContainerAndBlobs()
    {
        // Arrange        
        var containerName = await Fixture.Azurite.CreateContainerAsync("container-with-blobs");
        await Fixture.Azurite.CreateBlobAsync(containerName, "blob1.txt", "content1");
        await Fixture.Azurite.CreateBlobAsync(containerName, "blob2.txt", "content2");
        await Fixture.Azurite.CreateBlobAsync(containerName, "blob3.txt", "content3");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/containers/{containerName}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify container and blobs are deleted
        await Fixture.SynchronizeCacheAsync();
        var getResponse = await client.GetAsync($"/api/containers/{containerName}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Timeout = 60000)]
    public async Task DeleteContainer_WithEmptyContainer_ShouldDeleteSuccessfully()
    {
        // Arrange        
        var containerName = await Fixture.Azurite.CreateContainerAsync("empty-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/containers/{containerName}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact(Timeout = 60000)]
    public async Task DeleteContainer_IdempotentDeletion_ShouldReturnNoContentTwice()
    {
        // Arrange        
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act - Delete the first time
        var firstResponse = await client.DeleteAsync($"/api/containers/{containerName}");
        firstResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act - Delete the second time (container no longer exists in cache)
        var secondResponse = await client.DeleteAsync($"/api/containers/{containerName}");

        // Assert - Should return 404 because container not found in cache
        secondResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Error Tests

    [Fact(Timeout = 60000)]
    public async Task DeleteContainer_WithNonExistentContainer_ShouldReturn404()
    {
        // Arrange        
        using HttpClient client = Fixture.CreateClient();
        var nonExistentContainer = "container-that-does-not-exist";

        // Act
        var response = await client.DeleteAsync($"/api/containers/{nonExistentContainer}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Timeout = 60000)]
    public async Task DeleteContainer_WithInvalidContainerName_ShouldReturn404()
    {
        // Arrange        
        using HttpClient client = Fixture.CreateClient();

        // Act - Use a clearly invalid container name
        var response = await client.DeleteAsync("/api/containers/invalid-container-!@#$");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Conditional Request Tests - If-Match

    [Fact(Timeout = 60000)]
    public async Task DeleteContainer_WithMatchingIfMatch_ShouldReturnNoContent()
    {
        // Arrange        
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Get the container first to obtain its ETag
        var getResponse = await client.GetAsync($"/api/containers/{containerName}");
        var container = await getResponse.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);
        var etag = EnsureQuotedETag(container!.ETag);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/containers/{containerName}");
        request.Headers.Add(HeaderNames.IfMatch, etag);
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact(Timeout = 60000)]
    public async Task DeleteContainer_WithNonMatchingIfMatch_ShouldReturn412()
    {
        // Arrange        
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/containers/{containerName}");
        request.Headers.Add(HeaderNames.IfMatch, "\"non-matching-etag\"");
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        // Verify container was NOT deleted
        var verifyResponse = await client.GetAsync($"/api/containers/{containerName}");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
