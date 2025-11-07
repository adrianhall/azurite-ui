using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AzuriteUI.Web.Controllers.Models;

namespace AzuriteUI.Web.IntegrationTests.API;

[ExcludeFromCodeCoverage(Justification = "API Test class")]
public class StorageController_ListContainers_Tests(ServiceFixture fixture) : BaseApiTest(fixture)
{
    #region Basic GET Tests

    [Fact(Timeout = 60000)]
    public async Task ListContainers_WithNoContainers_ShouldReturnEmptyList()
    {
        // Arrange
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/containers");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>(ServiceFixture.JsonOptions);

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
    public async Task ListContainers_WithMultipleContainers_ShouldReturnAllContainers()
    {
        // Arrange
        await Fixture.Azurite.CreateContainerAsync("container1");
        await Fixture.Azurite.CreateContainerAsync("container2");
        await Fixture.Azurite.CreateContainerAsync("container3");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/containers");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.FilteredCount.Should().Be(3);
        result.Items.Should().Contain(c => c.GetProperty("name").GetString() == "container1");
        result.Items.Should().Contain(c => c.GetProperty("name").GetString() == "container2");
        result.Items.Should().Contain(c => c.GetProperty("name").GetString() == "container3");
    }

    #endregion

    #region Pagination Tests

    [Fact(Timeout = 60000)]
    public async Task ListContainers_WithTopParameter_ShouldLimitResults()
    {
        // Arrange
        for (int i = 1; i <= 10; i++)
        {
            await Fixture.Azurite.CreateContainerAsync($"container{i:D2}");
        }
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/containers?$top=5");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>(ServiceFixture.JsonOptions);

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
    public async Task ListContainers_WithSkipAndTop_ShouldReturnCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 10; i++)
        {
            await Fixture.Azurite.CreateContainerAsync($"container{i:D2}");
        }
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/containers?$skip=3&$top=4");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>(ServiceFixture.JsonOptions);

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
    public async Task ListContainers_WithSkipBeyondResults_ShouldReturnEmptyList()
    {
        // Arrange
        await Fixture.Azurite.CreateContainerAsync("container1");
        await Fixture.Azurite.CreateContainerAsync("container2");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/containers?$skip=10");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>(ServiceFixture.JsonOptions);

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
    public async Task ListContainers_OnLastPage_ShouldNotHaveNextLink()
    {
        // Arrange
        for (int i = 1; i <= 10; i++)
        {
            await Fixture.Azurite.CreateContainerAsync($"container{i:D2}");
        }
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/containers?$skip=8&$top=5");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>(ServiceFixture.JsonOptions);

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
    public async Task ListContainers_WithFilterByName_ShouldReturnMatchingContainers()
    {
        // Arrange
        await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.Azurite.CreateContainerAsync("prod-container");
        await Fixture.Azurite.CreateContainerAsync("test-data");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/containers?$filter=startswith(name,'test')");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(3);
        result.FilteredCount.Should().Be(2);
        result.Items.Should().OnlyContain(c => c.GetProperty("name").GetString()!.StartsWith("test"));
    }

    [Fact(Timeout = 60000)]
    public async Task ListContainers_WithFilterByBlobCount_ShouldReturnMatchingContainers()
    {
        // Arrange
        await Fixture.Azurite.CreateContainerAsync("empty-container");
        var containerWithBlobs = await Fixture.Azurite.CreateContainerAsync("container-with-blobs");
        await Fixture.Azurite.CreateBlobAsync(containerWithBlobs, "blob1.txt", "content1");
        await Fixture.Azurite.CreateBlobAsync(containerWithBlobs, "blob2.txt", "content2");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/containers?$filter=blobCount gt 0");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.FilteredCount.Should().Be(1);
        result.Items.First().GetProperty("name").GetString().Should().Be("container-with-blobs");
    }

    [Fact(Timeout = 60000)]
    public async Task ListContainers_WithComplexFilter_ShouldReturnMatchingContainers()
    {
        // Arrange
        await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.Azurite.CreateContainerAsync("prod-container");
        var testData = await Fixture.Azurite.CreateContainerAsync("test-data");
        await Fixture.Azurite.CreateBlobAsync(testData, "file.txt", "content");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/containers?$filter=startswith(name,'test') and blobCount gt 0");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.First().GetProperty("name").GetString().Should().Be("test-data");
    }

    #endregion

    #region Ordering Tests

    [Fact(Timeout = 60000)]
    public async Task ListContainers_WithOrderByName_ShouldReturnOrderedResults()
    {
        // Arrange
        await Fixture.Azurite.CreateContainerAsync("zebra");
        await Fixture.Azurite.CreateContainerAsync("alpha");
        await Fixture.Azurite.CreateContainerAsync("beta");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/containers?$orderby=name");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        var names = result!.Items.Select(c => c.GetProperty("name").GetString()).ToList();
        names.Should().BeInAscendingOrder();
        names[0].Should().Be("alpha");
        names[1].Should().Be("beta");
        names[2].Should().Be("zebra");
    }

    [Fact(Timeout = 60000)]
    public async Task ListContainers_WithOrderByNameDescending_ShouldReturnDescendingResults()
    {
        // Arrange
        await Fixture.Azurite.CreateContainerAsync("zebra");
        await Fixture.Azurite.CreateContainerAsync("alpha");
        await Fixture.Azurite.CreateContainerAsync("beta");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/containers?$orderby=name desc");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        var names = result!.Items.Select(c => c.GetProperty("name").GetString()).ToList();
        names.Should().BeInDescendingOrder();
        names[0].Should().Be("zebra");
        names[1].Should().Be("beta");
        names[2].Should().Be("alpha");
    }

    [Fact(Timeout = 60000)]
    public async Task ListContainers_WithOrderByBlobCount_ShouldReturnOrderedByCount()
    {
        // Arrange
        var container1 = await Fixture.Azurite.CreateContainerAsync("container1");
        var container2 = await Fixture.Azurite.CreateContainerAsync("container2");
        var container3 = await Fixture.Azurite.CreateContainerAsync("container3");
        await Fixture.Azurite.CreateBlobAsync(container2, "blob1.txt", "content1");
        await Fixture.Azurite.CreateBlobAsync(container2, "blob2.txt", "content2");
        await Fixture.Azurite.CreateBlobAsync(container3, "blob1.txt", "content1");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/containers?$orderby=blobCount desc");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        var items = result!.Items.ToList();
        items[0].GetProperty("blobCount").GetInt32().Should().Be(2);
        items[1].GetProperty("blobCount").GetInt32().Should().Be(1);
        items[2].GetProperty("blobCount").GetInt32().Should().Be(0);
    }

    #endregion

    #region Selection Tests

    [Fact(Timeout = 60000)]
    public async Task ListContainers_WithSelectName_ShouldReturnOnlyNameProperty()
    {
        // Arrange
        await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/containers?$select=name");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        var item = result.Items.First();
        item.TryGetProperty("name", out _).Should().BeTrue();
        item.TryGetProperty("eTag", out _).Should().BeFalse();
        item.TryGetProperty("lastModified", out _).Should().BeFalse();
    }

    [Fact(Timeout = 60000)]
    public async Task ListContainers_WithSelectMultipleProperties_ShouldReturnOnlySelectedProperties()
    {
        // Arrange
        await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/containers?$select=name,blobCount");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        var item = result!.Items.First();
        item.TryGetProperty("name", out _).Should().BeTrue();
        item.TryGetProperty("blobCount", out _).Should().BeTrue();
        item.TryGetProperty("eTag", out _).Should().BeFalse();
        item.TryGetProperty("lastModified", out _).Should().BeFalse();
    }

    #endregion

    #region Validation and Error Tests

    [Fact(Timeout = 60000)]
    public async Task ListContainers_WithInvalidFilter_ShouldReturnBadRequest()
    {
        // Arrange
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/containers?$filter=invalid syntax here");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Timeout = 60000)]
    public async Task ListContainers_WithTopTooLarge_ShouldReturnBadRequest()
    {
        // Arrange
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/containers?$top=1000");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Timeout = 60000)]
    public async Task ListContainers_WithInvalidOrderBy_ShouldReturnBadRequest()
    {
        // Arrange
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/containers?$orderby=nonExistentProperty");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Combined Query Tests

    [Fact(Timeout = 60000)]
    public async Task ListContainers_WithFilterOrderAndPagination_ShouldApplyAllOptions()
    {
        // Arrange
        await Fixture.Azurite.CreateContainerAsync("test-alpha");
        await Fixture.Azurite.CreateContainerAsync("test-beta");
        await Fixture.Azurite.CreateContainerAsync("test-gamma");
        await Fixture.Azurite.CreateContainerAsync("prod-alpha");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/containers?$filter=startswith(name,'test')&$orderby=name&$skip=1&$top=1");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(4);
        result.FilteredCount.Should().Be(3);
        result.Items.First().GetProperty("name").GetString().Should().Be("test-beta");
        result.NextLink.Should().NotBeNull();
        result.PrevLink.Should().NotBeNull();
    }

    [Fact(Timeout = 60000)]
    public async Task ListContainers_WithSelectFilterAndOrder_ShouldApplyAllOptions()
    {
        // Arrange
        await Fixture.Azurite.CreateContainerAsync("zebra");
        await Fixture.Azurite.CreateContainerAsync("alpha");
        await Fixture.Azurite.CreateContainerAsync("beta");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/containers?$select=name&$orderby=name desc&$top=2");
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        var names = result.Items.Select(c => c.GetProperty("name").GetString()).ToList();
        names[0].Should().Be("zebra");
        names[1].Should().Be("beta");
        result.Items.First().TryGetProperty("eTag", out _).Should().BeFalse();
    }

    #endregion
}
