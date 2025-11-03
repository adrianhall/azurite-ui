using AzuriteUI.Web.Services.Azurite.Models;
using AzuriteUI.Web.Services.CacheDb.Models;
using AzuriteUI.Web.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AzuriteUI.Web.UnitTests.Services.CacheDb;

[ExcludeFromCodeCoverage]
public class CacheDbContext_Tests : SqliteDbTests
{

    #region UpsertContainerAsync Tests

    [Fact(Timeout = 15000)]
    public async Task UpsertContainerAsync_WithNewContainer_ShouldCreateContainer()
    {
        // Arrange
        using var context = CreateDbContext();

        var containerItem = CreateContainerItem("new-container");
        var cacheCopyId = Guid.NewGuid().ToString("N");

        // Act
        var result = await context.UpsertContainerAsync(containerItem, cacheCopyId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("new-container");
        result.CachedCopyId.Should().Be(cacheCopyId);
        result.ETag.Should().Be(containerItem.ETag);

        var storedContainer = await context.Containers.FirstOrDefaultAsync(c => c.Name == "new-container");
        storedContainer.Should().NotBeNull();
    }

    [Fact(Timeout = 15000)]
    public async Task UpsertContainerAsync_WithExistingContainer_ShouldUpdateContainer()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create existing container
        var existingContainer = new ContainerModel
        {
            Name = "existing-container",
            CachedCopyId = "old-id",
            ETag = "old-etag",
            LastModified = DateTimeOffset.UtcNow.AddDays(-1),
            DefaultEncryptionScope = "old-scope"
        };
        context.Containers.Add(existingContainer);
        await context.SaveChangesAsync();

        var containerItem = CreateContainerItem("existing-container", etag: "new-etag");
        containerItem.DefaultEncryptionScope = "new-scope";
        var cacheCopyId = Guid.NewGuid().ToString("N");

        // Act
        var result = await context.UpsertContainerAsync(containerItem, cacheCopyId, CancellationToken.None);

        // Assert
        result.Name.Should().Be("existing-container");
        result.CachedCopyId.Should().Be(cacheCopyId);
        result.ETag.Should().Be("new-etag");
        result.DefaultEncryptionScope.Should().Be("new-scope");

        var containers = await context.Containers.ToListAsync();
        containers.Should().ContainSingle(x => x.Name == "existing-container" && x.ETag == "new-etag" && x.CachedCopyId == cacheCopyId);
    }

    [Fact(Timeout = 15000)]
    public async Task UpsertContainerAsync_ShouldSetAllProperties()
    {
        // Arrange
        using var context = CreateDbContext();

        var containerItem = new AzuriteContainerItem
        {
            Name = "test-container",
            ETag = "test-etag",
            LastModified = DateTimeOffset.UtcNow,
            DefaultEncryptionScope = "test-scope",
            HasImmutabilityPolicy = true,
            HasImmutableStorageWithVersioning = true,
            HasLegalHold = true,
            Metadata = new Dictionary<string, string> { ["key1"] = "value1" },
            PreventEncryptionScopeOverride = true,
            PublicAccess = AzuritePublicAccess.Container,
            RemainingRetentionDays = 30
        };
        var cacheCopyId = Guid.NewGuid().ToString("N");

        // Act
        var result = await context.UpsertContainerAsync(containerItem, cacheCopyId, CancellationToken.None);

        // Assert
        result.DefaultEncryptionScope.Should().Be("test-scope");
        result.HasImmutabilityPolicy.Should().BeTrue();
        result.HasImmutableStorageWithVersioning.Should().BeTrue();
        result.HasLegalHold.Should().BeTrue();
        result.Metadata.Should().ContainKey("key1");
        result.PreventEncryptionScopeOverride.Should().BeTrue();
        result.PublicAccess.Should().Be(AzuritePublicAccess.Container);
        result.RemainingRetentionDays.Should().Be(30);
    }

