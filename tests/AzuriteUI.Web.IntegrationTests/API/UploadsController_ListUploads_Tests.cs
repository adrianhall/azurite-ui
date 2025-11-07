using System.Net;
using System.Net.Http.Json;
using System.Text;
using AzuriteUI.Web.Controllers.Models;
using AzuriteUI.Web.Services.Repositories.Models;

namespace AzuriteUI.Web.IntegrationTests.API;

[ExcludeFromCodeCoverage(Justification = "API Test class")]
public class UploadsController_ListUploads_Tests(ServiceFixture fixture) : BaseApiTest(fixture)
{   
        #region Basic GET Tests

    [Fact(Timeout = 60000)]
    public async Task ListUploads_WithNoUploads_ShouldReturnEmptyList()
    {
        // Arrange
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/uploads");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<UploadDTO>>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.FilteredCount.Should().Be(0);
    }

    [Fact(Timeout = 60000)]
    public async Task ListUploads_WithMultipleUploads_ShouldReturnAllUploads()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        var uploadId1 = await CreateUploadSessionAsync(client, containerName, "blob1.txt", 1024);
        var uploadId2 = await CreateUploadSessionAsync(client, containerName, "blob2.txt", 2048);
        var uploadId3 = await CreateUploadSessionAsync(client, containerName, "blob3.txt", 512);

        // Act
        var response = await client.GetAsync("/api/uploads");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<UploadDTO>>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.FilteredCount.Should().Be(3);

