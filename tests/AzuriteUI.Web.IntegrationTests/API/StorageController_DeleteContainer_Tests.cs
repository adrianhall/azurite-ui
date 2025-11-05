using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AzuriteUI.Web.IntegrationTests.Helpers;
using AzuriteUI.Web.Services.CacheSync;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace AzuriteUI.Web.IntegrationTests.API;

[ExcludeFromCodeCoverage(Justification = "API Test class")]
public class StorageController_DeleteContainer_Tests(ServiceFixture fixture) : IClassFixture<ServiceFixture>
{
    #region Basic DELETE Tests

    [Fact(Timeout = 60000)]
    public async Task DeleteContainer_WithExistingContainer_ShouldReturnNoContent()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

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
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("container-with-blobs");
        await fixture.Azurite.CreateBlobAsync(containerName, "blob1.txt", "content1");
        await fixture.Azurite.CreateBlobAsync(containerName, "blob2.txt", "content2");
        await fixture.Azurite.CreateBlobAsync(containerName, "blob3.txt", "content3");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/containers/{containerName}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify container and blobs are deleted
        await SynchronizeCacheAsync();
        var getResponse = await client.GetAsync($"/api/containers/{containerName}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Timeout = 60000)]
    public async Task DeleteContainer_WithEmptyContainer_ShouldDeleteSuccessfully()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("empty-container");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/containers/{containerName}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact(Timeout = 60000)]
    public async Task DeleteContainer_IdempotentDeletion_ShouldReturnNoContentTwice()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

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
        await fixture.Azurite.CleanupAsync();
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();
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
        await fixture.Azurite.CleanupAsync();
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

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
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Get the container first to obtain its ETag
        var getResponse = await client.GetAsync($"/api/containers/{containerName}");
        var container = await getResponse.Content.ReadFromJsonAsync<ContainerDTO>();
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
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

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

    /// <summary>
    /// Ensures the ETag is properly quoted for use in HTTP headers.
    /// </summary>
    /// <param name="etag">The ETag value, which may or may not be quoted.</param>
    /// <returns>A properly quoted ETag value.</returns>
    private static string EnsureQuotedETag(string etag)
    {
        if (string.IsNullOrEmpty(etag))
            return etag;

        // If it's already quoted, return as-is
        if (etag.StartsWith("\"") && etag.EndsWith("\""))
            return etag;

        // Otherwise, add quotes
        return $"\"{etag}\"";
    }

    #endregion
}
