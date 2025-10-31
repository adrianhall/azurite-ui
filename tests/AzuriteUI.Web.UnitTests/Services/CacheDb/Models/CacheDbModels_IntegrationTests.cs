using AzuriteUI.Web.Services.Azurite.Models;
using AzuriteUI.Web.Services.CacheDb;
using AzuriteUI.Web.Services.CacheDb.Models;
using Microsoft.EntityFrameworkCore;

namespace AzuriteUI.Web.UnitTests.Services.CacheDb.Models;

/// <summary>
/// Integration tests for CacheDb models that verify CRUD operations,
/// navigation properties, and foreign key relationships.
/// </summary>
[ExcludeFromCodeCoverage]
public class CacheDbModels_IntegrationTests
{
    private static CacheDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CacheDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        var context = new CacheDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    #region ContainerModel Tests

    [Fact(Timeout = 15000)]
    public async Task ContainerModel_CanBeStoredAndRetrieved()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var container = new ContainerModel
        {
            Name = "test-container",
            ETag = "etag123",
            DefaultEncryptionScope = "default-scope",
            HasImmutabilityPolicy = true,
            HasImmutableStorageWithVersioning = false,
            PublicAccess = AzuritePublicAccess.Blob,
            PreventEncryptionScopeOverride = true,
            TotalSize = 1024,
            BlobCount = 5,
            LastModified = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, string> { { "key1", "value1" } }
        };

        // Act - Store
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        // Clear to ensure we're loading from database
        context.ChangeTracker.Clear();

