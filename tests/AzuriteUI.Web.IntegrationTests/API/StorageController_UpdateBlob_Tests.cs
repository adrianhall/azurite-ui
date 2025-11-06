using System.Net;
using System.Net.Http.Json;
using AzuriteUI.Web.IntegrationTests.Helpers;
using AzuriteUI.Web.Services.CacheSync;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace AzuriteUI.Web.IntegrationTests.API;

[ExcludeFromCodeCoverage(Justification = "API Test class")]
public class StorageController_UpdateBlob_Tests(ServiceFixture fixture) : IClassFixture<ServiceFixture>
{
    #region Basic PUT Tests

    [Fact(Timeout = 60000)]
    public async Task UpdateBlob_WithNewMetadata_ShouldReturnUpdatedBlob()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var dto = new UpdateBlobDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            Metadata = new Dictionary<string, string>
            {
                ["environment"] = "production",
                ["version"] = "2.0"
            }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/containers/{containerName}/blobs/{blobName}", dto);
        var result = await response.Content.ReadFromJsonAsync<BlobDTO>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be(blobName);
        result.ContainerName.Should().Be(containerName);
        result.Metadata.Should().HaveCount(2);
        result.Metadata.Should().ContainKey("environment").WhoseValue.Should().Be("production");
        result.Metadata.Should().ContainKey("version").WhoseValue.Should().Be("2.0");
        result.ETag.Should().NotBeNullOrEmpty();
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateBlob_WithNewTags_ShouldReturnUpdatedBlob()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var dto = new UpdateBlobDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            Tags = new Dictionary<string, string>
            {
                ["category"] = "documents",
                ["status"] = "active"
            }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/containers/{containerName}/blobs/{blobName}", dto);
        var result = await response.Content.ReadFromJsonAsync<BlobDTO>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be(blobName);
        result.Tags.Should().HaveCount(2);
        result.Tags.Should().ContainKey("category").WhoseValue.Should().Be("documents");
        result.Tags.Should().ContainKey("status").WhoseValue.Should().Be("active");
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateBlob_WithEmptyMetadata_ShouldClearMetadata()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var dto = new UpdateBlobDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            Metadata = new Dictionary<string, string>()
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/containers/{containerName}/blobs/{blobName}", dto);
        var result = await response.Content.ReadFromJsonAsync<BlobDTO>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Metadata.Should().BeEmpty();
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateBlob_WithoutContainerAndBlobNameInBody_ShouldUseRouteParameters()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var dto = new UpdateBlobDTO
        {
            ContainerName = "",  // Empty or will be set from route
            BlobName = "",       // Empty or will be set from route
            Metadata = new Dictionary<string, string>
            {
                ["test"] = "value"
            }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/containers/{containerName}/blobs/{blobName}", dto);
        var result = await response.Content.ReadFromJsonAsync<BlobDTO>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be(blobName);
        result.ContainerName.Should().Be(containerName);
        result.Metadata.Should().ContainKey("test").WhoseValue.Should().Be("value");
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateBlob_WithMatchingNamesInBody_ShouldUpdateSuccessfully()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var dto = new UpdateBlobDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            Metadata = new Dictionary<string, string>
            {
                ["updated"] = "true"
            }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/containers/{containerName}/blobs/{blobName}", dto);
        var result = await response.Content.ReadFromJsonAsync<BlobDTO>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be(blobName);
        result.ContainerName.Should().Be(containerName);
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateBlob_MultipleUpdates_ShouldChangeETag()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // First update
        var dto1 = new UpdateBlobDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            Metadata = new Dictionary<string, string> { ["version"] = "1" }
        };
        var response1 = await client.PutAsJsonAsync($"/api/containers/{containerName}/blobs/{blobName}", dto1);
        var result1 = await response1.Content.ReadFromJsonAsync<BlobDTO>();
        var firstETag = result1!.ETag;