        var uploadIds = result.Items.Select(u => u.Id).ToList();
        uploadIds.Should().Contain(uploadId1);
        uploadIds.Should().Contain(uploadId2);
        uploadIds.Should().Contain(uploadId3);
    }

    [Fact(Timeout = 60000)]
    public async Task ListUploads_WithProgressTracking_ShouldShowProgress()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        var uploadId = await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 1000);

        // Upload some blocks to create progress
        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block1"));
        await UploadBlockAsync(client, uploadId, blockId, 500);

        // Act
        var response = await client.GetAsync("/api/uploads");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<UploadDTO>>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().ContainSingle();

        var upload = result.Items.First();
        upload.Id.Should().Be(uploadId);
        upload.Name.Should().Be("test-blob.txt");
        upload.ContainerName.Should().Be(containerName);
        upload.Progress.Should().BeInRange(0, 100);
    }

    #endregion

    #region OData Query Tests

    [Fact(Timeout = 60000)]
    public async Task ListUploads_WithFilterByContainerName_ShouldReturnMatchingUploads()
    {
        // Arrange
        var container1 = await Fixture.Azurite.CreateContainerAsync("container1");
        var container2 = await Fixture.Azurite.CreateContainerAsync("container2");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        await CreateUploadSessionAsync(client, container1, "blob1.txt", 1024);
        await CreateUploadSessionAsync(client, container2, "blob2.txt", 1024);
        await CreateUploadSessionAsync(client, container1, "blob3.txt", 1024);

        // Act
        var response = await client.GetAsync($"/api/uploads?$filter=containerName eq '{container1}'");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<UploadDTO>>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(u => u.ContainerName == container1);
    }

    [Fact(Timeout = 60000)]
    public async Task ListUploads_WithTopParameter_ShouldLimitResults()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        await CreateUploadSessionAsync(client, containerName, "blob1.txt", 1024);
        await CreateUploadSessionAsync(client, containerName, "blob2.txt", 1024);
        await CreateUploadSessionAsync(client, containerName, "blob3.txt", 1024);
        await CreateUploadSessionAsync(client, containerName, "blob4.txt", 1024);

        // Act
        var response = await client.GetAsync("/api/uploads?$top=2");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<UploadDTO>>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(4);
        result.NextLink.Should().NotBeNullOrEmpty();
    }

    [Fact(Timeout = 60000)]
    public async Task ListUploads_WithSkipParameter_ShouldSkipResults()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        await CreateUploadSessionAsync(client, containerName, "blob1.txt", 1024);
        await CreateUploadSessionAsync(client, containerName, "blob2.txt", 1024);
        await CreateUploadSessionAsync(client, containerName, "blob3.txt", 1024);

        // Act
        var response = await client.GetAsync("/api/uploads?$skip=1");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<UploadDTO>>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(3);
    }

    [Fact(Timeout = 60000)]
    public async Task ListUploads_WithOrderByName_ShouldReturnOrderedResults()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        await CreateUploadSessionAsync(client, containerName, "charlie.txt", 1024);
        await CreateUploadSessionAsync(client, containerName, "alpha.txt", 1024);
        await CreateUploadSessionAsync(client, containerName, "bravo.txt", 1024);

        // Act
        var response = await client.GetAsync("/api/uploads?$orderby=name asc");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<UploadDTO>>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.Items.First().Name.Should().Be("alpha.txt");
        result.Items.Last().Name.Should().Be("charlie.txt");
    }

    [Fact(Timeout = 60000)]
    public async Task ListUploads_WithSelectParameter_ShouldReturnOnlySelectedFields()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        await CreateUploadSessionAsync(client, containerName, "test-blob.txt", 1024);

        // Act
        var response = await client.GetAsync("/api/uploads?$select=id,name");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<object>>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
    }

    [Fact(Timeout = 60000)]
    public async Task ListUploads_WithInvalidFilter_ShouldReturnBadRequest()
    {
        // Arrange
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/uploads?$filter=invalid syntax here");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Pagination Tests

    [Fact(Timeout = 60000)]
    public async Task ListUploads_WithPagination_ShouldProvideNextAndPrevLinks()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Create enough uploads to require pagination
        for (int i = 0; i < 30; i++)
        {
            await CreateUploadSessionAsync(client, containerName, $"blob{i:D3}.txt", 1024);
        }

        // Act - Get first page
        var response1 = await client.GetAsync("/api/uploads?$top=10");
        var result1 = await response1.Content.ReadFromJsonAsync<PagedResponse<UploadDTO>>(ServiceFixture.JsonOptions);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        result1.Should().NotBeNull();
        result1!.Items.Should().HaveCount(10);
        result1.TotalCount.Should().Be(30);
        result1.NextLink.Should().NotBeNullOrEmpty();
        result1.PrevLink.Should().BeNull();

        // Act - Get second page
        var response2 = await client.GetAsync($"/api/uploads?{result1.NextLink}");
        var result2 = await response2.Content.ReadFromJsonAsync<PagedResponse<UploadDTO>>(ServiceFixture.JsonOptions);

        // Assert
        result2.Should().NotBeNull();
        result2!.Items.Should().HaveCount(10);
        result2.NextLink.Should().NotBeNullOrEmpty();
        result2.PrevLink.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Integration Tests

    [Fact(Timeout = 60000)]
    public async Task ListUploads_AfterCancellingUpload_ShouldNotShowCancelledUpload()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        var uploadId1 = await CreateUploadSessionAsync(client, containerName, "blob1.txt", 1024);
        var uploadId2 = await CreateUploadSessionAsync(client, containerName, "blob2.txt", 1024);

        // Cancel first upload
        await client.DeleteAsync($"/api/uploads/{uploadId1}");

        // Act
        var response = await client.GetAsync("/api/uploads");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<UploadDTO>>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.First().Id.Should().Be(uploadId2);
    }

    [Fact(Timeout = 60000)]
    public async Task ListUploads_AfterCommittingUpload_ShouldNotShowCommittedUpload()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        var uploadId1 = await CreateUploadSessionAsync(client, containerName, "blob1.txt", 512);
        var uploadId2 = await CreateUploadSessionAsync(client, containerName, "blob2.txt", 1024);

        // Commit first upload
        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block1"));
        await UploadBlockAsync(client, uploadId1, blockId, 512);
        var commitDto = new CommitUploadRequestDTO
        {
            BlockIds = [blockId]
        };
        await client.PutAsJsonAsync($"/api/uploads/{uploadId1}/commit", commitDto);

        // Act
        var response = await client.GetAsync("/api/uploads");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<UploadDTO>>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().ContainSingle(x => x.Id == uploadId2);
    }

    #endregion
}
