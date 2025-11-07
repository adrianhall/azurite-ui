using System.Net;
using System.Net.Http.Json;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.Net.Http.Headers;

namespace AzuriteUI.Web.IntegrationTests.API;

[ExcludeFromCodeCoverage(Justification = "API Test class")]
public class StorageController_DeleteBlob_Tests(ServiceFixture fixture) : BaseApiTest(fixture)
{
    #region Basic DELETE Tests

    [Fact(Timeout = 60000)]
    public async Task DeleteBlob_WithExistingBlob_ShouldReturnNoContent()
    {
        // Arrange        
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await Fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

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
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        var blob1 = await Fixture.Azurite.CreateBlobAsync(containerName, "blob1.txt", "content1");
        var blob2 = await Fixture.Azurite.CreateBlobAsync(containerName, "blob2.txt", "content2");
        var blob3 = await Fixture.Azurite.CreateBlobAsync(containerName, "blob3.txt", "content3");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/containers/{containerName}/blobs/{blob2}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify blob2 is deleted and others remain
        await Fixture.SynchronizeCacheAsync();
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
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await Fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

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
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();
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
        using HttpClient client = Fixture.CreateClient();
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
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

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
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await Fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Get the blob first to obtain its ETag
        var getResponse = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}");
        var blob = await getResponse.Content.ReadFromJsonAsync<BlobDTO>(ServiceFixture.JsonOptions);
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
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await Fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

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
}
