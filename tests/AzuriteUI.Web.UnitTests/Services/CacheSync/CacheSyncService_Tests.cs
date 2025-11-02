using AzuriteUI.Web.Services.Azurite;
using AzuriteUI.Web.Services.Azurite.Models;
using AzuriteUI.Web.Services.CacheDb.Models;
using AzuriteUI.Web.Services.CacheSync;
using AzuriteUI.Web.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;

namespace AzuriteUI.Web.UnitTests.Services.CacheSync;

[ExcludeFromCodeCoverage]
public class CacheSyncService_Tests : SqliteDbTests
{
    private readonly IAzuriteService _service = Substitute.For<IAzuriteService>();
    private readonly FakeLogger<CacheSyncService> _logger = new();

    #region SynchronizeCacheAsync Tests

    [Fact(Timeout = 15000)]
    public async Task SynchronizeCacheAsync_WithNoContainers_ShouldCompleteSuccessfully()
    {
        // Arrange
        using var context = CreateDbContext();
        _service.GetContainersAsync(Arg.Any<CancellationToken>()).Returns(Utils.CreateAsyncEnumerable<AzuriteContainerItem>([]));
        var cacheSyncService = new CacheSyncService(context, _service, _logger);

        // Act
        await cacheSyncService.SynchronizeCacheAsync();

        // Assert
        var containers = await context.Containers.ToListAsync();
        containers.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task SynchronizeCacheAsync_WithSingleContainer_ShouldStoreContainer()
    {
        // Arrange
        using var context = CreateDbContext();
        var container = CreateContainerItem("container1");
        _service.GetContainersAsync(Arg.Any<CancellationToken>()).Returns(Utils.CreateAsyncEnumerable([container]));
        _service.GetBlobsAsync("container1", Arg.Any<CancellationToken>()).Returns(Utils.CreateAsyncEnumerable<AzuriteBlobItem>([]));
        var cacheSyncService = new CacheSyncService(context, _service, _logger);

        // Act
        await cacheSyncService.SynchronizeCacheAsync();

        // Assert
        var containers = await context.Containers.ToListAsync();
        containers.Should().ContainSingle(x => x.Name == "container1");
    }

    [Fact(Timeout = 15000)]
    public async Task SynchronizeCacheAsync_WithContainerAndBlobs_ShouldStoreBothContainerAndBlobs()
    {
        // Arrange
        using var context = CreateDbContext();
        var container = CreateContainerItem("container1");
        _service.GetContainersAsync(Arg.Any<CancellationToken>()).Returns(Utils.CreateAsyncEnumerable([container]));
        var blob1 = CreateBlobItem("blob1.txt", contentLength: 1024);
        var blob2 = CreateBlobItem("blob2.txt", contentLength: 2048);
        _service.GetBlobsAsync("container1", Arg.Any<CancellationToken>()).Returns(Utils.CreateAsyncEnumerable([blob1, blob2]));
        var cacheSyncService = new CacheSyncService(context, _service, _logger);

        // Act
        await cacheSyncService.SynchronizeCacheAsync();

        // Assert
        var containers = await context.Containers.ToListAsync();
        containers.Should().ContainSingle();
        containers[0].BlobCount.Should().Be(2);
        containers[0].TotalSize.Should().Be(3072);

        var blobs = await context.Blobs.ToListAsync();
        blobs.Should().HaveCount(2)
            .And.Contain(b => b.Name == "blob1.txt")
            .And.Contain(b => b.Name == "blob2.txt");
    }

    [Fact(Timeout = 15000)]
    public async Task SynchronizeCacheAsync_WithMultipleContainers_ShouldStoreAllContainers()
    {
        // Arrange
        using var context = CreateDbContext();
        var container1 = CreateContainerItem("container1");
        var container2 = CreateContainerItem("container2");
        _service.GetContainersAsync(Arg.Any<CancellationToken>()).Returns(Utils.CreateAsyncEnumerable([container1, container2]));
        _service.GetBlobsAsync("container1", Arg.Any<CancellationToken>()).Returns(Utils.CreateAsyncEnumerable<AzuriteBlobItem>([]));
        _service.GetBlobsAsync("container2", Arg.Any<CancellationToken>()).Returns(Utils.CreateAsyncEnumerable<AzuriteBlobItem>([]));
        var cacheSyncService = new CacheSyncService(context, _service, _logger);

        // Act
        await cacheSyncService.SynchronizeCacheAsync();

        // Assert
        var containers = await context.Containers.ToListAsync();
        containers.Should().HaveCount(2)
            .And.Contain(c => c.Name == "container1")
            .And.Contain(c => c.Name == "container2");
    }

    [Fact(Timeout = 15000)]
    public async Task SynchronizeCacheAsync_OnSecondRun_ShouldRemoveOldEntries()
    {
        // Arrange
        using var context = CreateDbContext();

        // First sync with container1
        var container1 = CreateContainerItem("container1");
        _service.GetContainersAsync(Arg.Any<CancellationToken>()).Returns(Utils.CreateAsyncEnumerable([container1]));
        _service.GetBlobsAsync("container1", Arg.Any<CancellationToken>()).Returns(Utils.CreateAsyncEnumerable<AzuriteBlobItem>([]));

        var cacheSyncService = new CacheSyncService(context, _service, _logger);
        await cacheSyncService.SynchronizeCacheAsync();

        // Second sync with container2
        var container2 = CreateContainerItem("container2");
        _service.GetContainersAsync(Arg.Any<CancellationToken>()).Returns(Utils.CreateAsyncEnumerable([container2]));
        _service.GetBlobsAsync("container2", Arg.Any<CancellationToken>()).Returns(Utils.CreateAsyncEnumerable<AzuriteBlobItem>([]));

        // Act
        await cacheSyncService.SynchronizeCacheAsync();

        // Assert
        var containers = await context.Containers.ToListAsync();
        containers.Should().ContainSingle(x => x.Name == "container2");
    }

    [Fact(Timeout = 15000)]
    public async Task SynchronizeCacheAsync_WithCancellationToken_ShouldPassTokenToService()
    {
        // Arrange
        using var context = CreateDbContext();
        var cts = new CancellationTokenSource();
        _service.GetContainersAsync(Arg.Any<CancellationToken>()).Returns(Utils.CreateAsyncEnumerable<AzuriteContainerItem>([]));
        var cacheSyncService = new CacheSyncService(context, _service, _logger);

        // Act
        await cacheSyncService.SynchronizeCacheAsync(cts.Token);

        // Assert
        _service.Received(1).GetContainersAsync(cts.Token);
    }

    [Fact(Timeout = 15000)]
    public async Task SynchronizeCacheAsync_WhenGetContainersThrows_ShouldThrowException()
    {
        // Arrange
        using var context = CreateDbContext();
        _service.GetContainersAsync(Arg.Any<CancellationToken>())
            .Returns(Utils.CreateAsyncEnumerableWithException<AzuriteContainerItem>(new InvalidOperationException("Test exception")));
        var cacheSyncService = new CacheSyncService(context, _service, _logger);

        // Act
        Func<Task> act = async () => await cacheSyncService.SynchronizeCacheAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact(Timeout = 15000)]
    public async Task SynchronizeCacheAsync_WhenGetBlobsThrows_ShouldThrowException()
    {
        // Arrange
        using var context = CreateDbContext();
            var container = CreateContainerItem("container1");
        _service.GetContainersAsync(Arg.Any<CancellationToken>()).Returns(Utils.CreateAsyncEnumerable([container]));
        _service.GetBlobsAsync("container1", Arg.Any<CancellationToken>())
            .Returns(Utils.CreateAsyncEnumerableWithException<AzuriteBlobItem>(new InvalidOperationException("Blob error")));

        var cacheSyncService = new CacheSyncService(context, _service, _logger);

        // Act
        Func<Task> act = async () => await cacheSyncService.SynchronizeCacheAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region UpdateSizeAndCount Tests

    [Fact(Timeout = 15000)]
    public void UpdateSizeAndCount_WithEmptyList_ShouldNotChangeValues()
    {
        // Arrange
        long totalSize = 0;
        int blobCount = 0;
        var blobs = new List<AzuriteBlobItem>();

        // Act
        CacheSyncService.UpdateSizeAndCount(blobs, ref totalSize, ref blobCount);

        // Assert
        totalSize.Should().Be(0);
        blobCount.Should().Be(0);
    }

    [Fact(Timeout = 15000)]
    public void UpdateSizeAndCount_WithSingleBlob_ShouldUpdateValues()
    {
        // Arrange
        long totalSize = 0;
        int blobCount = 0;
        var blobs = new List<AzuriteBlobItem>
        {
            CreateBlobItem(contentLength: 1024)
        };

        // Act
        CacheSyncService.UpdateSizeAndCount(blobs, ref totalSize, ref blobCount);

        // Assert
        totalSize.Should().Be(1024);
        blobCount.Should().Be(1);
    }

    [Fact(Timeout = 15000)]
    public void UpdateSizeAndCount_WithMultipleBlobs_ShouldSumValues()
    {
        // Arrange
        long totalSize = 0;
        int blobCount = 0;
        var blobs = new List<AzuriteBlobItem>
        {
            CreateBlobItem(contentLength: 1024),
            CreateBlobItem(contentLength: 2048),
            CreateBlobItem(contentLength: 512)
        };

        // Act
        CacheSyncService.UpdateSizeAndCount(blobs, ref totalSize, ref blobCount);

        // Assert
        totalSize.Should().Be(3584);
        blobCount.Should().Be(3);
    }

    [Fact(Timeout = 15000)]
    public void UpdateSizeAndCount_CalledMultipleTimes_ShouldAccumulateValues()
    {
        // Arrange
        long totalSize = 0;
        int blobCount = 0;
        var blobs1 = new List<AzuriteBlobItem> { CreateBlobItem(contentLength: 1024) };
        var blobs2 = new List<AzuriteBlobItem> { CreateBlobItem(contentLength: 2048) };

        // Act
        CacheSyncService.UpdateSizeAndCount(blobs1, ref totalSize, ref blobCount);
        CacheSyncService.UpdateSizeAndCount(blobs2, ref totalSize, ref blobCount);

        // Assert
        totalSize.Should().Be(3072);
        blobCount.Should().Be(2);
    }

    #endregion

    #region CleanupOldCacheEntriesAsync Tests

    [Fact(Timeout = 15000)]
    public async Task CleanupOldCacheEntriesAsync_ShouldRemoveContainersWithDifferentCacheId()
    {
        // Arrange
        using var context = CreateDbContext();
        var cacheSyncService = new CacheSyncService(context, _service, _logger);

        var oldContainer = new ContainerModel
        {
            Name = "old-container",
            CachedCopyId = "old-id",
            ETag = "etag1"
        };
        var newContainer = new ContainerModel
        {
            Name = "new-container",
            CachedCopyId = "new-id",
            ETag = "etag2"
        };
        context.Containers.AddRange(oldContainer, newContainer);
        await context.SaveChangesAsync();

        // Act
        await cacheSyncService.CleanupOldCacheEntriesAsync("new-id");

        // Assert
        var containers = await context.Containers.ToListAsync();
        containers.Should().ContainSingle(x => x.Name == "new-container");
    }

    [Fact(Timeout = 15000)]
    public async Task CleanupOldCacheEntriesAsync_ShouldRemoveBlobsWithDifferentCacheId()
    {
        // Arrange
        using var context = CreateDbContext();
        var cacheSyncService = new CacheSyncService(context, _service, _logger);

        // Create container first (required for foreign key constraint)
        var container = new ContainerModel
        {
            Name = "container",
            ETag = "container-etag",
            CachedCopyId = "new-id"
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var oldBlob = new BlobModel
        {
            Name = "old-blob.txt",
            ContainerName = "container",
            CachedCopyId = "old-id",
            ETag = "etag1"
        };
        var newBlob = new BlobModel
        {
            Name = "new-blob.txt",
            ContainerName = "container",
            CachedCopyId = "new-id",
            ETag = "etag2"
        };
        context.Blobs.AddRange(oldBlob, newBlob);
        await context.SaveChangesAsync();

        // Act
        await cacheSyncService.CleanupOldCacheEntriesAsync("new-id");

        // Assert
        var blobs = await context.Blobs.ToListAsync();
        blobs.Should().ContainSingle(x => x.Name == "new-blob.txt");
    }

    [Fact(Timeout = 15000)]
    public async Task CleanupOldCacheEntriesAsync_ShouldKeepEntriesWithMatchingCacheId()
    {
        // Arrange
        using var context = CreateDbContext();
        var cacheSyncService = new CacheSyncService(context, _service, _logger);

        var container1 = new ContainerModel
        {
            Name = "container1",
            CachedCopyId = "current-id",
            ETag = "etag1"
        };
        var container2 = new ContainerModel
        {
            Name = "container2",
            CachedCopyId = "current-id",
            ETag = "etag2"
        };
        context.Containers.AddRange(container1, container2);
        await context.SaveChangesAsync();

        // Act
        await cacheSyncService.CleanupOldCacheEntriesAsync("current-id");

        // Assert
        var containers = await context.Containers.ToListAsync();
        containers.Should().HaveCount(2)
            .And.Contain(c => c.Name == "container1")
            .And.Contain(c => c.Name == "container2")
            .And.AllSatisfy(c => c.CachedCopyId.Should().Be("current-id"));
    }

    #endregion

    #region CleanupStaleUploadsAsync Tests

    [Fact(Timeout = 15000)]
    public async Task CleanupStaleUploadsAsync_WithNoUploads_ShouldCompleteSuccessfully()
    {
        // Arrange
        using var context = CreateDbContext();
        var cacheSyncService = new CacheSyncService(context, _service, _logger);

        // Act
        await cacheSyncService.CleanupStaleUploadsAsync();

        // Assert
        var uploads = await context.Uploads.ToListAsync();
        uploads.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task CleanupStaleUploadsAsync_WithRecentUploads_ShouldNotRemoveThem()
    {
        // Arrange
        using var context = CreateDbContext();
        var cacheSyncService = new CacheSyncService(context, _service, _logger);

        var recentUpload = new UploadModel
        {
            UploadId = Guid.NewGuid(),
            ContainerName = "container",
            BlobName = "blob.txt",
            LastActivityAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        context.Uploads.Add(recentUpload);
        await context.SaveChangesAsync();

        // Act
        await cacheSyncService.CleanupStaleUploadsAsync();

        // Assert
        var uploads = await context.Uploads.ToListAsync();
        uploads.Should().ContainSingle(x => x.BlobName == "blob.txt" && x.ContainerName == "container");
    }

    [Fact(Timeout = 15000)]
    public async Task CleanupStaleUploadsAsync_WithStaleUploads_ShouldRemoveThem()
    {
        // Arrange
        using var context = CreateDbContext();
        var cacheSyncService = new CacheSyncService(context, _service, _logger);

        var staleUpload = new UploadModel
        {
            UploadId = Guid.NewGuid(),
            ContainerName = "container",
            BlobName = "blob.txt",
            LastActivityAt = DateTimeOffset.UtcNow.AddMinutes(-20)
        };
        context.Uploads.Add(staleUpload);
        await context.SaveChangesAsync();

        // Act
        await cacheSyncService.CleanupStaleUploadsAsync();

        // Assert
        var uploads = await context.Uploads.ToListAsync();
        uploads.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task CleanupStaleUploadsAsync_WithMixedUploads_ShouldRemoveOnlyStaleOnes()
    {
        // Arrange
        using var context = CreateDbContext();
        var cacheSyncService = new CacheSyncService(context, _service, _logger);

        var staleUpload = new UploadModel
        {
            UploadId = Guid.NewGuid(),
            ContainerName = "container",
            BlobName = "stale.txt",
            LastActivityAt = DateTimeOffset.UtcNow.AddMinutes(-20)
        };
        var recentUpload = new UploadModel
        {
            UploadId = Guid.NewGuid(),
            ContainerName = "container",
            BlobName = "recent.txt",
            LastActivityAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        context.Uploads.AddRange(staleUpload, recentUpload);
        await context.SaveChangesAsync();

        // Act
        await cacheSyncService.CleanupStaleUploadsAsync();

        // Assert
        var uploads = await context.Uploads.ToListAsync();
        uploads.Should().ContainSingle(x => x.BlobName == "recent.txt");
    }

    #endregion
}
