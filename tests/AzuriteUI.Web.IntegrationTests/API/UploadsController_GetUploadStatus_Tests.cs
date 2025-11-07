using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Http;

namespace AzuriteUI.Web.IntegrationTests.API;

[ExcludeFromCodeCoverage(Justification = "API Test class")]
public class UploadsController_GetUploadStatus_Tests(ServiceFixture fixture) : BaseApiTest(fixture)
{   
    #region Basic GET Tests

    [Fact(Timeout = 60000)]
    public async Task GetUploadStatus_WithValidUploadId_ShouldReturnUploadStatus()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Create an upload session
        var createDto = new CreateUploadRequestDTO
        {
            BlobName = "test-blob.txt",
            ContainerName = containerName,
            ContentLength = 1048576,
            ContentType = "text/plain"
        };
        var createResponse = await client.PostAsJsonAsync($"/api/containers/{containerName}/blobs", createDto);
        var createdUpload = await createResponse.Content.ReadFromJsonAsync<UploadStatusDTO>(ServiceFixture.JsonOptions);

        // Act
        var response = await client.GetAsync($"/api/uploads/{createdUpload!.UploadId}");
        var result = await response.Content.ReadFromJsonAsync<UploadStatusDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.UploadId.Should().Be(createdUpload.UploadId);
        result.ContainerName.Should().Be(containerName);
        result.BlobName.Should().Be("test-blob.txt");
        result.ContentLength.Should().Be(1048576);
        result.ContentType.Should().Be("text/plain");
        result.UploadedBlocks.Should().BeEmpty();
        result.UploadedLength.Should().Be(0);
    }

    [Fact(Timeout = 60000)]
    public async Task GetUploadStatus_AfterUploadingBlocks_ShouldShowProgress()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Create an upload session
        var createDto = new CreateUploadRequestDTO
        {
            BlobName = "test-blob.txt",
            ContainerName = containerName,
            ContentLength = 200,
            ContentType = "text/plain"
        };
        var createResponse = await client.PostAsJsonAsync($"/api/containers/{containerName}/blobs", createDto);
        var createdUpload = await createResponse.Content.ReadFromJsonAsync<UploadStatusDTO>(ServiceFixture.JsonOptions);

        // Upload a block
        var blockId = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("block1"));
        var blockContent = new ByteArrayContent(new byte[100]);
        blockContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        await client.PutAsync($"/api/uploads/{createdUpload!.UploadId}/blocks/{blockId}", blockContent);

        // Act
        var response = await client.GetAsync($"/api/uploads/{createdUpload.UploadId}");
        var result = await response.Content.ReadFromJsonAsync<UploadStatusDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.UploadedBlocks.Should().Contain(blockId);
        result.UploadedLength.Should().Be(100);
    }

    [Fact(Timeout = 60000)]
    public async Task GetUploadStatus_MultipleRequests_ShouldReturnConsistentData()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        var createDto = new CreateUploadRequestDTO
        {
            BlobName = "test-blob.txt",
            ContainerName = containerName,
            ContentLength = 1024,
            ContentType = "text/plain"
        };
        var createResponse = await client.PostAsJsonAsync($"/api/containers/{containerName}/blobs", createDto);
        var createdUpload = await createResponse.Content.ReadFromJsonAsync<UploadStatusDTO>(ServiceFixture.JsonOptions);

        // Act - Get status multiple times
        var response1 = await client.GetAsync($"/api/uploads/{createdUpload!.UploadId}");
        var result1 = await response1.Content.ReadFromJsonAsync<UploadStatusDTO>(ServiceFixture.JsonOptions);

        var response2 = await client.GetAsync($"/api/uploads/{createdUpload.UploadId}");
        var result2 = await response2.Content.ReadFromJsonAsync<UploadStatusDTO>(ServiceFixture.JsonOptions);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        result1!.UploadId.Should().Be(result2!.UploadId);
        result1.BlobName.Should().Be(result2.BlobName);
        result1.ContentLength.Should().Be(result2.ContentLength);
    }

    #endregion

    #region Error Tests

    [Fact(Timeout = 60000)]
    public async Task GetUploadStatus_WithNonExistentUploadId_ShouldReturn404NotFound()
    {
        // Arrange
        using HttpClient client = Fixture.CreateClient();
        var nonExistentUploadId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/uploads/{nonExistentUploadId}");

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
    public async Task GetUploadStatus_WithInvalidGuid_ShouldReturnNotFound()
    {
        // Arrange
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/uploads/invalid-guid-format");

        // Assert
        response.Should().Be404NotFound();
    }

    [Fact(Timeout = 60000)]
    public async Task GetUploadStatus_AfterCancellation_ShouldReturn404NotFound()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Create and then cancel an upload
        var createDto = new CreateUploadRequestDTO
        {
            BlobName = "test-blob.txt",
            ContainerName = containerName,
            ContentLength = 1024,
            ContentType = "text/plain"
        };
        var createResponse = await client.PostAsJsonAsync($"/api/containers/{containerName}/blobs", createDto);
        var createdUpload = await createResponse.Content.ReadFromJsonAsync<UploadStatusDTO>(ServiceFixture.JsonOptions);

        await client.DeleteAsync($"/api/uploads/{createdUpload!.UploadId}");

        // Act
        var response = await client.GetAsync($"/api/uploads/{createdUpload.UploadId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