    [Fact(Timeout = 15000)]
    public async Task UpsertContainerAsync_WithoutCacheCopyId_ShouldCreateContainerWithGeneratedId()
    {
        // Arrange
        using var context = CreateDbContext();

        var containerItem = CreateContainerItem("new-container");

        // Act
        var result = await context.UpsertContainerAsync(containerItem, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("new-container");
        result.CachedCopyId.Should().NotBeNullOrEmpty();
        result.ETag.Should().Be(containerItem.ETag);

        var storedContainer = await context.Containers.FirstOrDefaultAsync(c => c.Name == "new-container");
        storedContainer.Should().NotBeNull();
        storedContainer!.CachedCopyId.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region UpsertBlobAsync Tests

    [Fact(Timeout = 15000)]
    public async Task UpsertBlobAsync_WithNewBlob_ShouldCreateBlob()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create container first due to foreign key constraint
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "container-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var blobItem = CreateBlobItem("new-blob.txt");
        var containerName = "test-container";
        var cacheCopyId = Guid.NewGuid().ToString("N");

        // Act
        var result = await context.UpsertBlobAsync(blobItem, containerName, cacheCopyId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("new-blob.txt");
        result.ContainerName.Should().Be(containerName);
        result.CachedCopyId.Should().Be(cacheCopyId);
        result.ETag.Should().Be(blobItem.ETag);

        var storedBlob = await context.Blobs.FirstOrDefaultAsync(b => b.Name == "new-blob.txt" && b.ContainerName == containerName);
        storedBlob.Should().NotBeNull();
    }

    [Fact(Timeout = 15000)]
    public async Task UpsertBlobAsync_WithExistingBlob_ShouldUpdateBlob()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create container first due to foreign key constraint
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "container-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        // Create existing blob
        var existingBlob = new BlobModel
        {
            Name = "existing-blob.txt",
            ContainerName = "test-container",
            CachedCopyId = "old-id",
            ETag = "old-etag",
            LastModified = DateTimeOffset.UtcNow.AddDays(-1),
            ContentType = "text/plain",
            ContentLength = 100,
            CreatedOn = DateTimeOffset.UtcNow.AddDays(-2)
        };
        context.Blobs.Add(existingBlob);
        await context.SaveChangesAsync();

        var blobItem = CreateBlobItem("existing-blob.txt", etag: "new-etag", contentLength: 2048);
        blobItem.ContentType = "application/json";
        var cacheCopyId = Guid.NewGuid().ToString("N");

        // Act
        var result = await context.UpsertBlobAsync(blobItem, "test-container", cacheCopyId, CancellationToken.None);

        // Assert
        result.Name.Should().Be("existing-blob.txt");
        result.CachedCopyId.Should().Be(cacheCopyId);
        result.ETag.Should().Be("new-etag");
        result.ContentType.Should().Be("application/json");
        result.ContentLength.Should().Be(2048);

        var blobs = await context.Blobs.ToListAsync();
        blobs.Should().ContainSingle(x => x.Name == "existing-blob.txt" && x.ETag == "new-etag" && x.CachedCopyId == cacheCopyId);
    }

    [Fact(Timeout = 15000)]
    public async Task UpsertBlobAsync_ShouldSetAllProperties()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create container first due to foreign key constraint
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "container-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var blobItem = new AzuriteBlobItem
        {
            Name = "test-blob.txt",
            ETag = "test-etag",
            LastModified = DateTimeOffset.UtcNow,
            BlobType = AzuriteBlobType.Block,
            ContentEncoding = "gzip",
            ContentLanguage = "en-US",
            ContentLength = 4096,
            ContentType = "text/html",
            CreatedOn = DateTimeOffset.UtcNow.AddHours(-2),
            ExpiresOn = DateTimeOffset.UtcNow.AddDays(7),
            HasLegalHold = true,
            LastAccessedOn = DateTimeOffset.UtcNow.AddMinutes(-30),
            Metadata = new Dictionary<string, string> { ["key1"] = "value1", ["key2"] = "value2" },
            RemainingRetentionDays = 30,
            Tags = new Dictionary<string, string> { ["env"] = "test", ["version"] = "1.0" }
        };
        var containerName = "test-container";
        var cacheCopyId = Guid.NewGuid().ToString("N");

        // Act
        var result = await context.UpsertBlobAsync(blobItem, containerName, cacheCopyId, CancellationToken.None);

        // Assert
        result.BlobType.Should().Be(AzuriteBlobType.Block);
        result.ContentEncoding.Should().Be("gzip");
        result.ContentLanguage.Should().Be("en-US");
        result.ContentLength.Should().Be(4096);
        result.ContentType.Should().Be("text/html");
        result.CreatedOn.Should().Be(blobItem.CreatedOn.Value);
        result.ExpiresOn.Should().Be(blobItem.ExpiresOn);
        result.HasLegalHold.Should().BeTrue();
        result.LastAccessedOn.Should().Be(blobItem.LastAccessedOn);
        result.Metadata.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        result.Metadata.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
        result.RemainingRetentionDays.Should().Be(30);
        result.Tags.Should().ContainKey("env").WhoseValue.Should().Be("test");
        result.Tags.Should().ContainKey("version").WhoseValue.Should().Be("1.0");
    }

    [Fact(Timeout = 15000)]
    public async Task UpsertBlobAsync_WithoutCacheCopyId_ShouldCreateBlobWithGeneratedId()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create container first due to foreign key constraint
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "container-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var blobItem = CreateBlobItem("new-blob.txt");
        var containerName = "test-container";

        // Act
        var result = await context.UpsertBlobAsync(blobItem, containerName, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("new-blob.txt");
        result.ContainerName.Should().Be(containerName);
        result.CachedCopyId.Should().NotBeNullOrEmpty();
        result.ETag.Should().Be(blobItem.ETag);

        var storedBlob = await context.Blobs.FirstOrDefaultAsync(b => b.Name == "new-blob.txt" && b.ContainerName == containerName);
        storedBlob.Should().NotBeNull();
        storedBlob!.CachedCopyId.Should().NotBeNullOrEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task UpsertBlobAsync_WithNullCreatedOn_ShouldSetToMinValue()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create container first due to foreign key constraint
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "container-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var blobItem = CreateBlobItem("test-blob.txt");
        blobItem.CreatedOn = null;
        var containerName = "test-container";
        var cacheCopyId = Guid.NewGuid().ToString("N");

        // Act
        var result = await context.UpsertBlobAsync(blobItem, containerName, cacheCopyId, CancellationToken.None);

        // Assert
        result.CreatedOn.Should().Be(DateTimeOffset.MinValue);
    }

