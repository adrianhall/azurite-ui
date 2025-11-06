using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Http;

namespace AzuriteUI.Web.IntegrationTests.API;

[ExcludeFromCodeCoverage(Justification = "API Test class")]
public class UploadsController_UploadBlock_Tests(ServiceFixture fixture) : BaseApiTest()
{
    #region Basic PUT Tests

    [Fact(Timeout = 60000)]
    public async Task UploadBlock_WithValidBlock_ShouldSucceed()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var uploadId = await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 1024);
        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block1"));
        var blockData = new byte[512];
        new Random().NextBytes(blockData);
        var content = new ByteArrayContent(blockData);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        // Act
        var response = await client.PutAsync($"/api/uploads/{uploadId}/blocks/{blockId}", content);
        var result = await response.Content.ReadFromJsonAsync<JsonDocument>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        var root = result!.RootElement;
        root.GetProperty("uploadId").GetGuid().Should().Be(uploadId);
        root.GetProperty("blockId").GetString().Should().Be(blockId);
    }

    [Fact(Timeout = 60000)]
    public async Task UploadBlock_MultipleBlocks_ShouldSucceed()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var uploadId = await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 2048);

        // Act - Upload multiple blocks
        var blockId1 = Convert.ToBase64String(Encoding.UTF8.GetBytes("block1"));
        var blockData1 = new byte[512];
        var content1 = new ByteArrayContent(blockData1);
        content1.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        var response1 = await client.PutAsync($"/api/uploads/{uploadId}/blocks/{blockId1}", content1);

        var blockId2 = Convert.ToBase64String(Encoding.UTF8.GetBytes("block2"));
        var blockData2 = new byte[512];
        var content2 = new ByteArrayContent(blockData2);
        content2.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        var response2 = await client.PutAsync($"/api/uploads/{uploadId}/blocks/{blockId2}", content2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify status shows both blocks
        var statusResponse = await client.GetAsync($"/api/uploads/{uploadId}");
        var status = await statusResponse.Content.ReadFromJsonAsync<UploadStatusDTO>(ServiceFixture.JsonOptions);
        status!.UploadedBlocks.Should().HaveCount(2);
        status.UploadedBlocks.Should().Contain(blockId1);
        status.UploadedBlocks.Should().Contain(blockId2);
        status.UploadedLength.Should().Be(1024);
    }

    [Fact(Timeout = 60000)]
    public async Task UploadBlock_WithContentMD5Header_ShouldSucceed()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var uploadId = await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 1024);
        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block1"));
        var blockData = new byte[256];
        new Random().NextBytes(blockData);

        var md5Hash = Convert.ToBase64String(MD5.HashData(blockData));
        var content = new ByteArrayContent(blockData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Headers.Add("Content-MD5", md5Hash);

        // Act
        var response = await client.PutAsync($"/api/uploads/{uploadId}/blocks/{blockId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(Timeout = 60000)]
    public async Task UploadBlock_ReuploadingSameBlockId_ShouldSucceedIdempotently()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var uploadId = await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 1024);
        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block1"));
        var blockData = new byte[256];
        var content1 = new ByteArrayContent(blockData);
        content1.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        // Act - Upload same block twice
        var response1 = await client.PutAsync($"/api/uploads/{uploadId}/blocks/{blockId}", content1);

        var content2 = new ByteArrayContent(blockData);
        content2.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        var response2 = await client.PutAsync($"/api/uploads/{uploadId}/blocks/{blockId}", content2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify status shows only one block
        var statusResponse = await client.GetAsync($"/api/uploads/{uploadId}");
        var status = await statusResponse.Content.ReadFromJsonAsync<UploadStatusDTO>(ServiceFixture.JsonOptions);
        status!.UploadedBlocks.Should().HaveCount(1);
        status.UploadedBlocks.Should().Contain(blockId);
    }

    [Fact(Timeout = 60000)]
    public async Task UploadBlock_WithLargeBlock_ShouldSucceed()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var uploadId = await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 5 * 1024 * 1024);
        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("largeblock"));
        var blockData = new byte[4 * 1024 * 1024]; // 4 MB
        var content = new ByteArrayContent(blockData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        // Act
        var response = await client.PutAsync($"/api/uploads/{uploadId}/blocks/{blockId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Error Tests

    [Fact(Timeout = 60000)]
    public async Task UploadBlock_WithNonExistentUploadId_ShouldReturn404NotFound()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        using HttpClient client = fixture.CreateClient();

        var uploadId = Guid.NewGuid();
        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block1"));
        var blockData = new byte[256];
        var content = new ByteArrayContent(blockData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        // Act
        var response = await client.PutAsync($"/api/uploads/{uploadId}/blocks/{blockId}", content);

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
    public async Task UploadBlock_WithInvalidBlockId_ShouldReturnBadRequest()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var uploadId = await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 1024);
        var invalidBlockId = "not-base64-encoded!!!";
        var blockData = new byte[256];
        var content = new ByteArrayContent(blockData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        // Act
        var response = await client.PutAsync($"/api/uploads/{uploadId}/blocks/{invalidBlockId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Timeout = 60000)]
    public async Task UploadBlock_WithEmptyContent_ShouldReturnBadRequest()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var uploadId = await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 1024);
        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block1"));
        var content = new ByteArrayContent([]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        // Act
        var response = await client.PutAsync($"/api/uploads/{uploadId}/blocks/{blockId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Timeout = 60000)]
    public async Task UploadBlock_WithInvalidGuidFormat_ShouldReturn404NotFound()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        using HttpClient client = fixture.CreateClient();

        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block1"));
        var blockData = new byte[256];
        var content = new ByteArrayContent(blockData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        // Act
        var response = await client.PutAsync("/api/uploads/invalid-guid/blocks/" + blockId, content);

        // Assert
        response.Should().Be404NotFound();
    }

    [Fact(Timeout = 60000)]
    public async Task UploadBlock_AfterUploadCancelled_ShouldReturn404NotFound()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        var uploadId = await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 1024);
        await client.DeleteAsync($"/api/uploads/{uploadId}");

        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block1"));
        var blockData = new byte[256];
        var content = new ByteArrayContent(blockData);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        // Act
        var response = await client.PutAsync($"/api/uploads/{uploadId}/blocks/{blockId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