        // Act - Retrieve
        var retrieved = await context.Containers
            .FirstOrDefaultAsync(c => c.Name == "test-container");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("test-container");
        retrieved.ETag.Should().Be("etag123");
        retrieved.DefaultEncryptionScope.Should().Be("default-scope");
        retrieved.HasImmutabilityPolicy.Should().BeTrue();
        retrieved.HasImmutableStorageWithVersioning.Should().BeFalse();
        retrieved.PublicAccess.Should().Be(AzuritePublicAccess.Blob);
        retrieved.PreventEncryptionScopeOverride.Should().BeTrue();
        retrieved.TotalSize.Should().Be(1024);
        retrieved.BlobCount.Should().Be(5);
        retrieved.Metadata.Should().ContainKey("key1");
        retrieved.Metadata["key1"].Should().Be("value1");
    }

    [Fact(Timeout = 15000)]
    public async Task ContainerModel_CanBeDeleted()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var container = new ContainerModel
        {
            Name = "test-container",
            ETag = "etag123"
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        // Act
        context.Containers.Remove(container);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await context.Containers
            .FirstOrDefaultAsync(c => c.Name == "test-container");
        retrieved.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public async Task ContainerModel_WithBlobs_LoadsNavigationProperty()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var container = new ContainerModel
        {
            Name = "test-container",
            ETag = "etag123"
        };
        var blob1 = new BlobModel
        {
            Name = "blob1.txt",
            ContainerName = "test-container",
            ETag = "blob-etag1",
            ContentLength = 100
        };
        var blob2 = new BlobModel
        {
            Name = "blob2.txt",
            ContainerName = "test-container",
            ETag = "blob-etag2",
            ContentLength = 200
        };

        context.Containers.Add(container);
        context.Blobs.AddRange(blob1, blob2);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act
        var retrieved = await context.Containers
            .Include(c => c.Blobs)
            .FirstOrDefaultAsync(c => c.Name == "test-container");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Blobs.Should().HaveCount(2);
        retrieved.Blobs.Should().Contain(b => b.Name == "blob1.txt");
        retrieved.Blobs.Should().Contain(b => b.Name == "blob2.txt");
    }

    #endregion

    #region BlobModel Tests

    [Fact(Timeout = 15000)]
    public async Task BlobModel_CanBeStoredAndRetrieved()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var container = new ContainerModel
        {
            Name = "test-container",
            ETag = "container-etag"
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var blob = new BlobModel
        {
            Name = "test-blob.txt",
            ContainerName = "test-container",
            ETag = "blob-etag",
            BlobType = AzuriteBlobType.Block,
            ContentEncoding = "gzip",
            ContentLanguage = "en-US",
            ContentLength = 1024,
            ContentType = "text/plain",
            CreatedOn = DateTimeOffset.UtcNow,
            ExpiresOn = DateTimeOffset.UtcNow.AddDays(7),
            LastAccessedOn = DateTimeOffset.UtcNow,
            LastModified = DateTimeOffset.UtcNow,
            Tags = new Dictionary<string, string> { { "tag1", "value1" } },
            Metadata = new Dictionary<string, string> { { "meta1", "metavalue1" } },
            HasLegalHold = true,
            RemainingRetentionDays = 30
        };

        // Act - Store
        context.Blobs.Add(blob);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act - Retrieve
        var retrieved = await context.Blobs
            .FirstOrDefaultAsync(b => b.Name == "test-blob.txt" && b.ContainerName == "test-container");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("test-blob.txt");
        retrieved.ContainerName.Should().Be("test-container");
        retrieved.ETag.Should().Be("blob-etag");
        retrieved.BlobType.Should().Be(AzuriteBlobType.Block);
        retrieved.ContentEncoding.Should().Be("gzip");
        retrieved.ContentLanguage.Should().Be("en-US");
        retrieved.ContentLength.Should().Be(1024);
        retrieved.ContentType.Should().Be("text/plain");
        retrieved.ExpiresOn.Should().NotBeNull();
        retrieved.LastAccessedOn.Should().NotBeNull();
        retrieved.Tags.Should().ContainKey("tag1");
        retrieved.Tags["tag1"].Should().Be("value1");
        retrieved.Metadata.Should().ContainKey("meta1");
        retrieved.Metadata["meta1"].Should().Be("metavalue1");
        retrieved.HasLegalHold.Should().BeTrue();
        retrieved.RemainingRetentionDays.Should().Be(30);
    }

    [Fact(Timeout = 15000)]
    public async Task BlobModel_CanBeDeleted()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var container = new ContainerModel
        {
            Name = "test-container",
            ETag = "container-etag"
        };
        var blob = new BlobModel
        {
            Name = "test-blob.txt",
            ContainerName = "test-container",
            ETag = "blob-etag"
        };
        context.Containers.Add(container);
        context.Blobs.Add(blob);
        await context.SaveChangesAsync();

        // Act
        context.Blobs.Remove(blob);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await context.Blobs
            .FirstOrDefaultAsync(b => b.Name == "test-blob.txt");
        retrieved.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public async Task BlobModel_WithContainer_LoadsNavigationProperty()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var container = new ContainerModel
        {
            Name = "test-container",
            ETag = "container-etag",
            PublicAccess = AzuritePublicAccess.Container
        };
        var blob = new BlobModel
        {
            Name = "test-blob.txt",
            ContainerName = "test-container",
            ETag = "blob-etag"
        };
        context.Containers.Add(container);
        context.Blobs.Add(blob);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act
        var retrieved = await context.Blobs
            .Include(b => b.Container)
            .FirstOrDefaultAsync(b => b.Name == "test-blob.txt");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Container.Should().NotBeNull();
        retrieved.Container!.Name.Should().Be("test-container");
        retrieved.Container.PublicAccess.Should().Be(AzuritePublicAccess.Container);
    }

    [Fact(Timeout = 15000)]
    public async Task BlobModel_ForeignKey_EnforcesContainerExists()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var blob = new BlobModel
        {
            Name = "orphan-blob.txt",
            ContainerName = "non-existent-container",
            ETag = "blob-etag"
        };
        context.Blobs.Add(blob);

        // Act & Assert
        var act = async () => await context.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    #endregion

    #region UploadModel Tests

    [Fact(Timeout = 15000)]
    public async Task UploadModel_CanBeStoredAndRetrieved()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var uploadId = Guid.NewGuid();
        var upload = new UploadModel
        {
            UploadId = uploadId,
            ContainerName = "test-container",
            BlobName = "test-blob.bin",
            ContentLength = 5242880, // 5 MB
            ContentType = "application/octet-stream",
            ContentEncoding = "gzip",
            ContentLanguage = "en-US",
            Metadata = new Dictionary<string, string>
            {
                { "uploader", "test-user" },
                { "purpose", "testing" }
            },
            Tags = new Dictionary<string, string>
            {
                { "environment", "test" },
                { "version", "1.0" }
            },
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
            LastActivityAt = DateTimeOffset.UtcNow
        };

        // Act - Store
        context.Uploads.Add(upload);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act - Retrieve
        var retrieved = await context.Uploads
            .FirstOrDefaultAsync(u => u.UploadId == uploadId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.UploadId.Should().Be(uploadId);
        retrieved.ContainerName.Should().Be("test-container");
        retrieved.BlobName.Should().Be("test-blob.bin");
        retrieved.ContentLength.Should().Be(5242880);
        retrieved.ContentType.Should().Be("application/octet-stream");
        retrieved.ContentEncoding.Should().Be("gzip");
        retrieved.ContentLanguage.Should().Be("en-US");
        retrieved.Metadata.Should().HaveCount(2);
        retrieved.Metadata["uploader"].Should().Be("test-user");
        retrieved.Tags.Should().HaveCount(2);
        retrieved.Tags["environment"].Should().Be("test");
    }

    [Fact(Timeout = 15000)]
    public async Task UploadModel_CanBeDeleted()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var uploadId = Guid.NewGuid();
        var upload = new UploadModel
        {
            UploadId = uploadId,
            ContainerName = "test-container",
            BlobName = "test-blob.bin"
        };
        context.Uploads.Add(upload);
        await context.SaveChangesAsync();

        // Act
        context.Uploads.Remove(upload);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await context.Uploads
            .FirstOrDefaultAsync(u => u.UploadId == uploadId);
        retrieved.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public async Task UploadModel_WithBlocks_LoadsNavigationProperty()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var uploadId = Guid.NewGuid();
        var upload = new UploadModel
        {
            UploadId = uploadId,
            ContainerName = "test-container",
            BlobName = "test-blob.bin"
        };
        var block1 = new UploadBlockModel
        {
            UploadId = uploadId,
            BlockId = "block-001",
            BlockSize = 1048576, // 1 MB
            ContentMD5 = "abc123"
        };
        var block2 = new UploadBlockModel
        {
            UploadId = uploadId,
            BlockId = "block-002",
            BlockSize = 2097152, // 2 MB
            ContentMD5 = "def456"
        };

        context.Uploads.Add(upload);
        context.UploadBlocks.AddRange(block1, block2);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act
        var retrieved = await context.Uploads
            .Include(u => u.Blocks)
            .FirstOrDefaultAsync(u => u.UploadId == uploadId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Blocks.Should().HaveCount(2);
        retrieved.Blocks.Should().Contain(b => b.BlockId == "block-001");
        retrieved.Blocks.Should().Contain(b => b.BlockId == "block-002");
        retrieved.Blocks.Sum(b => b.BlockSize).Should().Be(3145728); // Total 3 MB
    }

    [Fact(Timeout = 15000)]
    public async Task UploadModel_Deletion_CascadesToBlocksIfConfigured()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var uploadId = Guid.NewGuid();
        var upload = new UploadModel
        {
            UploadId = uploadId,
            ContainerName = "test-container",
            BlobName = "test-blob.bin"
        };
        var block = new UploadBlockModel
        {
            UploadId = uploadId,
            BlockId = "block-001",
            BlockSize = 1048576
        };

        context.Uploads.Add(upload);
        context.UploadBlocks.Add(block);
        await context.SaveChangesAsync();

        // Act
        context.Uploads.Remove(upload);
        await context.SaveChangesAsync();

        // Assert
        var retrievedBlocks = await context.UploadBlocks
            .Where(b => b.UploadId == uploadId)
            .ToListAsync();
        retrievedBlocks.Should().BeEmpty("blocks should be cascade deleted with upload");
    }

    #endregion

    #region UploadBlockModel Tests

    [Fact(Timeout = 15000)]
    public async Task UploadBlockModel_CanBeStoredAndRetrieved()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var uploadId = Guid.NewGuid();
        var upload = new UploadModel
        {
            UploadId = uploadId,
            ContainerName = "test-container",
            BlobName = "test-blob.bin"
        };
        context.Uploads.Add(upload);
        await context.SaveChangesAsync();

        var block = new UploadBlockModel
        {
            UploadId = uploadId,
            BlockId = "YmxvY2stMDAx", // Base64 encoded "block-001"
            BlockSize = 4194304, // 4 MB
            ContentMD5 = "md5hash123",
            UploadedAt = DateTimeOffset.UtcNow
        };

        // Act - Store
        context.UploadBlocks.Add(block);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act - Retrieve
        var retrieved = await context.UploadBlocks
            .FirstOrDefaultAsync(b => b.UploadId == uploadId && b.BlockId == "YmxvY2stMDAx");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().BeGreaterThan(0);
        retrieved.UploadId.Should().Be(uploadId);
        retrieved.BlockId.Should().Be("YmxvY2stMDAx");
        retrieved.BlockSize.Should().Be(4194304);
        retrieved.ContentMD5.Should().Be("md5hash123");
        retrieved.UploadedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact(Timeout = 15000)]
    public async Task UploadBlockModel_CanBeDeleted()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var uploadId = Guid.NewGuid();
        var upload = new UploadModel
        {
            UploadId = uploadId,
            ContainerName = "test-container",
            BlobName = "test-blob.bin"
        };
        var block = new UploadBlockModel
        {
            UploadId = uploadId,
            BlockId = "block-001",
            BlockSize = 1024
        };
        context.Uploads.Add(upload);
        context.UploadBlocks.Add(block);
        await context.SaveChangesAsync();

        // Act
        context.UploadBlocks.Remove(block);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await context.UploadBlocks
            .FirstOrDefaultAsync(b => b.UploadId == uploadId);
        retrieved.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public async Task UploadBlockModel_WithUpload_LoadsNavigationProperty()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var uploadId = Guid.NewGuid();
        var upload = new UploadModel
        {
            UploadId = uploadId,
            ContainerName = "test-container",
            BlobName = "test-blob.bin",
            ContentType = "application/json"
        };
        var block = new UploadBlockModel
        {
            UploadId = uploadId,
            BlockId = "block-001",
            BlockSize = 1024
        };
        context.Uploads.Add(upload);
        context.UploadBlocks.Add(block);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act
        var retrieved = await context.UploadBlocks
            .Include(b => b.Upload)
            .FirstOrDefaultAsync(b => b.BlockId == "block-001");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Upload.Should().NotBeNull();
        retrieved.Upload!.UploadId.Should().Be(uploadId);
        retrieved.Upload.BlobName.Should().Be("test-blob.bin");
        retrieved.Upload.ContentType.Should().Be("application/json");
    }

    [Fact(Timeout = 15000)]
    public async Task UploadBlockModel_ForeignKey_EnforcesUploadExists()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var block = new UploadBlockModel
        {
            UploadId = Guid.NewGuid(), // Non-existent upload
            BlockId = "orphan-block",
            BlockSize = 1024
        };
        context.UploadBlocks.Add(block);

        // Act & Assert
        var act = async () => await context.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    #endregion

    #region Complex Relationship Tests

    [Fact(Timeout = 15000)]
    public async Task ComplexScenario_ContainerWithMultipleBlobsAndUploads()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var container = new ContainerModel
        {
            Name = "test-container",
            ETag = "container-etag"
        };

        var blob1 = new BlobModel
        {
            Name = "blob1.txt",
            ContainerName = "test-container",
            ETag = "blob1-etag",
            ContentLength = 100
        };

        var blob2 = new BlobModel
        {
            Name = "blob2.txt",
            ContainerName = "test-container",
            ETag = "blob2-etag",
            ContentLength = 200
        };

        var uploadId = Guid.NewGuid();
        var upload = new UploadModel
        {
            UploadId = uploadId,
            ContainerName = "test-container",
            BlobName = "uploading-blob.bin"
        };

        var block1 = new UploadBlockModel
        {
            UploadId = uploadId,
            BlockId = "block-001",
            BlockSize = 512
        };

        var block2 = new UploadBlockModel
        {
            UploadId = uploadId,
            BlockId = "block-002",
            BlockSize = 512
        };

        // Act
        context.Containers.Add(container);
        context.Blobs.AddRange(blob1, blob2);
        context.Uploads.Add(upload);
        context.UploadBlocks.AddRange(block1, block2);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Assert - Verify all entities were stored
        var retrievedContainer = await context.Containers
            .Include(c => c.Blobs)
            .FirstOrDefaultAsync(c => c.Name == "test-container");

        retrievedContainer.Should().NotBeNull();
        retrievedContainer!.Blobs.Should().HaveCount(2);

        var retrievedUpload = await context.Uploads
            .Include(u => u.Blocks)
            .FirstOrDefaultAsync(u => u.UploadId == uploadId);

        retrievedUpload.Should().NotBeNull();
        retrievedUpload!.Blocks.Should().HaveCount(2);
    }

    #endregion
}
