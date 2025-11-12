using System.Net;
using System.Net.Http.Json;
using AzuriteUI.Web.Controllers.Models;
using AzuriteUI.Web.Services.CacheDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AzuriteUI.Web.IntegrationTests.API;

[ExcludeFromCodeCoverage(Justification = "API Test class")]
public class DashboardController_GetDashboard_Tests(ServiceFixture fixture) : BaseApiTest(fixture)
{
    #region Basic Tests

    [Fact(Timeout = 60000)]
    public async Task GetDashboard_WithZeroContainersAndBlobs_ShouldReturnEmptyDashboard()
    {
        // Arrange
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/dashboard");
        var result = await response.Content.ReadFromJsonAsync<DashboardResponse>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Stats.Containers.Should().Be(0);
        result.Stats.Blobs.Should().Be(0);
        result.Stats.TotalBlobSize.Should().Be(0);
        result.Stats.TotalImageSize.Should().Be(0);
        result.RecentContainers.Should().BeEmpty();
        result.RecentBlobs.Should().BeEmpty();
    }

    [Fact(Timeout = 60000)]
    public async Task GetDashboard_WithFiveContainersAndBlobs_ShouldReturnAllItems()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            var containerName = await Fixture.Azurite.CreateContainerAsync($"container{i}");
            await Fixture.Azurite.CreateBlobAsync(containerName, $"blob{i}.txt", $"content{i}");
            // Small delay to ensure different timestamps
            await Task.Delay(100);
        }
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/dashboard");
        var result = await response.Content.ReadFromJsonAsync<DashboardResponse>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Stats.Containers.Should().Be(5);
        result.Stats.Blobs.Should().Be(5);
        result.Stats.TotalBlobSize.Should().BeGreaterThan(0);
        result.RecentContainers.Should().HaveCount(5);
        result.RecentBlobs.Should().HaveCount(5);
    }

    [Fact(Timeout = 60000)]
    public async Task GetDashboard_WithFifteenContainersAndBlobs_ShouldReturnOnlyTenMostRecentOfEach()
    {
        // Arrange
        for (int i = 0; i < 15; i++)
        {
            var containerName = await Fixture.Azurite.CreateContainerAsync($"container{i:D2}");
            await Fixture.Azurite.CreateBlobAsync(containerName, $"blob{i:D2}.txt", $"content{i}");
            // Small delay to ensure different timestamps
            await Task.Delay(100);
        }
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/dashboard");
        var result = await response.Content.ReadFromJsonAsync<DashboardResponse>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Stats.Containers.Should().Be(15, "stats should include all containers");
        result!.Stats.Blobs.Should().Be(15, "stats should include all blobs");
        result.Stats.TotalBlobSize.Should().BePositive();
        result.RecentContainers.Should().HaveCount(10, "should return only 10 most recent containers");
        result.RecentBlobs.Should().HaveCount(10, "should return only 10 most recent blobs");
    }

    #endregion

    #region Ordering Tests

    [Fact(Timeout = 60000)]
    public async Task GetDashboard_RecentBlobs_ShouldBeOrderedByLastModifiedDescending()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        for (int i = 0; i < 5; i++)
        {
            await Fixture.Azurite.CreateBlobAsync(containerName, $"blob{i}.txt", $"content{i}");
            await Task.Delay(100); // Ensure different timestamps
        }
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/dashboard");
        var result = await response.Content.ReadFromJsonAsync<DashboardResponse>(ServiceFixture.JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.RecentBlobs.Should().HaveCount(5);

        // Verify descending order
        var lastModifiedDates = result.RecentBlobs.Select(b => b.LastModified).ToList();
        lastModifiedDates.Should().BeInDescendingOrder("blobs should be ordered by LastModified descending");
    }

    [Fact(Timeout = 60000)]
    public async Task GetDashboard_RecentContainers_ShouldBeOrderedByLastModifiedDescending()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            await Fixture.Azurite.CreateContainerAsync($"container{i}");
            await Task.Delay(100); // Ensure different timestamps
        }
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/dashboard");
        var result = await response.Content.ReadFromJsonAsync<DashboardResponse>(ServiceFixture.JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.RecentContainers.Should().HaveCount(5);

        // Verify descending order
        var lastModifiedDates = result.RecentContainers.Select(c => c.LastModified).ToList();
        lastModifiedDates.Should().BeInDescendingOrder("containers should be ordered by LastModified descending");
    }

    #endregion

    #region Image Size Tests

    [Fact(Timeout = 60000)]
    public async Task GetDashboard_TotalImageSize_ShouldIncludeOnlyImageContentTypes()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");

        // Create image blobs
        await Fixture.Azurite.CreateBlobAsync(containerName, "image1.jpg", "fake-jpg-content", "image/jpeg");
        await Fixture.Azurite.CreateBlobAsync(containerName, "image2.png", "fake-png-content", "image/png");
        await Fixture.Azurite.CreateBlobAsync(containerName, "image3.gif", "fake-gif-content", "image/gif");

        // Create non-image blobs
        await Fixture.Azurite.CreateBlobAsync(containerName, "text.txt", "text-content", "text/plain");
        await Fixture.Azurite.CreateBlobAsync(containerName, "data.json", "json-content", "application/json");

        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/dashboard");
        var result = await response.Content.ReadFromJsonAsync<DashboardResponse>(ServiceFixture.JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.Stats.Blobs.Should().Be(5);
        result.Stats.TotalBlobSize.Should().BePositive();
        result.Stats.TotalImageSize.Should().BePositive()
            .And.BeLessThan(result.Stats.TotalBlobSize, "image size should be less than total blob size");

        // Calculate expected image size
        long expectedImageSize = "fake-jpg-content".Length + "fake-png-content".Length + "fake-gif-content".Length;
        result.Stats.TotalImageSize.Should().Be(expectedImageSize);
    }

    [Fact(Timeout = 60000)]
    public async Task GetDashboard_TotalImageSize_WithNoImages_ShouldBeZero()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.Azurite.CreateBlobAsync(containerName, "text.txt", "text-content", "text/plain");
        await Fixture.Azurite.CreateBlobAsync(containerName, "data.json", "json-content", "application/json");

        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/dashboard");
        var result = await response.Content.ReadFromJsonAsync<DashboardResponse>(ServiceFixture.JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.Stats.TotalImageSize.Should().Be(0);
        result.Stats.TotalBlobSize.Should().BePositive();
    }

    #endregion

    #region Container LastModified Tests

    [Fact(Timeout = 60000)]
    public async Task GetDashboard_ContainerLastModified_ShouldUseMaxOfContainerAndBlobLastModified()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        var containerCreatedTime = DateTimeOffset.UtcNow;

        // Add a delay and create a blob (should have a later timestamp)
        await Task.Delay(500);
        await Fixture.Azurite.CreateBlobAsync(containerName, "blob.txt", "content");

        await Fixture.SynchronizeCacheAsync();
        // Find the time of the container and blob LastModified
        DateTimeOffset? lastModified = null;
        using (var scope = Fixture.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<CacheDbContext>();
            var cInfo = await context.Containers.SingleAsync(c => c.Name == containerName);
            var bInfo = await context.Blobs.Where(b => b.ContainerName == containerName).MaxAsync(b => b.LastModified);
            lastModified = cInfo.LastModified > bInfo ? cInfo.LastModified : bInfo;
        }

        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/dashboard");
        var result = await response.Content.ReadFromJsonAsync<DashboardResponse>(ServiceFixture.JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.RecentContainers.Should().ContainSingle();

        var container = result.RecentContainers.First();
        container.Name.Should().Be(containerName);
        container.LastModified.Should().BeCloseTo(lastModified.Value, TimeSpan.FromSeconds(2));
        container.BlobCount.Should().Be(1);
    }

    [Fact(Timeout = 60000)]
    public async Task GetDashboard_ContainerWithoutBlobs_ShouldUseContainerLastModified()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("empty-container");
        var containerCreatedTime = DateTimeOffset.UtcNow;

        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/dashboard");
        var result = await response.Content.ReadFromJsonAsync<DashboardResponse>(ServiceFixture.JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.RecentContainers.Should().ContainSingle();

        var container = result.RecentContainers.First();
        container.Name.Should().Be(containerName);
        container.BlobCount.Should().Be(0);
        container.TotalSize.Should().Be(0);
        container.LastModified.Should().BeCloseTo(containerCreatedTime, TimeSpan.FromSeconds(5));
    }

    [Fact(Timeout = 60000)]
    public async Task GetDashboard_ContainerInfo_ShouldIncludeBlobCountAndTotalSize()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.Azurite.CreateBlobAsync(containerName, "blob1.txt", "content1");
        await Fixture.Azurite.CreateBlobAsync(containerName, "blob2.txt", "content2");
        await Fixture.Azurite.CreateBlobAsync(containerName, "blob3.txt", "content3");

        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/dashboard");
        var result = await response.Content.ReadFromJsonAsync<DashboardResponse>(ServiceFixture.JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.RecentContainers.Should().ContainSingle();

        var container = result.RecentContainers.First();
        container.Name.Should().Be(containerName);
        container.BlobCount.Should().Be(3);
        container.TotalSize.Should().BePositive();

        long expectedSize = "content1".Length + "content2".Length + "content3".Length;
        container.TotalSize.Should().Be(expectedSize);
    }

    #endregion
}
