using System.Net;
using System.Net.Http.Json;
using AzuriteUI.Web.IntegrationTests.Helpers;
using AzuriteUI.Web.Services.CacheSync;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace AzuriteUI.Web.IntegrationTests.API;

[ExcludeFromCodeCoverage(Justification = "API Test class")]
public class StorageController_DeleteBlob_Tests(ServiceFixture fixture) : IClassFixture<ServiceFixture>
{
    #region Basic DELETE Tests

    [Fact(Timeout = 60000)]
    public async Task DeleteBlob_WithExistingBlob_ShouldReturnNoContent()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/containers/{containerName}/blobs/{blobName}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().BeEmpty();
    }

    [Fact(Timeout = 60000)]
    public async Task DeleteBlob_WithMultipleBlobs_ShouldDeleteOnlySpecifiedBlob()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blob1 = await fixture.Azurite.CreateBlobAsync(containerName, "blob1.txt", "content1");
        var blob2 = await fixture.Azurite.CreateBlobAsync(containerName, "blob2.txt", "content2");
        var blob3 = await fixture.Azurite.CreateBlobAsync(containerName, "blob3.txt", "content3");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/containers/{containerName}/blobs/{blob2}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify blob2 is deleted and others remain
        await SynchronizeCacheAsync();
        var blob1Response = await client.GetAsync($"/api/containers/{containerName}/blobs/{blob1}");
        blob1Response.StatusCode.Should().Be(HttpStatusCode.OK);

        var blob2Response = await client.GetAsync($"/api/containers/{containerName}/blobs/{blob2}");
        blob2Response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var blob3Response = await client.GetAsync($"/api/containers/{containerName}/blobs/{blob3}");
        blob3Response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(Timeout = 60000)]
    public async Task DeleteBlob_IdempotentDeletion_ShouldReturnNoContentThenNotFound()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act - Delete the first time
        var firstResponse = await client.DeleteAsync($"/api/containers/{containerName}/blobs/{blobName}");
        firstResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act - Delete the second time (blob no longer exists in cache)
        var secondResponse = await client.DeleteAsync($"/api/containers/{containerName}/blobs/{blobName}");

        // Assert - Should return 404 because blob not found in cache
        secondResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Error Tests

    [Fact(Timeout = 60000)]
    public async Task DeleteBlob_WithNonExistentBlob_ShouldReturn404()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();
        var nonExistentBlob = "blob-that-does-not-exist.txt";

        // Act
        var response = await client.DeleteAsync($"/api/containers/{containerName}/blobs/{nonExistentBlob}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Timeout = 60000)]
    public async Task DeleteBlob_WithNonExistentContainer_ShouldReturn404()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();
        var nonExistentContainer = "container-that-does-not-exist";

        // Act
        var response = await client.DeleteAsync($"/api/containers/{nonExistentContainer}/blobs/test-blob.txt");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Timeout = 60000)]
    public async Task DeleteBlob_WithInvalidBlobName_ShouldReturn404()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act - Use a clearly invalid blob name
        var response = await client.DeleteAsync($"/api/containers/{containerName}/blobs/invalid-blob-!@#$");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Conditional Request Tests - If-Match

    [Fact(Timeout = 60000)]
    public async Task DeleteBlob_WithMatchingIfMatch_ShouldReturnNoContent()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Get the blob first to obtain its ETag
        var getResponse = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}");
        var blob = await getResponse.Content.ReadFromJsonAsync<BlobDTO>();
        var etag = EnsureQuotedETag(blob!.ETag);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/containers/{containerName}/blobs/{blobName}");
        request.Headers.Add(HeaderNames.IfMatch, etag);
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact(Timeout = 60000)]
    public async Task DeleteBlob_WithNonMatchingIfMatch_ShouldReturn412()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/containers/{containerName}/blobs/{blobName}");
        request.Headers.Add(HeaderNames.IfMatch, "\"non-matching-etag\"");
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        // Verify blob was NOT deleted
        var verifyResponse = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}");
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