    [Fact(Timeout = 15000)]
    public async Task UpsertBlobAsync_WithNewBlob_ShouldUpdateContainerBlobCountAndTotalSize()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create container first due to foreign key constraint
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "container-etag",
            LastModified = DateTimeOffset.UtcNow,
            BlobCount = 0,
            TotalSize = 0
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var blobItem = CreateBlobItem("new-blob.txt", contentLength: 2048);
        var containerName = "test-container";
        var cacheCopyId = Guid.NewGuid().ToString("N");

        // Act
        await context.UpsertBlobAsync(blobItem, containerName, cacheCopyId, CancellationToken.None);

        // Assert
        var updatedContainer = await context.Containers.FirstOrDefaultAsync(c => c.Name == containerName);
        updatedContainer.Should().NotBeNull();
        updatedContainer!.BlobCount.Should().Be(1);
        updatedContainer.TotalSize.Should().Be(2048);
    }

    [Fact(Timeout = 15000)]
    public async Task UpsertBlobAsync_WithMultipleBlobs_ShouldUpdateContainerBlobCountAndTotalSize()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create container first due to foreign key constraint
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "container-etag",
            LastModified = DateTimeOffset.UtcNow,
            BlobCount = 0,
            TotalSize = 0
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var blob1 = CreateBlobItem("blob1.txt", contentLength: 1024);
        var blob2 = CreateBlobItem("blob2.txt", contentLength: 2048);
        var blob3 = CreateBlobItem("blob3.txt", contentLength: 4096);
        var cacheCopyId = Guid.NewGuid().ToString("N");

        // Act
        await context.UpsertBlobAsync(blob1, "test-container", cacheCopyId, CancellationToken.None);
        await context.UpsertBlobAsync(blob2, "test-container", cacheCopyId, CancellationToken.None);
        await context.UpsertBlobAsync(blob3, "test-container", cacheCopyId, CancellationToken.None);

        // Assert
        var updatedContainer = await context.Containers.FirstOrDefaultAsync(c => c.Name == "test-container");
        updatedContainer.Should().NotBeNull();
        updatedContainer!.BlobCount.Should().Be(3);
        updatedContainer.TotalSize.Should().Be(7168); // 1024 + 2048 + 4096
    }

    [Fact(Timeout = 15000)]
    public async Task UpsertBlobAsync_WithExistingBlobUpdateSize_ShouldRecalculateContainerTotalSize()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create container first due to foreign key constraint
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "container-etag",
            LastModified = DateTimeOffset.UtcNow,
            BlobCount = 1,
            TotalSize = 1024
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        // Create existing blob with size 1024
        var existingBlob = new BlobModel
        {
            Name = "existing-blob.txt",
            ContainerName = "test-container",
            CachedCopyId = "old-id",
            ETag = "old-etag",
            LastModified = DateTimeOffset.UtcNow.AddDays(-1),
            ContentType = "text/plain",
            ContentLength = 1024,
            CreatedOn = DateTimeOffset.UtcNow.AddDays(-2)
        };
        context.Blobs.Add(existingBlob);
        await context.SaveChangesAsync();

        // Update blob with new size 5120
        var blobItem = CreateBlobItem("existing-blob.txt", etag: "new-etag", contentLength: 5120);
        var cacheCopyId = Guid.NewGuid().ToString("N");

        // Act
        await context.UpsertBlobAsync(blobItem, "test-container", cacheCopyId, CancellationToken.None);

        // Assert
        var updatedContainer = await context.Containers.FirstOrDefaultAsync(c => c.Name == "test-container");
        updatedContainer.Should().NotBeNull();
        updatedContainer!.BlobCount.Should().Be(1); // Still 1 blob
        updatedContainer.TotalSize.Should().Be(5120); // Size updated from 1024 to 5120
    }

    [Fact(Timeout = 15000)]
    public async Task UpsertBlobAsync_WithExistingBlobsAndNewBlob_ShouldAccumulateContainerTotals()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create container first due to foreign key constraint
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "container-etag",
            LastModified = DateTimeOffset.UtcNow,
            BlobCount = 2,
            TotalSize = 3072
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        // Create existing blobs
        var existingBlob1 = new BlobModel
        {
            Name = "existing1.txt",
            ContainerName = "test-container",
            CachedCopyId = "id1",
            ETag = "etag1",
            LastModified = DateTimeOffset.UtcNow,
            ContentLength = 1024,
            CreatedOn = DateTimeOffset.UtcNow
        };
        var existingBlob2 = new BlobModel
        {
            Name = "existing2.txt",
            ContainerName = "test-container",
            CachedCopyId = "id2",
            ETag = "etag2",
            LastModified = DateTimeOffset.UtcNow,
            ContentLength = 2048,
            CreatedOn = DateTimeOffset.UtcNow
        };
        context.Blobs.AddRange(existingBlob1, existingBlob2);
        await context.SaveChangesAsync();

        // Add a new blob
        var newBlob = CreateBlobItem("new-blob.txt", contentLength: 4096);
        var cacheCopyId = Guid.NewGuid().ToString("N");

        // Act
        await context.UpsertBlobAsync(newBlob, "test-container", cacheCopyId, CancellationToken.None);

        // Assert
        var updatedContainer = await context.Containers.FirstOrDefaultAsync(c => c.Name == "test-container");
        updatedContainer.Should().NotBeNull();
        updatedContainer!.BlobCount.Should().Be(3); // 2 existing + 1 new
        updatedContainer.TotalSize.Should().Be(7168); // 1024 + 2048 + 4096
    }

    [Fact(Timeout = 15000)]
    public async Task UpsertBlobAsync_WithMultipleUpdates_ShouldMaintainCorrectContainerTotals()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create container first due to foreign key constraint
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "container-etag",
            LastModified = DateTimeOffset.UtcNow,
            BlobCount = 0,
            TotalSize = 0
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var cacheCopyId = Guid.NewGuid().ToString("N");

        // Act - Add first blob
        var blob1 = CreateBlobItem("blob1.txt", contentLength: 1000);
        await context.UpsertBlobAsync(blob1, "test-container", cacheCopyId, CancellationToken.None);

        // Act - Add second blob
        var blob2 = CreateBlobItem("blob2.txt", contentLength: 2000);
        await context.UpsertBlobAsync(blob2, "test-container", cacheCopyId, CancellationToken.None);

        // Act - Update first blob with new size
        var blob1Updated = CreateBlobItem("blob1.txt", etag: "new-etag", contentLength: 1500);
        await context.UpsertBlobAsync(blob1Updated, "test-container", cacheCopyId, CancellationToken.None);

        // Assert
        var updatedContainer = await context.Containers.FirstOrDefaultAsync(c => c.Name == "test-container");
        updatedContainer.Should().NotBeNull();
        updatedContainer!.BlobCount.Should().Be(2);
        updatedContainer.TotalSize.Should().Be(3500); // 1500 (updated) + 2000
    }

    #endregion


    #region UpsertBlobsAsync Tests

    [Fact(Timeout = 15000)]
    public async Task UpsertBlobsAsync_WithEmptyList_ShouldReturnImmediately()
    {
        // Arrange
        using var context = CreateDbContext();
            var cacheCopyId = Guid.NewGuid().ToString("N");

        // Act
        await context.UpsertBlobsAsync([], "test-container", cacheCopyId, CancellationToken.None);

        // Assert
        var blobs = await context.Blobs.ToListAsync();
        blobs.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task StoreBlobsInContextAsync_WithNewBlobs_ShouldCreateBlobs()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create container first (required for foreign key constraint)
        var container = new ContainerModel
        {
            Name = "test-container",
            ETag = "test-etag",
            CachedCopyId = Guid.NewGuid().ToString("N")
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var blob1 = CreateBlobItem("blob1.txt");
        var blob2 = CreateBlobItem("blob2.txt");
        var cacheCopyId = Guid.NewGuid().ToString("N");

        // Act
        await context.UpsertBlobsAsync([ blob1, blob2 ], "test-container", cacheCopyId, CancellationToken.None);

        // Assert
        var blobs = await context.Blobs.ToListAsync();
        blobs.Should().HaveCount(2)
            .And.Contain(b => b.Name == "blob1.txt")
            .And.Contain(b => b.Name == "blob2.txt")
            .And.AllSatisfy(b => b.ContainerName.Should().Be("test-container"))
            .And.AllSatisfy(b => b.CachedCopyId.Should().Be(cacheCopyId));
    }

    [Fact(Timeout = 15000)]
    public async Task StoreBlobsInContextAsync_WithExistingBlobs_ShouldUpdateBlobs()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create container first (required for foreign key constraint)
        var container = new ContainerModel
        {
            Name = "test-container",
            ETag = "test-etag",
            CachedCopyId = Guid.NewGuid().ToString("N")
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        // Create existing blob
        var existingBlob = new BlobModel
        {
            Name = "existing.txt",
            ContainerName = "test-container",
            CachedCopyId = "old-id",
            ETag = "old-etag",
            ContentLength = 100,
            LastModified = DateTimeOffset.UtcNow.AddDays(-1)
        };
        context.Blobs.Add(existingBlob);
        await context.SaveChangesAsync();

        var blobItem = CreateBlobItem("existing.txt", etag: "new-etag", contentLength: 200);
        var cacheCopyId = Guid.NewGuid().ToString("N");

        // Act
        await context.UpsertBlobsAsync([blobItem], "test-container", cacheCopyId, CancellationToken.None);

        // Assert
        var blobs = await context.Blobs.ToListAsync();
        blobs.Should().ContainSingle(x => x.Name == "existing.txt" && x.ETag == "new-etag" && x.ContentLength == 200 && x.CachedCopyId == cacheCopyId);
    }

    [Fact(Timeout = 15000)]
    public async Task StoreBlobsInContextAsync_WithMixedBlobs_ShouldCreateAndUpdate()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create container first (required for foreign key constraint)
        var container = new ContainerModel
        {
            Name = "test-container",
            ETag = "test-etag",
            CachedCopyId = Guid.NewGuid().ToString("N")
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        // Create existing blob
        var existingBlob = new BlobModel
        {
            Name = "existing.txt",
            ContainerName = "test-container",
            CachedCopyId = "old-id",
            ETag = "old-etag",
            ContentLength = 100
        };
        context.Blobs.Add(existingBlob);
        await context.SaveChangesAsync();

        var existingBlobItem = CreateBlobItem("existing.txt", etag: "new-etag", contentLength: 200);
        var newBlobItem = CreateBlobItem("new.txt");
        var cacheCopyId = Guid.NewGuid().ToString("N");

        // Act
        await context.UpsertBlobsAsync([existingBlobItem, newBlobItem], "test-container", cacheCopyId, CancellationToken.None);

        // Assert
        var blobs = await context.Blobs.ToListAsync();
        blobs.Should().HaveCount(2)
            .And.Contain(b => b.Name == "existing.txt" && b.ETag == "new-etag")
            .And.Contain(b => b.Name == "new.txt");
    }

    [Fact(Timeout = 15000)]
    public async Task StoreBlobsInContextAsync_ShouldSetAllBlobProperties()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create container first (required for foreign key constraint)
        var container = new ContainerModel
        {
            Name = "test-container",
            ETag = "test-etag",
            CachedCopyId = Guid.NewGuid().ToString("N")
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var blobItem = new AzuriteBlobItem
        {
            Name = "test.txt",
            ETag = "test-etag",
            LastModified = DateTimeOffset.UtcNow,
            BlobType = AzuriteBlobType.Append,
            ContentEncoding = "gzip",
            ContentLanguage = "en-US",
            ContentLength = 5000,
            ContentType = "application/json",
            CreatedOn = DateTimeOffset.UtcNow.AddHours(-2),
            ExpiresOn = DateTimeOffset.UtcNow.AddDays(7),
            HasLegalHold = true,
            LastAccessedOn = DateTimeOffset.UtcNow.AddMinutes(-30),
            Metadata = new Dictionary<string, string> { ["key"] = "value" },
            RemainingRetentionDays = 15,
            Tags = new Dictionary<string, string> { ["env"] = "test" }
        };
        var cacheCopyId = Guid.NewGuid().ToString("N");

        // Act
        await context.UpsertBlobsAsync([blobItem], "test-container", cacheCopyId, CancellationToken.None);

        // Assert
        var blob = await context.Blobs.FirstOrDefaultAsync();
        blob.Should().NotBeNull();
        blob!.BlobType.Should().Be(AzuriteBlobType.Append);
        blob.ContentEncoding.Should().Be("gzip");
        blob.ContentLanguage.Should().Be("en-US");
        blob.ContentLength.Should().Be(5000);
        blob.ContentType.Should().Be("application/json");
        blob.HasLegalHold.Should().BeTrue();
        blob.Metadata.Should().ContainKey("key");
        blob.RemainingRetentionDays.Should().Be(15);
        blob.Tags.Should().ContainKey("env");
    }

    [Fact(Timeout = 15000)]
    public async Task StoreBlobsInContextAsync_WithNullCreatedOnForNewBlob_ShouldUseMinValue()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create container first (required for foreign key constraint)
        var container = new ContainerModel
        {
            Name = "test-container",
            ETag = "test-etag",
            CachedCopyId = Guid.NewGuid().ToString("N")
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        // Create blob item with null CreatedOn
        var blobItem = new AzuriteBlobItem
        {
            Name = "test.txt",
            ETag = "test-etag",
            LastModified = DateTimeOffset.UtcNow,
            BlobType = AzuriteBlobType.Block,
            ContentEncoding = string.Empty,
            ContentLanguage = string.Empty,
            ContentLength = 1024,
            ContentType = "text/plain",
            CreatedOn = null, // This is the key: null value to test the ?? branch
            ExpiresOn = null,
            HasLegalHold = false,
            LastAccessedOn = null,
            Metadata = new Dictionary<string, string>(),
            RemainingRetentionDays = null,
            Tags = new Dictionary<string, string>()
        };
        var cacheCopyId = Guid.NewGuid().ToString("N");

        // Act
        await context.UpsertBlobsAsync([blobItem], "test-container", cacheCopyId, CancellationToken.None);

        // Assert
        var blob = await context.Blobs.FirstOrDefaultAsync();
        blob.Should().NotBeNull();
        blob!.CreatedOn.Should().Be(DateTimeOffset.MinValue); // Should default to MinValue
    }

    [Fact(Timeout = 15000)]
    public async Task StoreBlobsInContextAsync_WithNullCreatedOnForExistingBlob_ShouldUseMinValue()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create container first (required for foreign key constraint)
        var container = new ContainerModel
        {
            Name = "test-container",
            ETag = "test-etag",
            CachedCopyId = Guid.NewGuid().ToString("N")
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        // Create existing blob with a specific CreatedOn value
        var existingBlob = new BlobModel
        {
            Name = "existing.txt",
            ContainerName = "test-container",
            CachedCopyId = "old-id",
            ETag = "old-etag",
            ContentLength = 100,
            LastModified = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedOn = DateTimeOffset.UtcNow.AddDays(-10)
        };
        context.Blobs.Add(existingBlob);
        await context.SaveChangesAsync();

        // Create blob item with null CreatedOn
        var blobItem = new AzuriteBlobItem
        {
            Name = "existing.txt",
            ETag = "new-etag",
            LastModified = DateTimeOffset.UtcNow,
            BlobType = AzuriteBlobType.Block,
            ContentEncoding = string.Empty,
            ContentLanguage = string.Empty,
            ContentLength = 200,
            ContentType = "text/plain",
            CreatedOn = null, // This is the key: null value to test the ?? branch
            ExpiresOn = null,
            HasLegalHold = false,
            LastAccessedOn = null,
            Metadata = new Dictionary<string, string>(),
            RemainingRetentionDays = null,
            Tags = new Dictionary<string, string>()
        };
        var cacheCopyId = Guid.NewGuid().ToString("N");

        // Act
        await context.UpsertBlobsAsync([blobItem], "test-container", cacheCopyId, CancellationToken.None);

        // Assert
        var blob = await context.Blobs.FirstOrDefaultAsync();
        blob.Should().NotBeNull();
        blob!.CreatedOn.Should().Be(DateTimeOffset.MinValue); // Should default to MinValue when null
        blob.ETag.Should().Be("new-etag"); // Verify it was updated
    }

    #endregion

    #region RemoveBlobAsync Tests

    [Fact(Timeout = 15000)]
    public async Task RemoveBlobAsync_WithExistingBlob_ShouldRemoveBlob()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create container first due to foreign key constraint
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "container-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var blob = new BlobModel
        {
            Name = "blob-to-remove.txt",
            ContainerName = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "test-etag",
            LastModified = DateTimeOffset.UtcNow,
            CreatedOn = DateTimeOffset.UtcNow
        };
        context.Blobs.Add(blob);
        await context.SaveChangesAsync();

        // Verify blob exists
        var existingBlob = await context.Blobs.FirstOrDefaultAsync(b => b.Name == "blob-to-remove.txt" && b.ContainerName == "test-container");
        existingBlob.Should().NotBeNull();

        // Act
        await context.RemoveBlobAsync("test-container", "blob-to-remove.txt", CancellationToken.None);

        // Assert
        var removedBlob = await context.Blobs.FirstOrDefaultAsync(b => b.Name == "blob-to-remove.txt" && b.ContainerName == "test-container");
        removedBlob.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public async Task RemoveBlobAsync_WithNonExistentBlob_ShouldNotThrow()
    {
        // Arrange
        using var context = CreateDbContext();

        // Act & Assert - should not throw
        await context.RemoveBlobAsync("test-container", "non-existent-blob.txt", CancellationToken.None);

        // Verify no blobs were affected
        var blobs = await context.Blobs.ToListAsync();
        blobs.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task RemoveBlobAsync_WithMultipleBlobs_ShouldOnlyRemoveSpecifiedBlob()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create container first due to foreign key constraint
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "container-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var blob1 = new BlobModel
        {
            Name = "blob-1.txt",
            ContainerName = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "etag-1",
            LastModified = DateTimeOffset.UtcNow,
            CreatedOn = DateTimeOffset.UtcNow
        };
        var blob2 = new BlobModel
        {
            Name = "blob-2.txt",
            ContainerName = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "etag-2",
            LastModified = DateTimeOffset.UtcNow,
            CreatedOn = DateTimeOffset.UtcNow
        };
        context.Blobs.AddRange(blob1, blob2);
        await context.SaveChangesAsync();

        // Act
        await context.RemoveBlobAsync("test-container", "blob-1.txt", CancellationToken.None);

        // Assert
        var remainingBlobs = await context.Blobs.ToListAsync();
        remainingBlobs.Should().ContainSingle();
        remainingBlobs[0].Name.Should().Be("blob-2.txt");
    }

    [Fact(Timeout = 15000)]
    public async Task RemoveBlobAsync_WithSameBlobNameInDifferentContainers_ShouldOnlyRemoveFromSpecifiedContainer()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create containers first due to foreign key constraint
        var container1 = new ContainerModel
        {
            Name = "container-1",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "container-etag-1",
            LastModified = DateTimeOffset.UtcNow
        };
        var container2 = new ContainerModel
        {
            Name = "container-2",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "container-etag-2",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.AddRange(container1, container2);
        await context.SaveChangesAsync();

        var blob1 = new BlobModel
        {
            Name = "same-blob.txt",
            ContainerName = "container-1",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "etag-1",
            LastModified = DateTimeOffset.UtcNow,
            CreatedOn = DateTimeOffset.UtcNow
        };
        var blob2 = new BlobModel
        {
            Name = "same-blob.txt",
            ContainerName = "container-2",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "etag-2",
            LastModified = DateTimeOffset.UtcNow,
            CreatedOn = DateTimeOffset.UtcNow
        };
        context.Blobs.AddRange(blob1, blob2);
        await context.SaveChangesAsync();

        // Act
        await context.RemoveBlobAsync("container-1", "same-blob.txt", CancellationToken.None);

        // Assert
        var remainingBlobs = await context.Blobs.ToListAsync();
        remainingBlobs.Should().ContainSingle();
        remainingBlobs[0].Name.Should().Be("same-blob.txt");
        remainingBlobs[0].ContainerName.Should().Be("container-2");
    }

    #endregion

    #region RemoveContainerAsync Tests

    [Fact(Timeout = 15000)]
    public async Task RemoveContainerAsync_WithExistingContainer_ShouldRemoveContainer()
    {
        // Arrange
        using var context = CreateDbContext();

        var container = new ContainerModel
        {
            Name = "container-to-remove",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "test-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        // Verify container exists
        var existingContainer = await context.Containers.FirstOrDefaultAsync(c => c.Name == "container-to-remove");
        existingContainer.Should().NotBeNull();

        // Act
        await context.RemoveContainerAsync("container-to-remove", CancellationToken.None);

        // Assert
        var removedContainer = await context.Containers.FirstOrDefaultAsync(c => c.Name == "container-to-remove");
        removedContainer.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public async Task RemoveContainerAsync_WithNonExistentContainer_ShouldNotThrow()
    {
        // Arrange
        using var context = CreateDbContext();

        // Act & Assert - should not throw
        await context.RemoveContainerAsync("non-existent-container", CancellationToken.None);

        // Verify no containers were affected
        var containers = await context.Containers.ToListAsync();
        containers.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task RemoveContainerAsync_WithMultipleContainers_ShouldOnlyRemoveSpecifiedContainer()
    {
        // Arrange
        using var context = CreateDbContext();

        var container1 = new ContainerModel
        {
            Name = "container-1",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "etag-1",
            LastModified = DateTimeOffset.UtcNow
        };
        var container2 = new ContainerModel
        {
            Name = "container-2",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "etag-2",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.AddRange(container1, container2);
        await context.SaveChangesAsync();

        // Act
        await context.RemoveContainerAsync("container-1", CancellationToken.None);

        // Assert
        var remainingContainers = await context.Containers.ToListAsync();
        remainingContainers.Should().ContainSingle();
        remainingContainers[0].Name.Should().Be("container-2");
    }

    #endregion

    #region UpsertUploadBlockAsync Tests

    [Fact(Timeout = 15000)]
    public async Task UpsertUploadBlockAsync_WithNewBlock_ShouldCreateBlock()
    {
        // Arrange
        using var context = CreateDbContext();

        await CreateContainerModelAsync(context);
        var upload = await CreateUploadModelAsync(context, "test-container");
        var blockModel = CreateUploadBlockModel(upload.UploadId, "block-001", contentMD5: "abc123");

        // Act
        var result = await context.UpsertUploadBlockAsync(blockModel, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UploadId.Should().Be(upload.UploadId);
        result.BlockId.Should().Be("block-001");
        result.BlockSize.Should().Be(1024);
        result.ContentMD5.Should().Be("abc123");

        var storedBlock = await context.UploadBlocks.FirstOrDefaultAsync(b => b.BlockId == "block-001" && b.UploadId == upload.UploadId);
        storedBlock.Should().NotBeNull();
    }

    [Fact(Timeout = 15000)]
    public async Task UpsertUploadBlockAsync_WithExistingBlock_ShouldUpdateBlock()
    {
        // Arrange
        using var context = CreateDbContext();

        await CreateContainerModelAsync(context);
        var upload = await CreateUploadModelAsync(context, "test-container");

        // Create existing block
        var existingBlock = CreateUploadBlockModel(upload.UploadId, "block-001", blockSize: 512, contentMD5: "old-hash", uploadedAt: DateTimeOffset.UtcNow.AddMinutes(-10));
        context.UploadBlocks.Add(existingBlock);
        await context.SaveChangesAsync();

        var newUploadedAt = DateTimeOffset.UtcNow;
        var updatedBlock = CreateUploadBlockModel(upload.UploadId, "block-001", blockSize: 2048, contentMD5: "new-hash", uploadedAt: newUploadedAt);

        // Act
        var result = await context.UpsertUploadBlockAsync(updatedBlock, CancellationToken.None);

        // Assert
        result.UploadId.Should().Be(upload.UploadId);
        result.BlockId.Should().Be("block-001");
        result.BlockSize.Should().Be(2048);
        result.ContentMD5.Should().Be("new-hash");
        result.UploadedAt.Should().Be(newUploadedAt);

        var blocks = await context.UploadBlocks.ToListAsync();
        blocks.Should().ContainSingle(x => x.BlockId == "block-001" && x.UploadId == upload.UploadId && x.BlockSize == 2048);
    }

    [Fact(Timeout = 15000)]
    public async Task UpsertUploadBlockAsync_WithAssociatedUpload_ShouldUpdateUploadLastActivityAt()
    {
        // Arrange
        using var context = CreateDbContext();

        await CreateContainerModelAsync(context);
        var originalLastActivityAt = DateTimeOffset.UtcNow.AddMinutes(-30);
        var upload = await CreateUploadModelAsync(context, "test-container",
            createdAt: DateTimeOffset.UtcNow.AddHours(-1),
            lastActivityAt: originalLastActivityAt);

        var blockModel = CreateUploadBlockModel(upload.UploadId, "block-001", contentMD5: "abc123");

        // Act
        await context.UpsertUploadBlockAsync(blockModel, CancellationToken.None);

        // Assert
        var updatedUpload = await context.Uploads.FirstOrDefaultAsync(u => u.UploadId == upload.UploadId);
        updatedUpload.Should().NotBeNull();
        updatedUpload!.LastActivityAt.Should().BeAfter(originalLastActivityAt);
        updatedUpload.LastActivityAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact(Timeout = 15000)]
    public async Task UpsertUploadBlockAsync_WithDeletedUpload_ShouldStillCreateBlockWithoutUpdatingUpload()
    {
        // Arrange
        using var context = CreateDbContext();

        await CreateContainerModelAsync(context);
        var upload = await CreateUploadModelAsync(context, "test-container");
        var blockModel = CreateUploadBlockModel(upload.UploadId, "block-001", contentMD5: "abc123");

        // Act
        var result = await context.UpsertUploadBlockAsync(blockModel, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UploadId.Should().Be(upload.UploadId);
        result.BlockId.Should().Be("block-001");

        var storedBlock = await context.UploadBlocks.FirstOrDefaultAsync(b => b.BlockId == "block-001" && b.UploadId == upload.UploadId);
        storedBlock.Should().NotBeNull();

        // Verify upload's LastActivityAt was updated
        var updatedUpload = await context.Uploads.FirstOrDefaultAsync(u => u.UploadId == upload.UploadId);
        updatedUpload.Should().NotBeNull();
    }

    [Fact(Timeout = 15000)]
    public async Task UpsertUploadBlockAsync_WithNullContentMD5_ShouldCreateBlock()
    {
        // Arrange
        using var context = CreateDbContext();

        await CreateContainerModelAsync(context);
        var upload = await CreateUploadModelAsync(context, "test-container");
        var blockModel = CreateUploadBlockModel(upload.UploadId, "block-001", contentMD5: null);

        // Act
        var result = await context.UpsertUploadBlockAsync(blockModel, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ContentMD5.Should().BeNull();

        var storedBlock = await context.UploadBlocks.FirstOrDefaultAsync(b => b.BlockId == "block-001" && b.UploadId == upload.UploadId);
        storedBlock.Should().NotBeNull();
        storedBlock!.ContentMD5.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public async Task UpsertUploadBlockAsync_WithMultipleBlocksForSameUpload_ShouldCreateAllBlocks()
    {
        // Arrange
        using var context = CreateDbContext();

        await CreateContainerModelAsync(context);
        var upload = await CreateUploadModelAsync(context, "test-container");

        var block1 = CreateUploadBlockModel(upload.UploadId, "block-001", contentMD5: "hash1");
        var block2 = CreateUploadBlockModel(upload.UploadId, "block-002", blockSize: 2048, contentMD5: "hash2");

        // Act
        await context.UpsertUploadBlockAsync(block1, CancellationToken.None);
        await context.UpsertUploadBlockAsync(block2, CancellationToken.None);

        // Assert
        var blocks = await context.UploadBlocks.Where(b => b.UploadId == upload.UploadId).ToListAsync();
        blocks.Should().HaveCount(2);
        blocks.Should().Contain(b => b.BlockId == "block-001");
        blocks.Should().Contain(b => b.BlockId == "block-002");
    }

    [Fact(Timeout = 15000)]
    public async Task UpsertUploadBlockAsync_WithSameBlockIdDifferentUploadId_ShouldCreateBothBlocks()
    {
        // Arrange
        using var context = CreateDbContext();

        await CreateContainerModelAsync(context);
        var upload1 = await CreateUploadModelAsync(context, "test-container", blobName: "test-blob1.txt");
        var upload2 = await CreateUploadModelAsync(context, "test-container", blobName: "test-blob2.txt", contentLength: 20480);

        var block1 = CreateUploadBlockModel(upload1.UploadId, "block-001", contentMD5: "hash1");
        var block2 = CreateUploadBlockModel(upload2.UploadId, "block-001", blockSize: 2048, contentMD5: "hash2");

        // Act
        await context.UpsertUploadBlockAsync(block1, CancellationToken.None);
        await context.UpsertUploadBlockAsync(block2, CancellationToken.None);

        // Assert
        var blocks = await context.UploadBlocks.ToListAsync();
        blocks.Should().HaveCount(2);
        blocks.Should().Contain(b => b.BlockId == "block-001" && b.UploadId == upload1.UploadId && b.BlockSize == 1024);
        blocks.Should().Contain(b => b.BlockId == "block-001" && b.UploadId == upload2.UploadId && b.BlockSize == 2048);
    }

    #endregion

}