using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Http;

namespace AzuriteUI.Web.IntegrationTests.API;

[ExcludeFromCodeCoverage(Justification = "API Test class")]
public class UploadsController_CommitUpload_Tests(ServiceFixture fixture) : BaseApiTest()
{
    #region Basic PUT Tests

    [Fact(Timeout = 60000)]
    public async Task CommitUpload_WithAllBlocksUploaded_ShouldCreateBlob()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var uploadId = await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 1024);

        // Upload blocks
        var blockId1 = Convert.ToBase64String(Encoding.UTF8.GetBytes("block1"));
        var blockId2 = Convert.ToBase64String(Encoding.UTF8.GetBytes("block2"));
        await UploadBlockAsync(client, uploadId, blockId1, 512);
        await UploadBlockAsync(client, uploadId, blockId2, 512);

        var commitDto = new CommitUploadRequestDTO
        {
            BlockIds = [blockId1, blockId2]
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/uploads/{uploadId}/commit", commitDto);
        var result = await response.Content.ReadFromJsonAsync<BlobDTO>(ServiceFixture.JsonOptions);
        var endTime = DateTimeOffset.UtcNow;

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be("test-blob.txt");
        result.ContainerName.Should().Be(containerName);
        result.ContentLength.Should().Be(1024);
        result.ETag.Should().NotBeNullOrEmpty();
        result.LastModified.Should().BeCloseTo(endTime, TimeSpan.FromSeconds(5));

        // Verify Location header
        response.Headers.Location.Should().NotBeNull()
            .And.BeOfType<Uri>()
            .Which.AbsolutePath.Should().Be($"/api/containers/{containerName}/blobs/test-blob.txt");

        // Verify blob exists in Azurite
        var blobExists = await fixture.Azurite.BlobExistsAsync(containerName, "test-blob.txt");
        blobExists.Should().BeTrue();

        // Verify upload session is removed
        var statusResponse = await client.GetAsync($"/api/uploads/{uploadId}");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Timeout = 60000)]
    public async Task CommitUpload_WithBlocksInSpecificOrder_ShouldRespectOrder()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var uploadId = await CreateUploadSessionAsync(client, containerName, "ordered-blob.txt", 300);

        // Upload blocks out of order
        var blockId1 = Convert.ToBase64String(Encoding.UTF8.GetBytes("block1"));
        var blockId2 = Convert.ToBase64String(Encoding.UTF8.GetBytes("block2"));
        var blockId3 = Convert.ToBase64String(Encoding.UTF8.GetBytes("block3"));
        await UploadBlockAsync(client, uploadId, blockId3, 100);
        await UploadBlockAsync(client, uploadId, blockId1, 100);
        await UploadBlockAsync(client, uploadId, blockId2, 100);

        // Commit in specific order: 1, 2, 3
        var commitDto = new CommitUploadRequestDTO
        {
            BlockIds = [blockId1, blockId2, blockId3]
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/uploads/{uploadId}/commit", commitDto);
        var result = await response.Content.ReadFromJsonAsync<BlobDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.ContentLength.Should().Be(300);
    }

    [Fact(Timeout = 60000)]
    public async Task CommitUpload_WithSingleBlock_ShouldCreateBlob()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var uploadId = await CreateUploadSessionAsync(client, containerName, "single-block.txt", 256);

        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("onlyblock"));
        await UploadBlockAsync(client, uploadId, blockId, 256);

        var commitDto = new CommitUploadRequestDTO
        {
            BlockIds = [blockId]
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/uploads/{uploadId}/commit", commitDto);
        var result = await response.Content.ReadFromJsonAsync<BlobDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.ContentLength.Should().Be(256);
    }

    [Fact(Timeout = 60000)]
    public async Task CommitUpload_WithMetadataAndTags_ShouldPreserveProperties()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var createDto = new CreateUploadRequestDTO
        {
            BlobName = "blob-with-props.txt",
            ContainerName = containerName,
            ContentLength = 256,
            ContentType = "text/plain",
            Metadata = new Dictionary<string, string>
            {
                ["author"] = "test-user",
                ["version"] = "1.0"
            },
            Tags = new Dictionary<string, string>
            {
                ["environment"] = "test",
                ["category"] = "document"
            }
        };
        var createResponse = await client.PostAsJsonAsync($"/api/containers/{containerName}/blobs", createDto);
        var createdUpload = await createResponse.Content.ReadFromJsonAsync<UploadStatusDTO>(ServiceFixture.JsonOptions);
        var uploadId = createdUpload!.UploadId;

        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block1"));
        await UploadBlockAsync(client, uploadId, blockId, 256);

        var commitDto = new CommitUploadRequestDTO
        {
            BlockIds = [blockId]
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/uploads/{uploadId}/commit", commitDto);
        var result = await response.Content.ReadFromJsonAsync<BlobDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Metadata.Should().ContainKey("author").WhoseValue.Should().Be("test-user");
        result.Metadata.Should().ContainKey("version").WhoseValue.Should().Be("1.0");
        result.Tags.Should().ContainKey("environment").WhoseValue.Should().Be("test");
        result.Tags.Should().ContainKey("category").WhoseValue.Should().Be("document");
    }

    #endregion

    #region Error Tests

    [Fact(Timeout = 60000)]
    public async Task CommitUpload_WithNonExistentUploadId_ShouldReturn404NotFound()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        using HttpClient client = fixture.CreateClient();

        var uploadId = Guid.NewGuid();
        var commitDto = new CommitUploadRequestDTO
        {
            BlockIds = [Convert.ToBase64String(Encoding.UTF8.GetBytes("block1"))]
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/uploads/{uploadId}/commit", commitDto);

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
    public async Task CommitUpload_WithMissingBlocks_ShouldReturnBadRequest()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var uploadId = await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 1024);

        // Upload only one block
        var blockId1 = Convert.ToBase64String(Encoding.UTF8.GetBytes("block1"));
        await UploadBlockAsync(client, uploadId, blockId1, 512);

        // Try to commit with a block that wasn't uploaded
        var blockId2 = Convert.ToBase64String(Encoding.UTF8.GetBytes("block2"));
        var commitDto = new CommitUploadRequestDTO
        {
            BlockIds = [blockId1, blockId2]
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/uploads/{uploadId}/commit", commitDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Timeout = 60000)]
    public async Task CommitUpload_WithEmptyBlockList_ShouldReturnBadRequest()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var uploadId = await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 1024);

        var commitDto = new CommitUploadRequestDTO
        {
            BlockIds = []
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/uploads/{uploadId}/commit", commitDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Timeout = 60000)]
    public async Task CommitUpload_WithInvalidGuidFormat_ShouldReturnBadRequest()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        using HttpClient client = fixture.CreateClient();

        var commitDto = new CommitUploadRequestDTO
        {
            BlockIds = [Convert.ToBase64String(Encoding.UTF8.GetBytes("block1"))]
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/uploads/invalid-guid/commit", commitDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Timeout = 60000)]
    public async Task CommitUpload_AfterAlreadyCommitted_ShouldReturn404NotFound()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var uploadId = await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 512);

        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block1"));
        await UploadBlockAsync(client, uploadId, blockId, 512);

        var commitDto = new CommitUploadRequestDTO
        {
            BlockIds = [blockId]
        };

        // Commit once successfully
        var response1 = await client.PutAsJsonAsync($"/api/uploads/{uploadId}/commit", commitDto);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Try to commit again
        var response2 = await client.PutAsJsonAsync($"/api/uploads/{uploadId}/commit", commitDto);

        // Assert
        response2.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Timeout = 60000)]
    public async Task CommitUpload_WithNullBody_ShouldReturnBadRequest()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var uploadId = await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 512);

        // Act
        var response = await client.PutAsync($"/api/uploads/{uploadId}/commit", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    #endregion
}
