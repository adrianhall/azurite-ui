using System.Net;
using System.Net.Http.Json;
using System.Text;
using AzuriteUI.Web.Services.Repositories.Models;

namespace AzuriteUI.Web.IntegrationTests.API;

[ExcludeFromCodeCoverage(Justification = "API Test class")]
public class UploadsController_CancelUpload_Tests(ServiceFixture fixture) : BaseApiTest(fixture)
{
    #region Basic DELETE Tests

    [Fact(Timeout = 60000)]
    public async Task CancelUpload_WithValidUploadId_ShouldReturnNoContent()
    {
        // Arrange        
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        var uploadId = await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 1024);

        // Act
        var response = await client.DeleteAsync($"/api/uploads/{uploadId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify upload session is removed
        var statusResponse = await client.GetAsync($"/api/uploads/{uploadId}");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Timeout = 60000)]
    public async Task CancelUpload_WithUploadedBlocks_ShouldRemoveSession()
    {
        // Arrange        
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        var uploadId = await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 1024);

        // Upload some blocks
        var blockId1 = Convert.ToBase64String(Encoding.UTF8.GetBytes("block1"));
        var blockId2 = Convert.ToBase64String(Encoding.UTF8.GetBytes("block2"));
        await UploadBlockAsync(client, uploadId, blockId1, 512);
        await UploadBlockAsync(client, uploadId, blockId2, 512);

        // Act
        var response = await client.DeleteAsync($"/api/uploads/{uploadId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify upload session is removed
        var statusResponse = await client.GetAsync($"/api/uploads/{uploadId}");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Verify blob was not created
        var blobExists = await Fixture.Azurite.BlobExistsAsync(containerName, "test-blob.txt");
        blobExists.Should().BeFalse();
    }

    [Fact(Timeout = 60000)]
    public async Task CancelUpload_BeforeAnyBlocksUploaded_ShouldSucceed()
    {
        // Arrange        
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        var uploadId = await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 1024);

        // Act - Cancel immediately without uploading any blocks
        var response = await client.DeleteAsync($"/api/uploads/{uploadId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify upload session is removed
        var statusResponse = await client.GetAsync($"/api/uploads/{uploadId}");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Timeout = 60000)]
    public async Task CancelUpload_MultipleUploads_ShouldOnlyCancelSpecifiedUpload()
    {
        // Arrange        
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        var uploadId1 = await CreateUploadSessionAsync(client, containerName, "blob1.txt", 1024);
        var uploadId2 = await CreateUploadSessionAsync(client, containerName, "blob2.txt", 1024);

        // Act - Cancel only the first upload
        var response = await client.DeleteAsync($"/api/uploads/{uploadId1}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify first upload is removed
        var status1Response = await client.GetAsync($"/api/uploads/{uploadId1}");
        status1Response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Verify second upload still exists
        var status2Response = await client.GetAsync($"/api/uploads/{uploadId2}");
        status2Response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Error Tests

    [Fact(Timeout = 60000)]
    public async Task CancelUpload_WithNonExistentUploadId_ShouldReturn204NoContent()
    {
        // Arrange        
        using HttpClient client = Fixture.CreateClient();
        var nonExistentUploadId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/uploads/{nonExistentUploadId}");

        // Assert
        response.Should().Be204NoContent();
    }

    [Fact(Timeout = 60000)]
    public async Task CancelUpload_WithInvalidGuidFormat_ShouldReturn404NotFound()
    {
        // Arrange        
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.DeleteAsync("/api/uploads/invalid-guid-format");

        // Assert
        response.Should().Be404NotFound();
    }

    [Fact(Timeout = 60000)]
    public async Task CancelUpload_AlreadyCancelled_ShouldReturn204NoContent()
    {
        // Arrange        
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        var uploadId = await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 1024);

        // Cancel once
        var response1 = await client.DeleteAsync($"/api/uploads/{uploadId}");
        response1.Should().Be204NoContent();

        // Act - Try to cancel again
        var response2 = await client.DeleteAsync($"/api/uploads/{uploadId}");
        response2.Should().Be204NoContent();
    }

    [Fact(Timeout = 60000)]
    public async Task CancelUpload_AfterCommit_ShouldReturn204NoContent()
    {
        // Arrange        
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        var uploadId = await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 512);

        // Upload and commit
        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block1"));
        await UploadBlockAsync(client, uploadId, blockId, 512);

        var commitDto = new CommitUploadRequestDTO
        {
            BlockIds = [blockId]
        };
        await client.PutAsJsonAsync($"/api/uploads/{uploadId}/commit", commitDto);

        // Act - Try to cancel after commit
        var response = await client.DeleteAsync($"/api/uploads/{uploadId}");

        // Assert
        response.Should().Be204NoContent();
    }

    #endregion

    #region Integration Tests

    [Fact(Timeout = 60000)]
    public async Task CancelUpload_ThenCreateNewUploadWithSameBlobName_ShouldSucceed()
    {
        // Arrange        
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        var uploadId1 = await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 1024);

        // Cancel the first upload
        var cancelResponse = await client.DeleteAsync($"/api/uploads/{uploadId1}");
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act - Create a new upload with the same blob name
        var uploadId2 = await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 1024);

        // Assert
        uploadId2.Should().NotBe(uploadId1);

        // Verify new upload exists
        var statusResponse = await client.GetAsync($"/api/uploads/{uploadId2}");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
