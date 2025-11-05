using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AzuriteUI.Web.Controllers.Models;
using AzuriteUI.Web.IntegrationTests.Helpers;
using AzuriteUI.Web.Services.CacheSync;
using Microsoft.Extensions.DependencyInjection;

namespace AzuriteUI.Web.IntegrationTests.API;

[ExcludeFromCodeCoverage(Justification = "API Test class")]
public class StorageController_ListBlobs_Tests(ServiceFixture fixture) : IClassFixture<ServiceFixture>
{
    #region Basic GET Tests

    [Fact(Timeout = 60000)]
    public async Task ListBlobs_WithNoBlobs_ShouldReturnEmptyList()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.FilteredCount.Should().Be(0);
        result.NextLink.Should().BeNull();
        result.PrevLink.Should().BeNull();
    }

    [Fact(Timeout = 60000)]
    public async Task ListBlobs_WithMultipleBlobs_ShouldReturnAllBlobs()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.Azurite.CreateBlobAsync(containerName, "blob1.txt", "content1");
        await fixture.Azurite.CreateBlobAsync(containerName, "blob2.txt", "content2");
        await fixture.Azurite.CreateBlobAsync(containerName, "blob3.txt", "content3");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.FilteredCount.Should().Be(3);
        result.Items.Should().Contain(b => b.GetProperty("name").GetString() == "blob1.txt");
        result.Items.Should().Contain(b => b.GetProperty("name").GetString() == "blob2.txt");
        result.Items.Should().Contain(b => b.GetProperty("name").GetString() == "blob3.txt");
    }

    [Fact(Timeout = 60000)]
    public async Task ListBlobs_WithEmptyContainer_ShouldReturnEmptyList()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("empty-container");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.FilteredCount.Should().Be(0);
    }

    [Fact(Timeout = 60000)]
    public async Task ListBlobs_WithNonExistentContainer_ShouldReturnEmptyList()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/containers/non-existent-container/blobs");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.FilteredCount.Should().Be(0);
    }

    #endregion

    #region Pagination Tests

    [Fact(Timeout = 60000)]
    public async Task ListBlobs_WithTopParameter_ShouldLimitResults()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        for (int i = 1; i <= 10; i++)
        {
            await fixture.Azurite.CreateBlobAsync(containerName, $"blob{i:D2}.txt", $"content{i}");
        }
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs?$top=5");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(10);
        result.FilteredCount.Should().Be(10);
        result.NextLink.Should().NotBeNull();
        result.NextLink.Should().Contain("$skip=5");
    }

    [Fact(Timeout = 60000)]
    public async Task ListBlobs_WithSkipAndTop_ShouldReturnCorrectPage()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        for (int i = 1; i <= 10; i++)
        {
            await fixture.Azurite.CreateBlobAsync(containerName, $"blob{i:D2}.txt", $"content{i}");
        }
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs?$skip=3&$top=4");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(4);
        result.TotalCount.Should().Be(10);
        result.FilteredCount.Should().Be(10);
        result.NextLink.Should().NotBeNull();
        result.PrevLink.Should().NotBeNull();
    }

    [Fact(Timeout = 60000)]
    public async Task ListBlobs_WithSkipBeyondResults_ShouldReturnEmptyList()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.Azurite.CreateBlobAsync(containerName, "blob1.txt", "content1");
        await fixture.Azurite.CreateBlobAsync(containerName, "blob2.txt", "content2");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs?$skip=10");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(2);
        result.FilteredCount.Should().Be(2);
        result.NextLink.Should().BeNull();
        result.PrevLink.Should().NotBeNull();
    }

    [Fact(Timeout = 60000)]
    public async Task ListBlobs_OnLastPage_ShouldNotHaveNextLink()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        for (int i = 1; i <= 10; i++)
        {
            await fixture.Azurite.CreateBlobAsync(containerName, $"blob{i:D2}.txt", $"content{i}");
        }
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs?$skip=8&$top=5");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.NextLink.Should().BeNull();
        result.PrevLink.Should().NotBeNull();
    }

    #endregion

    #region Filtering Tests

    [Fact(Timeout = 60000)]
    public async Task ListBlobs_WithFilterByName_ShouldReturnMatchingBlobs()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.Azurite.CreateBlobAsync(containerName, "test-file.txt", "content1");
        await fixture.Azurite.CreateBlobAsync(containerName, "prod-file.txt", "content2");
        await fixture.Azurite.CreateBlobAsync(containerName, "test-data.txt", "content3");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs?$filter=startswith(name,'test')");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(3);
        result.FilteredCount.Should().Be(2);
        result.Items.Should().OnlyContain(b => b.GetProperty("name").GetString()!.StartsWith("test"));
    }

    [Fact(Timeout = 60000)]
    public async Task ListBlobs_WithFilterByContentType_ShouldReturnMatchingBlobs()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.Azurite.CreateBlobAsync(containerName, "file1.txt", "content1");
        await fixture.Azurite.CreateBlobAsync(containerName, "file2.txt", "content2");
        await fixture.Azurite.CreateBlobAsync(containerName, "file3.txt", "content3");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs?$filter=contentType eq 'text/plain'");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.FilteredCount.Should().Be(3);
        result.Items.Should().OnlyContain(b => b.GetProperty("contentType").GetString() == "text/plain");
    }

    [Fact(Timeout = 60000)]
    public async Task ListBlobs_WithComplexFilter_ShouldReturnMatchingBlobs()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.Azurite.CreateBlobAsync(containerName, "test-small.txt", "hi");
        await fixture.Azurite.CreateBlobAsync(containerName, "test-large.txt", "this is a longer content string");
        await fixture.Azurite.CreateBlobAsync(containerName, "prod-small.txt", "hi");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs?$filter=startswith(name,'test') and contentLength gt 5");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.First().GetProperty("name").GetString().Should().Be("test-large.txt");
    }

    #endregion

    #region Ordering Tests

    [Fact(Timeout = 60000)]
    public async Task ListBlobs_WithOrderByName_ShouldReturnOrderedResults()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.Azurite.CreateBlobAsync(containerName, "zebra.txt", "content1");
        await fixture.Azurite.CreateBlobAsync(containerName, "alpha.txt", "content2");
        await fixture.Azurite.CreateBlobAsync(containerName, "beta.txt", "content3");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs?$orderby=name");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        var names = result!.Items.Select(b => b.GetProperty("name").GetString()).ToList();
        names.Should().BeInAscendingOrder();
        names[0].Should().Be("alpha.txt");
        names[1].Should().Be("beta.txt");
        names[2].Should().Be("zebra.txt");
    }

    [Fact(Timeout = 60000)]
    public async Task ListBlobs_WithOrderByNameDescending_ShouldReturnDescendingResults()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.Azurite.CreateBlobAsync(containerName, "zebra.txt", "content1");
        await fixture.Azurite.CreateBlobAsync(containerName, "alpha.txt", "content2");
        await fixture.Azurite.CreateBlobAsync(containerName, "beta.txt", "content3");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs?$orderby=name desc");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        var names = result!.Items.Select(b => b.GetProperty("name").GetString()).ToList();
        names.Should().BeInDescendingOrder();
        names[0].Should().Be("zebra.txt");
        names[1].Should().Be("beta.txt");
        names[2].Should().Be("alpha.txt");
    }

    [Fact(Timeout = 60000)]
    public async Task ListBlobs_WithOrderByContentLength_ShouldReturnOrderedBySize()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.Azurite.CreateBlobAsync(containerName, "small.txt", "hi");
        await fixture.Azurite.CreateBlobAsync(containerName, "large.txt", "this is a much longer content string");
        await fixture.Azurite.CreateBlobAsync(containerName, "medium.txt", "medium content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs?$orderby=contentLength desc");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        var items = result!.Items.ToList();
        items[0].GetProperty("name").GetString().Should().Be("large.txt");
        items[1].GetProperty("name").GetString().Should().Be("medium.txt");
        items[2].GetProperty("name").GetString().Should().Be("small.txt");
    }

    #endregion

    #region Selection Tests

    [Fact(Timeout = 60000)]
    public async Task ListBlobs_WithSelectName_ShouldReturnOnlyNameProperty()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs?$select=name");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        var item = result.Items.First();
        item.TryGetProperty("name", out _).Should().BeTrue();
        item.TryGetProperty("eTag", out _).Should().BeFalse();
        item.TryGetProperty("lastModified", out _).Should().BeFalse();
        item.TryGetProperty("contentType", out _).Should().BeFalse();
    }

    [Fact(Timeout = 60000)]
    public async Task ListBlobs_WithSelectMultipleProperties_ShouldReturnOnlySelectedProperties()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "content");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs?$select=name,contentLength");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        var item = result!.Items.First();
        item.TryGetProperty("name", out _).Should().BeTrue();
        item.TryGetProperty("contentLength", out _).Should().BeTrue();
        item.TryGetProperty("eTag", out _).Should().BeFalse();
        item.TryGetProperty("lastModified", out _).Should().BeFalse();
    }

    #endregion

    #region Validation and Error Tests

    [Fact(Timeout = 60000)]
    public async Task ListBlobs_WithInvalidFilter_ShouldReturnBadRequest()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs?$filter=invalid syntax here");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Timeout = 60000)]
    public async Task ListBlobs_WithTopTooLarge_ShouldReturnBadRequest()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs?$top=1000");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Timeout = 60000)]
    public async Task ListBlobs_WithInvalidOrderBy_ShouldReturnBadRequest()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs?$orderby=nonExistentProperty");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Combined Query Tests

    [Fact(Timeout = 60000)]
    public async Task ListBlobs_WithFilterOrderAndPagination_ShouldApplyAllOptions()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.Azurite.CreateBlobAsync(containerName, "test-alpha.txt", "content1");
        await fixture.Azurite.CreateBlobAsync(containerName, "test-beta.txt", "content2");
        await fixture.Azurite.CreateBlobAsync(containerName, "test-gamma.txt", "content3");
        await fixture.Azurite.CreateBlobAsync(containerName, "prod-alpha.txt", "content4");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs?$filter=startswith(name,'test')&$orderby=name&$skip=1&$top=1");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(4);
        result.FilteredCount.Should().Be(3);
        result.Items.First().GetProperty("name").GetString().Should().Be("test-beta.txt");
        result.NextLink.Should().NotBeNull();
        result.PrevLink.Should().NotBeNull();
    }

    [Fact(Timeout = 60000)]
    public async Task ListBlobs_WithSelectFilterAndOrder_ShouldApplyAllOptions()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.Azurite.CreateBlobAsync(containerName, "zebra.txt", "content1");
        await fixture.Azurite.CreateBlobAsync(containerName, "alpha.txt", "content2");
        await fixture.Azurite.CreateBlobAsync(containerName, "beta.txt", "content3");
        await SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs?$select=name&$orderby=name desc&$top=2");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        var names = result.Items.Select(b => b.GetProperty("name").GetString()).ToList();
        names[0].Should().Be("zebra.txt");
        names[1].Should().Be("beta.txt");
        result.Items.First().TryGetProperty("eTag", out _).Should().BeFalse();
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

    #endregion
}
