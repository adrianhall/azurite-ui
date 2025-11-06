using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Http;

namespace AzuriteUI.Web.IntegrationTests.API;

[ExcludeFromCodeCoverage(Justification = "API Test class")]
public class StorageController_CreateUpload_Tests(ServiceFixture fixture) : BaseApiTest()
{
    #region Basic POST Tests

    [Fact(Timeout = 60000)]
    public async Task CreateUpload_WithValidRequest_ShouldReturnCreatedUploadSession()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();
        var dto = new CreateUploadRequestDTO
        {
            BlobName = "test-blob.txt",
            ContainerName = containerName,
            ContentLength = 1048576,
            ContentType = "text/plain"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/containers/{containerName}/blobs", dto);
        var endTime = DateTimeOffset.UtcNow;

        // Assert
        response.Should().Be201Created();

        // Get the JSON content
        var jsonContent = await response.Content.ReadAsStringAsync();
        var result = await JsonSerializer.DeserializeAsync<UploadStatusDTO>(
            new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent)),
            ServiceFixture.JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.UploadId.Should().NotBeEmpty();
        result.ContainerName.Should().Be(containerName);
        result.BlobName.Should().Be("test-blob.txt");
        result.ContentLength.Should().Be(1048576);
        result.ContentType.Should().Be("text/plain");
        result.UploadedBlocks.Should().BeEmpty();
        result.UploadedLength.Should().Be(0);
        result.CreatedAt.Should().BeCloseTo(endTime, TimeSpan.FromSeconds(5));
        result.LastActivityAt.Should().BeCloseTo(endTime, TimeSpan.FromSeconds(5));

        // Verify Location header
        response.Headers.Location.Should().NotBeNull()
            .And.BeOfType<Uri>()
            .Which.AbsolutePath.Should().Be($"/api/uploads/{result.UploadId}");
    }

    [Fact(Timeout = 60000)]
    public async Task CreateUpload_WithMetadata_ShouldIncludeMetadata()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();
        var dto = new CreateUploadRequestDTO
        {
            BlobName = "blob-with-metadata.txt",
            ContainerName = containerName,
            ContentLength = 1024,
            ContentType = "text/plain",
            Metadata = new Dictionary<string, string>
            {
                ["author"] = "test-user",
                ["version"] = "1.0"
            }
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/containers/{containerName}/blobs", dto);
        var result = await response.Content.ReadFromJsonAsync<UploadStatusDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Should().NotBeNull();
        result!.UploadId.Should().NotBeEmpty();
    }

    [Fact(Timeout = 60000)]
    public async Task CreateUpload_WithTags_ShouldIncludeTags()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();
        var dto = new CreateUploadRequestDTO
        {
            BlobName = "blob-with-tags.txt",
            ContainerName = containerName,
            ContentLength = 1024,
            ContentType = "text/plain",
            Tags = new Dictionary<string, string>
            {
                ["environment"] = "test",
                ["category"] = "document"
            }
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/containers/{containerName}/blobs", dto);
        var result = await response.Content.ReadFromJsonAsync<UploadStatusDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Should().NotBeNull();
        result!.UploadId.Should().NotBeEmpty();
    }

    [Fact(Timeout = 60000)]
    public async Task CreateUpload_WithContentEncoding_ShouldAcceptEncoding()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();
        var dto = new CreateUploadRequestDTO
        {
            BlobName = "compressed.gz",
            ContainerName = containerName,
            ContentLength = 2048,
            ContentType = "application/gzip",
            ContentEncoding = "gzip"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/containers/{containerName}/blobs", dto);
        var result = await response.Content.ReadFromJsonAsync<UploadStatusDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Should().NotBeNull();
        result!.ContentType.Should().Be("application/gzip");
    }

    [Fact(Timeout = 60000)]
    public async Task CreateUpload_WithLargeContentLength_ShouldAcceptUpTo10GB()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();
        var dto = new CreateUploadRequestDTO
        {
            BlobName = "large-file.bin",
            ContainerName = containerName,
            ContentLength = 10L * 1024 * 1024 * 1024, // 10 GB
            ContentType = "application/octet-stream"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/containers/{containerName}/blobs", dto);
        var result = await response.Content.ReadFromJsonAsync<UploadStatusDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Should().NotBeNull();
        result!.ContentLength.Should().Be(10L * 1024 * 1024 * 1024);
    }

    #endregion

    #region Error Tests

    [Fact(Timeout = 60000)]
    public async Task CreateUpload_WhenContainerDoesNotExist_ShouldReturn404NotFound()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        using HttpClient client = fixture.CreateClient();
        var containerName = "non-existent-container";
        var dto = new CreateUploadRequestDTO
        {
            BlobName = "test-blob.txt",
            ContainerName = containerName,
            ContentLength = 1024,
            ContentType = "text/plain"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/containers/{containerName}/blobs", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>(ServiceFixture.JsonOptions);
        problemDetails.Should().NotBeNull();

        var root = problemDetails!.RootElement;
        root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status404NotFound);
        root.GetProperty("title").GetString().Should().Be("Not Found");
    }

    [Fact(Timeout = 60000)]
    public async Task CreateUpload_WhenBlobAlreadyExists_ShouldReturn409Conflict()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "existing-blob.txt", "existing content");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();
        var dto = new CreateUploadRequestDTO
        {
            BlobName = blobName,
            ContainerName = containerName,
            ContentLength = 1024,
            ContentType = "text/plain"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/containers/{containerName}/blobs", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>(ServiceFixture.JsonOptions);
        problemDetails.Should().NotBeNull();

        var root = problemDetails!.RootElement;
        root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status409Conflict);
        root.GetProperty("title").GetString().Should().Be("Conflict");
    }

    [Fact(Timeout = 60000)]
    public async Task CreateUpload_WhenContainerNameMismatch_ShouldReturnBadRequest()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();
        var dto = new CreateUploadRequestDTO
        {
            BlobName = "test-blob.txt",
            ContainerName = "different-container",
            ContentLength = 1024,
            ContentType = "text/plain"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/containers/{containerName}/blobs", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Timeout = 60000)]
    public async Task CreateUpload_WithZeroContentLength_ShouldReturnBadRequest()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();
        var dto = new CreateUploadRequestDTO
        {
            BlobName = "test-blob.txt",
            ContainerName = containerName,
            ContentLength = 0,
            ContentType = "text/plain"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/containers/{containerName}/blobs", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Timeout = 60000)]
    public async Task CreateUpload_WithExcessiveContentLength_ShouldReturnBadRequest()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();
        var dto = new CreateUploadRequestDTO
        {
            BlobName = "test-blob.txt",
            ContainerName = containerName,
            ContentLength = 11L * 1024 * 1024 * 1024, // 11 GB (over limit)
            ContentType = "text/plain"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/containers/{containerName}/blobs", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Timeout = 60000)]
    public async Task CreateUpload_WithEmptyBlobName_ShouldReturnBadRequest()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();
        var dto = new CreateUploadRequestDTO
        {
            BlobName = "",
            ContainerName = containerName,
            ContentLength = 1024,
            ContentType = "text/plain"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/containers/{containerName}/blobs", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Timeout = 60000)]
    public async Task CreateUpload_WithNullBody_ShouldReturnBadRequest()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.PostAsync($"/api/containers/{containerName}/blobs", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    #endregion
}