        // Second update
        var dto2 = new UpdateBlobDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            Metadata = new Dictionary<string, string> { ["version"] = "2" }
        };
        var response2 = await client.PutAsJsonAsync($"/api/containers/{containerName}/blobs/{blobName}", dto2);
        var result2 = await response2.Content.ReadFromJsonAsync<BlobDTO>();
        var secondETag = result2!.ETag;

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        firstETag.Should().NotBe(secondETag);
        result2.Metadata.Should().ContainKey("version").WhoseValue.Should().Be("2");
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateBlob_WithMetadataAndTags_ShouldUpdateBoth()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var dto = new UpdateBlobDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            Metadata = new Dictionary<string, string>
            {
                ["meta1"] = "value1"
            },
            Tags = new Dictionary<string, string>
            {
                ["tag1"] = "tagvalue1"
            }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/containers/{containerName}/blobs/{blobName}", dto);
        var result = await response.Content.ReadFromJsonAsync<BlobDTO>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Metadata.Should().ContainKey("meta1").WhoseValue.Should().Be("value1");
        result.Tags.Should().ContainKey("tag1").WhoseValue.Should().Be("tagvalue1");
    }

    #endregion

    #region Error Tests

    [Fact(Timeout = 60000)]
    public async Task UpdateBlob_WithNonExistentBlob_ShouldReturn404()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();
        var nonExistentBlob = "blob-that-does-not-exist.txt";

        var dto = new UpdateBlobDTO
        {
            ContainerName = containerName,
            BlobName = nonExistentBlob,
            Metadata = new Dictionary<string, string>
            {
                ["test"] = "value"
            }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/containers/{containerName}/blobs/{nonExistentBlob}", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateBlob_WithNonExistentContainer_ShouldReturn404()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();
        var nonExistentContainer = "container-that-does-not-exist";

        var dto = new UpdateBlobDTO
        {
            ContainerName = nonExistentContainer,
            BlobName = "test-blob.txt",
            Metadata = new Dictionary<string, string>()
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/containers/{nonExistentContainer}/blobs/test-blob.txt", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateBlob_WithMismatchedContainerName_ShouldReturn400()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var dto = new UpdateBlobDTO
        {
            ContainerName = "different-container-name",
            BlobName = blobName,
            Metadata = new Dictionary<string, string>()
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/containers/{containerName}/blobs/{blobName}", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        string? contentType = response.Content.Headers.ContentType?.MediaType;
        contentType.Should().Be("application/problem+json");
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateBlob_WithMismatchedBlobName_ShouldReturn400()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var dto = new UpdateBlobDTO
        {
            ContainerName = containerName,
            BlobName = "different-blob-name.txt",
            Metadata = new Dictionary<string, string>()
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/containers/{containerName}/blobs/{blobName}", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        string? contentType = response.Content.Headers.ContentType?.MediaType;
        contentType.Should().Be("application/problem+json");
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateBlob_WithInvalidBlobName_ShouldReturn400()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var dto = new UpdateBlobDTO
        {
            ContainerName = containerName,
            BlobName = "invalid-blob-!@#$",
            Metadata = new Dictionary<string, string>()
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/containers/{containerName}/blobs/invalid-blob-!@#$", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateBlob_WithNullBody_ShouldReturnBadRequest()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.PutAsync($"/api/containers/{containerName}/blobs/{blobName}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    #endregion

    #region Conditional Request Tests - If-Match

    [Fact(Timeout = 60000)]
    public async Task UpdateBlob_WithMatchingIfMatch_ShouldReturnOk()
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

        var dto = new UpdateBlobDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            Metadata = new Dictionary<string, string>
            {
                ["updated"] = "true"
            }
        };

        // Act
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/containers/{containerName}/blobs/{blobName}")
        {
            Content = JsonContent.Create(dto)
        };
        request.Headers.Add(HeaderNames.IfMatch, etag);
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<BlobDTO>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Metadata.Should().ContainKey("updated");
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateBlob_WithNonMatchingIfMatch_ShouldReturn412()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var dto = new UpdateBlobDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            Metadata = new Dictionary<string, string>()
        };

        // Act
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/containers/{containerName}/blobs/{blobName}")
        {
            Content = JsonContent.Create(dto)
        };
        request.Headers.Add(HeaderNames.IfMatch, "\"non-matching-etag\"");
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        // Verify blob metadata was NOT updated
        var verifyResponse = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var verifyResult = await verifyResponse.Content.ReadFromJsonAsync<BlobDTO>();
        verifyResult!.Metadata.Should().BeEmpty();
    }

    #endregion

    #region Cache Synchronization Tests

    [Fact(Timeout = 60000)]
    public async Task UpdateBlob_ShouldBeReflectedInGetEndpoint()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "test content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var dto = new UpdateBlobDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            Metadata = new Dictionary<string, string>
            {
                ["environment"] = "staging",
                ["updated"] = "true"
            },
            Tags = new Dictionary<string, string>
            {
                ["status"] = "active"
            }
        };

        // Act - Update the blob
        var updateResponse = await client.PutAsJsonAsync($"/api/containers/{containerName}/blobs/{blobName}", dto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Synchronize cache
        await SynchronizeCacheAsync();

        // Act - Get the blob
        var getResponse = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}");
        var result = await getResponse.Content.ReadFromJsonAsync<BlobDTO>();

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Metadata.Should().ContainKey("environment").WhoseValue.Should().Be("staging");
        result.Metadata.Should().ContainKey("updated").WhoseValue.Should().Be("true");
        result.Tags.Should().ContainKey("status").WhoseValue.Should().Be("active");
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
        if (etag.StartsWith('"') && etag.EndsWith('"'))
            return etag;

        // Otherwise, add quotes
        return $"\"{etag}\"";
    }

    #endregion
}
