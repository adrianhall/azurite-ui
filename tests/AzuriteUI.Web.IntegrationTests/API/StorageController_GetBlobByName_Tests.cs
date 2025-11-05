using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AzuriteUI.Web.IntegrationTests.Helpers;
using AzuriteUI.Web.Services.CacheSync;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace AzuriteUI.Web.IntegrationTests.API;

[ExcludeFromCodeCoverage(Justification = "API Test class")]
public class StorageController_GetBlobByName_Tests(ServiceFixture fixture) : IClassFixture<ServiceFixture>
{
    #region Basic GET Tests

    [Fact(Timeout = 60000)]
    public async Task GetBlobByName_WithExistingBlob_ShouldReturnBlob()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}");
        var result = await response.Content.ReadFromJsonAsync<BlobDTO>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be(blobName);
        result.ContainerName.Should().Be(containerName);
        result.ETag.Should().NotBeNullOrEmpty();
        result.LastModified.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        result.ContentType.Should().Be("text/plain");
        result.ContentLength.Should().BeGreaterThan(0);
    }

    [Fact(Timeout = 60000)]
    public async Task GetBlobByName_WithBlobWithMetadata_ShouldIncludeMetadata()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}");
        var result = await response.Content.ReadFromJsonAsync<BlobDTO>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be(blobName);
        result.Metadata.Should().NotBeNull();
    }

    #endregion

    #region Error Tests

    [Fact(Timeout = 60000)]
    public async Task GetBlobByName_WithNonExistentBlob_ShouldReturn404()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();
        var nonExistentBlob = "blob-that-does-not-exist-12345.txt";

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs/{nonExistentBlob}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Timeout = 60000)]
    public async Task GetBlobByName_WithNonExistentContainer_ShouldReturn404()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();
        var nonExistentContainer = "container-that-does-not-exist-12345";

        // Act
        var response = await client.GetAsync($"/api/containers/{nonExistentContainer}/blobs/test-blob.txt");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Timeout = 60000)]
    public async Task GetBlobByName_WithInvalidBlobName_ShouldReturn404()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act - Use a clearly invalid blob name
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs/invalid-blob-!@#$");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Conditional Request Tests - If-Match

    [Fact(Timeout = 60000)]
    public async Task GetBlobByName_WithMatchingIfMatch_ShouldReturn200()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Get the blob first to obtain its ETag
        var initialResponse = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}");
        var blob = await initialResponse.Content.ReadFromJsonAsync<BlobDTO>();
        var etag = EnsureQuotedETag(blob!.ETag);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}/blobs/{blobName}");
        request.Headers.Add(HeaderNames.IfMatch, etag);
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<BlobDTO>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be(blobName);
    }

    [Fact(Timeout = 60000)]
    public async Task GetBlobByName_WithNonMatchingIfMatch_ShouldReturn412()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}/blobs/{blobName}");
        request.Headers.Add(HeaderNames.IfMatch, "\"non-matching-etag\"");
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    #endregion

    #region Conditional Request Tests - If-None-Match

    [Fact(Timeout = 60000)]
    public async Task GetBlobByName_WithMatchingIfNoneMatch_ShouldReturn304()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Get the blob first to obtain its ETag
        var initialResponse = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}");
        var blob = await initialResponse.Content.ReadFromJsonAsync<BlobDTO>();
        var etag = EnsureQuotedETag(blob!.ETag);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}/blobs/{blobName}");
        request.Headers.Add(HeaderNames.IfNoneMatch, etag);
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotModified);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().BeEmpty();
    }

    [Fact(Timeout = 60000)]
    public async Task GetBlobByName_WithNonMatchingIfNoneMatch_ShouldReturn200()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}/blobs/{blobName}");
        request.Headers.Add(HeaderNames.IfNoneMatch, "\"different-etag\"");
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<BlobDTO>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be(blobName);
    }

    #endregion

    #region Conditional Request Tests - If-Modified-Since

    [Fact(Timeout = 60000)]
    public async Task GetBlobByName_WithIfModifiedSinceBeforeLastModified_ShouldReturn200()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Get the blob first to obtain its LastModified
        var initialResponse = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}");
        var blob = await initialResponse.Content.ReadFromJsonAsync<BlobDTO>();
        var lastModified = blob!.LastModified;

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}/blobs/{blobName}");
        request.Headers.Add(HeaderNames.IfModifiedSince, lastModified.AddHours(-1).ToString("R"));
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<BlobDTO>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be(blobName);
    }

    [Fact(Timeout = 60000)]
    public async Task GetBlobByName_WithIfModifiedSinceAfterLastModified_ShouldReturn304()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Get the blob first to obtain its LastModified
        var initialResponse = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}");
        var blob = await initialResponse.Content.ReadFromJsonAsync<BlobDTO>();
        var lastModified = blob!.LastModified;

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}/blobs/{blobName}");
        request.Headers.Add(HeaderNames.IfModifiedSince, lastModified.AddHours(1).ToString("R"));
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotModified);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().BeEmpty();
    }

    #endregion

    #region Conditional Request Tests - If-Unmodified-Since

    [Fact(Timeout = 60000)]
    public async Task GetBlobByName_WithIfUnmodifiedSinceAfterLastModified_ShouldReturn200()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Get the blob first to obtain its LastModified
        var initialResponse = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}");
        var blob = await initialResponse.Content.ReadFromJsonAsync<BlobDTO>();
        var lastModified = blob!.LastModified;

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}/blobs/{blobName}");
        request.Headers.Add(HeaderNames.IfUnmodifiedSince, lastModified.AddHours(1).ToString("R"));
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<BlobDTO>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be(blobName);
    }

    [Fact(Timeout = 60000)]
    public async Task GetBlobByName_WithIfUnmodifiedSinceBeforeLastModified_ShouldReturn412()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Get the blob first to obtain its LastModified
        var initialResponse = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}");
        var blob = await initialResponse.Content.ReadFromJsonAsync<BlobDTO>();
        var lastModified = blob!.LastModified;

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}/blobs/{blobName}");
        request.Headers.Add(HeaderNames.IfUnmodifiedSince, lastModified.AddHours(-1).ToString("R"));
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    #endregion

    #region Conditional Request Tests - Combined Headers

    [Fact(Timeout = 60000)]
    public async Task GetBlobByName_WithMatchingIfMatchAndIfUnmodifiedSince_ShouldReturn200()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Get the blob first to obtain its ETag and LastModified
        var initialResponse = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}");
        var blob = await initialResponse.Content.ReadFromJsonAsync<BlobDTO>();
        var etag = EnsureQuotedETag(blob!.ETag);
        var lastModified = blob.LastModified;

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}/blobs/{blobName}");
        request.Headers.Add(HeaderNames.IfMatch, etag);
        request.Headers.Add(HeaderNames.IfUnmodifiedSince, lastModified.AddHours(1).ToString("R"));
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<BlobDTO>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be(blobName);
    }

    [Fact(Timeout = 60000)]
    public async Task GetBlobByName_WithMatchingIfNoneMatchAndIfModifiedSince_ShouldReturn304()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Get the blob first to obtain its ETag and LastModified
        var initialResponse = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}");
        var blob = await initialResponse.Content.ReadFromJsonAsync<BlobDTO>();
        var etag = EnsureQuotedETag(blob!.ETag);
        var lastModified = blob.LastModified;

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}/blobs/{blobName}");
        request.Headers.Add(HeaderNames.IfNoneMatch, etag);
        request.Headers.Add(HeaderNames.IfModifiedSince, lastModified.AddHours(1).ToString("R"));
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotModified);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().BeEmpty();
    }

    [Fact(Timeout = 60000)]
    public async Task GetBlobByName_WithIfMatchTakesPrecedenceOverIfUnmodifiedSince_ShouldReturn200()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Get the blob first to obtain its ETag and LastModified
        var initialResponse = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}");
        var blob = await initialResponse.Content.ReadFromJsonAsync<BlobDTO>();
        var etag = EnsureQuotedETag(blob!.ETag);
        var lastModified = blob.LastModified;

        // Act - If-Match matches, so should succeed even though If-Unmodified-Since would fail
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}/blobs/{blobName}");
        request.Headers.Add(HeaderNames.IfMatch, etag);
        request.Headers.Add(HeaderNames.IfUnmodifiedSince, lastModified.AddHours(-1).ToString("R"));
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<BlobDTO>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
    }

    [Fact(Timeout = 60000)]
    public async Task GetBlobByName_WithIfNoneMatchTakesPrecedenceOverIfModifiedSince_ShouldReturn200()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Get the blob first to obtain its LastModified
        var initialResponse = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}");
        var blob = await initialResponse.Content.ReadFromJsonAsync<BlobDTO>();
        var lastModified = blob!.LastModified;

        // Act - If-None-Match doesn't match, so should succeed even though If-Modified-Since would return 304
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}/blobs/{blobName}");
        request.Headers.Add(HeaderNames.IfNoneMatch, "\"different-etag\"");
        request.Headers.Add(HeaderNames.IfModifiedSince, lastModified.AddHours(1).ToString("R"));
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<BlobDTO>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
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
