using AzuriteUI.Web.Services.Azurite;
using AzuriteUI.Web.Services.Azurite.Exceptions;
using AzuriteUI.Web.Services.Azurite.Models;
using AzuriteUI.Web.Services.CacheDb;
using AzuriteUI.Web.Services.CacheDb.Models;
using AzuriteUI.Web.Services.Repositories;
using AzuriteUI.Web.Services.Repositories.Models;
using AzuriteUI.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Runtime.InteropServices;

namespace AzuriteUI.Web.UnitTests.Services.Repositories;

[ExcludeFromCodeCoverage]
public class StorageRepository_Tests : SqliteDbTests
{
    private readonly IAzuriteService _mockAzuriteService;
    private readonly ILogger<StorageRepository> _mockLogger;

    public StorageRepository_Tests()
    {
        _mockAzuriteService = Substitute.For<IAzuriteService>();
        _mockLogger = Substitute.For<ILogger<StorageRepository>>();
    }

    private StorageRepository CreateRepository(CacheDbContext context)
    {
        return new StorageRepository(context, _mockAzuriteService, _mockLogger);
    }

    #region Blobs Property Tests

    [Fact(Timeout = 15000)]
    public async Task Blobs_WithNoBlobs_ShouldReturnEmptyQueryable()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);

        // Act
        var result = await repository.Blobs.ToListAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task Blobs_WithSingleBlob_ShouldReturnMappedDTO()
    {
        // Arrange
        using var context = CreateDbContext();
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "cont-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var blob = new BlobModel
        {
            Name = "test-blob.txt",
            ContainerName = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "blob-etag",
            LastModified = DateTimeOffset.UtcNow,
            BlobType = AzuriteBlobType.Block,
            ContentEncoding = "gzip",
            ContentLanguage = "en-us",
            ContentLength = 2048,
            ContentType = "text/plain",
            CreatedOn = DateTimeOffset.UtcNow.AddHours(-2),
            ExpiresOn = DateTimeOffset.UtcNow.AddDays(7),
            HasLegalHold = true,
            LastAccessedOn = DateTimeOffset.UtcNow.AddHours(-1),
            Metadata = new Dictionary<string, string> { ["author"] = "test" },
            Tags = new Dictionary<string, string> { ["category"] = "test" },
            RemainingRetentionDays = 30
        };
        context.Blobs.Add(blob);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act
        var result = await repository.Blobs.SingleAsync();

        // Assert
        result.Name.Should().Be("test-blob.txt");
        result.ContainerName.Should().Be("test-container");
        result.ETag.Should().Be("blob-etag");
        result.LastModified.Should().BeCloseTo(blob.LastModified, TimeSpan.FromMilliseconds(100));
        result.BlobType.Should().Be("block");
        result.ContentEncoding.Should().Be("gzip");
        result.ContentLanguage.Should().Be("en-us");
        result.ContentLength.Should().Be(2048);
        result.ContentType.Should().Be("text/plain");
        result.CreatedOn.Should().BeCloseTo(blob.CreatedOn, TimeSpan.FromMilliseconds(100));
        result.ExpiresOn.Should().BeCloseTo(blob.ExpiresOn.Value, TimeSpan.FromMilliseconds(100));
        result.HasLegalHold.Should().BeTrue();
        result.LastAccessedOn.Should().BeCloseTo(blob.LastAccessedOn.Value, TimeSpan.FromMilliseconds(100));
        result.Metadata.Should().ContainKey("author").WhoseValue.Should().Be("test");
        result.Tags.Should().ContainKey("category").WhoseValue.Should().Be("test");
        result.RemainingRetentionDays.Should().Be(30);
    }

    [Fact(Timeout = 15000)]
    public async Task Blobs_WithMultipleBlobs_ShouldReturnAllMappedDTOs()
    {
        // Arrange
        using var context = CreateDbContext();
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "cont-etag",
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
            LastModified = DateTimeOffset.UtcNow
        };
        var blob2 = new BlobModel
        {
            Name = "blob-2.txt",
            ContainerName = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "etag-2",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Blobs.AddRange(blob1, blob2);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act
        var result = await repository.Blobs.ToListAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(b => b.Name == "blob-1.txt" && b.ETag == "etag-1");
        result.Should().Contain(b => b.Name == "blob-2.txt" && b.ETag == "etag-2");
    }

    [Fact(Timeout = 15000)]
    public async Task Blobs_ShouldConvertBlobTypeEnumToLowercase()
    {
        // Arrange
        using var context = CreateDbContext();
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "cont-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var blobBlock = new BlobModel
        {
            Name = "blob-block.txt",
            ContainerName = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "etag-1",
            LastModified = DateTimeOffset.UtcNow,
            BlobType = AzuriteBlobType.Block
        };
        var blobAppend = new BlobModel
        {
            Name = "blob-append.txt",
            ContainerName = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "etag-2",
            LastModified = DateTimeOffset.UtcNow,
            BlobType = AzuriteBlobType.Append
        };
        var blobPage = new BlobModel
        {
            Name = "blob-page.txt",
            ContainerName = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "etag-3",
            LastModified = DateTimeOffset.UtcNow,
            BlobType = AzuriteBlobType.Page
        };
        context.Blobs.AddRange(blobBlock, blobAppend, blobPage);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act
        var result = await repository.Blobs.ToListAsync();

        // Assert
        result.Should().Contain(b => b.Name == "blob-block.txt" && b.BlobType == "block");
        result.Should().Contain(b => b.Name == "blob-append.txt" && b.BlobType == "append");
        result.Should().Contain(b => b.Name == "blob-page.txt" && b.BlobType == "page");
    }

    #endregion

    #region DeleteBlobAsync Tests

    [Fact(Timeout = 15000)]
    public async Task DeleteBlobAsync_WithExistingBlob_ShouldDeleteFromAzuriteAndCache()
    {
        // Arrange
        using var context = CreateDbContext();
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "cont-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var blob = new BlobModel
        {
            Name = "blob-to-delete.txt",
            ContainerName = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "blob-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Blobs.Add(blob);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        _mockAzuriteService.DeleteBlobAsync("test-container", "blob-to-delete.txt", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await repository.DeleteBlobAsync("test-container", "blob-to-delete.txt", CancellationToken.None);

        // Assert
        var cached = await context.Blobs.FirstOrDefaultAsync(b => b.ContainerName == "test-container" && b.Name == "blob-to-delete.txt");
        cached.Should().BeNull();

        await _mockAzuriteService.Received(1).DeleteBlobAsync("test-container", "blob-to-delete.txt", Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 15000)]
    public async Task DeleteBlobAsync_WhenAzuriteFails_ShouldPropagateExceptionAndNotDeleteFromCache()
    {
        // Arrange
        using var context = CreateDbContext();
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "cont-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var blob = new BlobModel
        {
            Name = "blob-to-delete.txt",
            ContainerName = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "blob-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Blobs.Add(blob);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        _mockAzuriteService.DeleteBlobAsync("test-container", "blob-to-delete.txt", Arg.Any<CancellationToken>())
            .ThrowsAsync(new AzuriteServiceException("Delete failed"));

        // Act
        var act = async () => await repository.DeleteBlobAsync("test-container", "blob-to-delete.txt", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AzuriteServiceException>();

        // Verify blob still exists in cache
        var cached = await context.Blobs.FirstOrDefaultAsync(b => b.ContainerName == "test-container" && b.Name == "blob-to-delete.txt");
        cached.Should().NotBeNull();
    }

    [Fact(Timeout = 15000)]
    public async Task DeleteBlobAsync_WithNonExistentBlob_ShouldCallAzuriteAndCacheRemoval()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);

        _mockAzuriteService.DeleteBlobAsync("test-container", "non-existent.txt", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await repository.DeleteBlobAsync("test-container", "non-existent.txt", CancellationToken.None);

        // Assert
        await _mockAzuriteService.Received(1).DeleteBlobAsync("test-container", "non-existent.txt", Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 15000)]
    public async Task DeleteBlobAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockAzuriteService.DeleteBlobAsync("test-container", "test-blob.txt", Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var act = async () => await repository.DeleteBlobAsync("test-container", "test-blob.txt", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region GetBlobAsync Tests

    [Fact(Timeout = 15000)]
    public async Task GetBlobAsync_WithExistingBlob_ShouldReturnBlobDTO()
    {
        // Arrange
        using var context = CreateDbContext();
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "cont-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var blob = new BlobModel
        {
            Name = "existing-blob.txt",
            ContainerName = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "blob-etag",
            LastModified = DateTimeOffset.UtcNow,
            ContentLength = 512,
            ContentType = "text/plain"
        };
        context.Blobs.Add(blob);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetBlobAsync("test-container", "existing-blob.txt", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("existing-blob.txt");
        result.ContainerName.Should().Be("test-container");
        result.ETag.Should().Be("blob-etag");
        result.ContentLength.Should().Be(512);
        result.ContentType.Should().Be("text/plain");
    }

    [Fact(Timeout = 15000)]
    public async Task GetBlobAsync_WithNonExistentBlob_ShouldReturnNull()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetBlobAsync("test-container", "non-existent.txt", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public async Task GetBlobAsync_WithMultipleBlobs_ShouldReturnCorrectOne()
    {
        // Arrange
        using var context = CreateDbContext();
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "cont-etag",
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
            LastModified = DateTimeOffset.UtcNow
        };
        var blob2 = new BlobModel
        {
            Name = "blob-2.txt",
            ContainerName = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "etag-2",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Blobs.AddRange(blob1, blob2);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetBlobAsync("test-container", "blob-2.txt", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("blob-2.txt");
        result.ETag.Should().Be("etag-2");
    }

    [Fact(Timeout = 15000)]
    public async Task GetBlobAsync_WithSameNameInDifferentContainers_ShouldReturnCorrectOne()
    {
        // Arrange
        using var context = CreateDbContext();
        var container1 = new ContainerModel
        {
            Name = "container-1",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "cont-etag-1",
            LastModified = DateTimeOffset.UtcNow
        };
        var container2 = new ContainerModel
        {
            Name = "container-2",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "cont-etag-2",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.AddRange(container1, container2);
        await context.SaveChangesAsync();

        var blob1 = new BlobModel
        {
            Name = "same-name.txt",
            ContainerName = "container-1",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "etag-1",
            LastModified = DateTimeOffset.UtcNow
        };
        var blob2 = new BlobModel
        {
            Name = "same-name.txt",
            ContainerName = "container-2",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "etag-2",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Blobs.AddRange(blob1, blob2);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetBlobAsync("container-2", "same-name.txt", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("same-name.txt");
        result.ContainerName.Should().Be("container-2");
        result.ETag.Should().Be("etag-2");
    }

    [Fact(Timeout = 15000)]
    public async Task GetBlobAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        using var context = CreateDbContext();
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "cont-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var blob = new BlobModel
        {
            Name = "test-blob.txt",
            ContainerName = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "blob-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Blobs.Add(blob);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await repository.GetBlobAsync("test-container", "test-blob.txt", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region UpdateBlobAsync Tests

    [Fact(Timeout = 15000)]
    public async Task UpdateBlobAsync_WithValidInput_ShouldUpdateAzuriteAndCache()
    {
        // Arrange
        using var context = CreateDbContext();
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "cont-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var existingBlob = new BlobModel
        {
            Name = "existing-blob.txt",
            ContainerName = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "old-etag",
            LastModified = DateTimeOffset.UtcNow.AddDays(-1),
            Metadata = new Dictionary<string, string> { ["old"] = "value" },
            Tags = new Dictionary<string, string> { ["old-tag"] = "value" }
        };
        context.Blobs.Add(existingBlob);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var updateDto = new BlobUpdateDTO
        {
            ContentEncoding = "gzip",
            ContentLanguage = "en-us",
            Metadata = new Dictionary<string, string> { ["new"] = "value" },
            Tags = new Dictionary<string, string> { ["new-tag"] = "value" }
        };

        var updatedAzuriteBlob = CreateBlobItem("existing-blob.txt", etag: "new-etag");
        updatedAzuriteBlob.ContentEncoding = "gzip";
        updatedAzuriteBlob.ContentLanguage = "en-us";
        updatedAzuriteBlob.Metadata = new Dictionary<string, string> { ["new"] = "value" };
        updatedAzuriteBlob.Tags = new Dictionary<string, string> { ["new-tag"] = "value" };

        _mockAzuriteService.UpdateBlobAsync("test-container", "existing-blob.txt", Arg.Any<AzuriteBlobProperties>(), Arg.Any<CancellationToken>())
            .Returns(updatedAzuriteBlob);

        // Act
        var result = await repository.UpdateBlobAsync("test-container", "existing-blob.txt", updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("existing-blob.txt");
        result.ContainerName.Should().Be("test-container");
        result.ETag.Should().Be("new-etag");
        result.ContentEncoding.Should().Be("gzip");
        result.ContentLanguage.Should().Be("en-us");
        result.Metadata.Should().ContainKey("new").WhoseValue.Should().Be("value");
        result.Tags.Should().ContainKey("new-tag").WhoseValue.Should().Be("value");

        // Verify Azurite service was called with correct properties
        await _mockAzuriteService.Received(1).UpdateBlobAsync(
            "test-container",
            "existing-blob.txt",
            Arg.Is<AzuriteBlobProperties>(p =>
                p.ContentEncoding == "gzip" &&
                p.ContentLanguage == "en-us" &&
                p.Metadata.ContainsKey("new") && p.Metadata["new"] == "value" &&
                p.Tags.ContainsKey("new-tag") && p.Tags["new-tag"] == "value"),
            Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 15000)]
    public async Task UpdateBlobAsync_WithEmptyMetadata_ShouldUpdateSuccessfully()
    {
        // Arrange
        using var context = CreateDbContext();
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "cont-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var existingBlob = new BlobModel
        {
            Name = "existing-blob.txt",
            ContainerName = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "old-etag",
            LastModified = DateTimeOffset.UtcNow.AddDays(-1),
            Metadata = new Dictionary<string, string> { ["old"] = "value" }
        };
        context.Blobs.Add(existingBlob);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var updateDto = new BlobUpdateDTO
        {
            ContentEncoding = string.Empty,
            ContentLanguage = string.Empty,
            Metadata = new Dictionary<string, string>(),
            Tags = new Dictionary<string, string>()
        };

        var updatedAzuriteBlob = CreateBlobItem("existing-blob.txt", etag: "new-etag");
        updatedAzuriteBlob.Metadata = new Dictionary<string, string>();
        updatedAzuriteBlob.Tags = new Dictionary<string, string>();

        _mockAzuriteService.UpdateBlobAsync("test-container", "existing-blob.txt", Arg.Any<AzuriteBlobProperties>(), Arg.Any<CancellationToken>())
            .Returns(updatedAzuriteBlob);

        // Act
        var result = await repository.UpdateBlobAsync("test-container", "existing-blob.txt", updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Metadata.Should().BeEmpty();
        result.Tags.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task UpdateBlobAsync_WhenAzuriteFails_ShouldPropagateExceptionAndNotUpdateCache()
    {
        // Arrange
        using var context = CreateDbContext();
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "cont-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var existingBlob = new BlobModel
        {
            Name = "existing-blob.txt",
            ContainerName = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "old-etag",
            LastModified = DateTimeOffset.UtcNow.AddDays(-1)
        };
        context.Blobs.Add(existingBlob);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var updateDto = new BlobUpdateDTO
        {
            Metadata = new Dictionary<string, string> { ["new"] = "value" },
            Tags = new Dictionary<string, string>()
        };

        _mockAzuriteService.UpdateBlobAsync("test-container", "existing-blob.txt", Arg.Any<AzuriteBlobProperties>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AzuriteServiceException("Update failed"));

        // Act
        var act = async () => await repository.UpdateBlobAsync("test-container", "existing-blob.txt", updateDto, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AzuriteServiceException>();

        // Verify cache still has old values
        var cached = await context.Blobs.FirstAsync(b => b.ContainerName == "test-container" && b.Name == "existing-blob.txt");
        cached.ETag.Should().Be("old-etag");
    }

    [Fact(Timeout = 15000)]
    public async Task UpdateBlobAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var updateDto = new BlobUpdateDTO
        {
            Metadata = new Dictionary<string, string>(),
            Tags = new Dictionary<string, string>()
        };

        _mockAzuriteService.UpdateBlobAsync("test-container", "test-blob.txt", Arg.Any<AzuriteBlobProperties>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var act = async () => await repository.UpdateBlobAsync("test-container", "test-blob.txt", updateDto, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Containers Property Tests

    [Fact(Timeout = 15000)]
    public async Task Containers_WithNoContainers_ShouldReturnEmptyQueryable()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);

        // Act
        var result = await repository.Containers.ToListAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task Containers_WithSingleContainer_ShouldReturnMappedDTO()
    {
        // Arrange
        using var context = CreateDbContext();
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "test-etag",
            LastModified = DateTimeOffset.UtcNow,
            BlobCount = 5,
            TotalSize = 1024,
            DefaultEncryptionScope = "test-scope",
            HasImmutabilityPolicy = true,
            HasImmutableStorageWithVersioning = true,
            HasLegalHold = true,
            Metadata = new Dictionary<string, string> { ["key1"] = "value1" },
            PreventEncryptionScopeOverride = true,
            PublicAccess = AzuritePublicAccess.Container,
            RemainingRetentionDays = 30
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act
        var result = await repository.Containers.SingleAsync();

        // Assert
        result.Name.Should().Be("test-container");
        result.ETag.Should().Be("test-etag");
        result.LastModified.Should().BeCloseTo(container.LastModified, TimeSpan.FromMilliseconds(100));
        result.BlobCount.Should().Be(5);
        result.TotalSize.Should().Be(1024);
        result.DefaultEncryptionScope.Should().Be("test-scope");
        result.HasImmutabilityPolicy.Should().BeTrue();
        result.HasImmutableStorageWithVersioning.Should().BeTrue();
        result.HasLegalHold.Should().BeTrue();
        result.Metadata.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        result.PreventEncryptionScopeOverride.Should().BeTrue();
        result.PublicAccess.Should().Be("container");
        result.RemainingRetentionDays.Should().Be(30);
    }

    [Fact(Timeout = 15000)]
    public async Task Containers_WithMultipleContainers_ShouldReturnAllMappedDTOs()
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

        var repository = CreateRepository(context);

        // Act
        var result = await repository.Containers.ToListAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Name == "container-1" && c.ETag == "etag-1");
        result.Should().Contain(c => c.Name == "container-2" && c.ETag == "etag-2");
    }

    [Fact(Timeout = 15000)]
    public async Task Containers_ShouldConvertPublicAccessEnumToLowercase()
    {
        // Arrange
        using var context = CreateDbContext();
        var containerNone = new ContainerModel
        {
            Name = "container-none",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "etag-1",
            LastModified = DateTimeOffset.UtcNow,
            PublicAccess = AzuritePublicAccess.None
        };
        var containerBlob = new ContainerModel
        {
            Name = "container-blob",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "etag-2",
            LastModified = DateTimeOffset.UtcNow,
            PublicAccess = AzuritePublicAccess.Blob
        };
        var containerContainer = new ContainerModel
        {
            Name = "container-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "etag-3",
            LastModified = DateTimeOffset.UtcNow,
            PublicAccess = AzuritePublicAccess.Container
        };
        context.Containers.AddRange(containerNone, containerBlob, containerContainer);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act
        var result = await repository.Containers.ToListAsync();

        // Assert
        result.Should().Contain(c => c.Name == "container-none" && c.PublicAccess == "none");
        result.Should().Contain(c => c.Name == "container-blob" && c.PublicAccess == "blob");
        result.Should().Contain(c => c.Name == "container-container" && c.PublicAccess == "container");
    }

    #endregion

    #region CreateContainerAsync Tests

    [Fact(Timeout = 15000)]
    public async Task CreateContainerAsync_WithValidInput_ShouldCreateContainerInAzuriteAndCache()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);

        var updateDto = new ContainerUpdateDTO
        {
            PublicAccess = "blob",
            DefaultEncryptionScope = "test-scope",
            PreventEncryptionScopeOverride = true,
            Metadata = new Dictionary<string, string> { ["key"] = "value" }
        };

        var azuriteContainer = CreateContainerItem("new-container");
        azuriteContainer.PublicAccess = AzuritePublicAccess.Blob;
        azuriteContainer.DefaultEncryptionScope = "test-scope";
        azuriteContainer.PreventEncryptionScopeOverride = true;
        azuriteContainer.Metadata = new Dictionary<string, string> { ["key"] = "value" };

        _mockAzuriteService.CreateContainerAsync("new-container", Arg.Any<AzuriteContainerProperties>(), Arg.Any<CancellationToken>())
            .Returns(azuriteContainer);

        // Act
        var result = await repository.CreateContainerAsync("new-container", updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("new-container");
        result.PublicAccess.Should().Be("blob");
        result.DefaultEncryptionScope.Should().Be("test-scope");
        result.PreventEncryptionScopeOverride.Should().BeTrue();
        result.Metadata.Should().ContainKey("key").WhoseValue.Should().Be("value");

        // Verify it was saved to cache
        var cached = await context.Containers.FirstOrDefaultAsync(c => c.Name == "new-container");
        cached.Should().NotBeNull();

        // Verify Azurite service was called with correct properties
        await _mockAzuriteService.Received(1).CreateContainerAsync(
            "new-container",
            Arg.Is<AzuriteContainerProperties>(p =>
                p.PublicAccessType == AzuritePublicAccess.Blob &&
                p.DefaultEncryptionScope == "test-scope" &&
                p.PreventEncryptionScopeOverride == true &&
                p.Metadata.ContainsKey("key") && p.Metadata["key"] == "value"),
            Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 15000)]
    public async Task CreateContainerAsync_WithNullPublicAccess_ShouldNotSetPublicAccessType()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);

        var updateDto = new ContainerUpdateDTO
        {
            PublicAccess = null!,
            Metadata = new Dictionary<string, string>()
        };

        var azuriteContainer = CreateContainerItem("new-container");

        _mockAzuriteService.CreateContainerAsync("new-container", Arg.Any<AzuriteContainerProperties>(), Arg.Any<CancellationToken>())
            .Returns(azuriteContainer);

        // Act
        var result = await repository.CreateContainerAsync("new-container", updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        // Verify Azurite service was called with null PublicAccessType
        await _mockAzuriteService.Received(1).CreateContainerAsync(
            "new-container",
            Arg.Is<AzuriteContainerProperties>(p => p.PublicAccessType == null),
            Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 15000)]
    public async Task CreateContainerAsync_WithEmptyMetadata_ShouldCreateSuccessfully()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);

        var updateDto = new ContainerUpdateDTO
        {
            PublicAccess = "none",
            Metadata = new Dictionary<string, string>()
        };

        var azuriteContainer = CreateContainerItem("new-container");

        _mockAzuriteService.CreateContainerAsync("new-container", Arg.Any<AzuriteContainerProperties>(), Arg.Any<CancellationToken>())
            .Returns(azuriteContainer);

        // Act
        var result = await repository.CreateContainerAsync("new-container", updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Metadata.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task CreateContainerAsync_WhenAzuriteFails_ShouldPropagateException()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);

        var updateDto = new ContainerUpdateDTO
        {
            PublicAccess = "none",
            Metadata = new Dictionary<string, string>()
        };

        _mockAzuriteService.CreateContainerAsync("new-container", Arg.Any<AzuriteContainerProperties>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AzuriteServiceException("Container creation failed"));

        // Act
        var act = async () => await repository.CreateContainerAsync("new-container", updateDto, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AzuriteServiceException>();

        // Verify nothing was saved to cache
        var cached = await context.Containers.FirstOrDefaultAsync(c => c.Name == "new-container");
        cached.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public async Task CreateContainerAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var updateDto = new ContainerUpdateDTO
        {
            PublicAccess = "none",
            Metadata = new Dictionary<string, string>()
        };

        _mockAzuriteService.CreateContainerAsync("new-container", Arg.Any<AzuriteContainerProperties>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var act = async () => await repository.CreateContainerAsync("new-container", updateDto, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region DeleteContainerAsync Tests

    [Fact(Timeout = 15000)]
    public async Task DeleteContainerAsync_WithExistingContainer_ShouldDeleteFromAzuriteAndCache()
    {
        // Arrange
        using var context = CreateDbContext();
        var container = new ContainerModel
        {
            Name = "container-to-delete",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "test-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        _mockAzuriteService.DeleteContainerAsync("container-to-delete", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await repository.DeleteContainerAsync("container-to-delete", CancellationToken.None);

        // Assert
        var cached = await context.Containers.FirstOrDefaultAsync(c => c.Name == "container-to-delete");
        cached.Should().BeNull();

        await _mockAzuriteService.Received(1).DeleteContainerAsync("container-to-delete", Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 15000)]
    public async Task DeleteContainerAsync_WhenAzuriteFails_ShouldPropagateExceptionAndNotDeleteFromCache()
    {
        // Arrange
        using var context = CreateDbContext();
        var container = new ContainerModel
        {
            Name = "container-to-delete",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "test-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        _mockAzuriteService.DeleteContainerAsync("container-to-delete", Arg.Any<CancellationToken>())
            .ThrowsAsync(new AzuriteServiceException("Delete failed"));

        // Act
        var act = async () => await repository.DeleteContainerAsync("container-to-delete", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AzuriteServiceException>();

        // Verify container still exists in cache
        var cached = await context.Containers.FirstOrDefaultAsync(c => c.Name == "container-to-delete");
        cached.Should().NotBeNull();
    }

    [Fact(Timeout = 15000)]
    public async Task DeleteContainerAsync_WithNonExistentContainer_ShouldCallAzuriteAndCacheRemoval()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);

        _mockAzuriteService.DeleteContainerAsync("non-existent", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await repository.DeleteContainerAsync("non-existent", CancellationToken.None);

        // Assert
        await _mockAzuriteService.Received(1).DeleteContainerAsync("non-existent", Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 15000)]
    public async Task DeleteContainerAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockAzuriteService.DeleteContainerAsync("test-container", Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var act = async () => await repository.DeleteContainerAsync("test-container", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region GetContainerAsync Tests

    [Fact(Timeout = 15000)]
    public async Task GetContainerAsync_WithExistingContainer_ShouldReturnContainerDTO()
    {
        // Arrange
        using var context = CreateDbContext();
        var container = new ContainerModel
        {
            Name = "existing-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "test-etag",
            LastModified = DateTimeOffset.UtcNow,
            BlobCount = 10,
            TotalSize = 2048,
            PublicAccess = AzuritePublicAccess.Blob
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetContainerAsync("existing-container", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("existing-container");
        result.ETag.Should().Be("test-etag");
        result.BlobCount.Should().Be(10);
        result.TotalSize.Should().Be(2048);
        result.PublicAccess.Should().Be("blob");
    }

    [Fact(Timeout = 15000)]
    public async Task GetContainerAsync_WithNonExistentContainer_ShouldReturnNull()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetContainerAsync("non-existent", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public async Task GetContainerAsync_WithMultipleContainers_ShouldReturnCorrectOne()
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

        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetContainerAsync("container-2", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("container-2");
        result.ETag.Should().Be("etag-2");
    }

    [Fact(Timeout = 15000)]
    public async Task GetContainerAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        using var context = CreateDbContext();
        var container = new ContainerModel
        {
            Name = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "test-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await repository.GetContainerAsync("test-container", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region UpdateContainerAsync Tests

    [Fact(Timeout = 15000)]
    public async Task UpdateContainerAsync_WithValidInput_ShouldUpdateAzuriteAndCache()
    {
        // Arrange
        using var context = CreateDbContext();
        var existingContainer = new ContainerModel
        {
            Name = "existing-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "old-etag",
            LastModified = DateTimeOffset.UtcNow.AddDays(-1),
            Metadata = new Dictionary<string, string> { ["old"] = "value" }
        };
        context.Containers.Add(existingContainer);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var updateDto = new ContainerUpdateDTO
        {
            Metadata = new Dictionary<string, string> { ["new"] = "value" }
        };

        var updatedAzuriteContainer = CreateContainerItem("existing-container", etag: "new-etag");
        updatedAzuriteContainer.Metadata = new Dictionary<string, string> { ["new"] = "value" };

        _mockAzuriteService.UpdateContainerAsync("existing-container", Arg.Any<AzuriteContainerProperties>(), Arg.Any<CancellationToken>())
            .Returns(updatedAzuriteContainer);

        // Act
        var result = await repository.UpdateContainerAsync("existing-container", updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("existing-container");
        result.ETag.Should().Be("new-etag");
        result.Metadata.Should().ContainKey("new").WhoseValue.Should().Be("value");

        // Verify Azurite service was called with correct properties (only Metadata)
        await _mockAzuriteService.Received(1).UpdateContainerAsync(
            "existing-container",
            Arg.Is<AzuriteContainerProperties>(p =>
                p.Metadata.ContainsKey("new") && p.Metadata["new"] == "value"),
            Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 15000)]
    public async Task UpdateContainerAsync_ShouldOnlyUpdateMetadata()
    {
        // Arrange
        using var context = CreateDbContext();
        var existingContainer = new ContainerModel
        {
            Name = "existing-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "old-etag",
            LastModified = DateTimeOffset.UtcNow.AddDays(-1)
        };
        context.Containers.Add(existingContainer);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var updateDto = new ContainerUpdateDTO
        {
            PublicAccess = "blob",
            DefaultEncryptionScope = "new-scope",
            PreventEncryptionScopeOverride = true,
            Metadata = new Dictionary<string, string> { ["key"] = "value" }
        };

        var updatedAzuriteContainer = CreateContainerItem("existing-container", etag: "new-etag");
        updatedAzuriteContainer.Metadata = new Dictionary<string, string> { ["key"] = "value" };

        _mockAzuriteService.UpdateContainerAsync("existing-container", Arg.Any<AzuriteContainerProperties>(), Arg.Any<CancellationToken>())
            .Returns(updatedAzuriteContainer);

        // Act
        var result = await repository.UpdateContainerAsync("existing-container", updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        // Verify only Metadata was passed to Azurite (not PublicAccessType, DefaultEncryptionScope, PreventEncryptionScopeOverride)
        await _mockAzuriteService.Received(1).UpdateContainerAsync(
            "existing-container",
            Arg.Is<AzuriteContainerProperties>(p =>
                p.Metadata.ContainsKey("key") &&
                p.PublicAccessType == null &&
                p.DefaultEncryptionScope == null &&
                p.PreventEncryptionScopeOverride == null),
            Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 15000)]
    public async Task UpdateContainerAsync_WithEmptyMetadata_ShouldUpdateSuccessfully()
    {
        // Arrange
        using var context = CreateDbContext();
        var existingContainer = new ContainerModel
        {
            Name = "existing-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "old-etag",
            LastModified = DateTimeOffset.UtcNow.AddDays(-1),
            Metadata = new Dictionary<string, string> { ["old"] = "value" }
        };
        context.Containers.Add(existingContainer);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var updateDto = new ContainerUpdateDTO
        {
            Metadata = new Dictionary<string, string>()
        };

        var updatedAzuriteContainer = CreateContainerItem("existing-container", etag: "new-etag");
        updatedAzuriteContainer.Metadata = new Dictionary<string, string>();

        _mockAzuriteService.UpdateContainerAsync("existing-container", Arg.Any<AzuriteContainerProperties>(), Arg.Any<CancellationToken>())
            .Returns(updatedAzuriteContainer);

        // Act
        var result = await repository.UpdateContainerAsync("existing-container", updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Metadata.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task UpdateContainerAsync_WhenAzuriteFails_ShouldPropagateExceptionAndNotUpdateCache()
    {
        // Arrange
        using var context = CreateDbContext();
        var existingContainer = new ContainerModel
        {
            Name = "existing-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "old-etag",
            LastModified = DateTimeOffset.UtcNow.AddDays(-1)
        };
        context.Containers.Add(existingContainer);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var updateDto = new ContainerUpdateDTO
        {
            Metadata = new Dictionary<string, string> { ["new"] = "value" }
        };

        _mockAzuriteService.UpdateContainerAsync("existing-container", Arg.Any<AzuriteContainerProperties>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AzuriteServiceException("Update failed"));

        // Act
        var act = async () => await repository.UpdateContainerAsync("existing-container", updateDto, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AzuriteServiceException>();

        // Verify cache still has old values
        var cached = await context.Containers.FirstAsync(c => c.Name == "existing-container");
        cached.ETag.Should().Be("old-etag");
    }

    [Fact(Timeout = 15000)]
    public async Task UpdateContainerAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var updateDto = new ContainerUpdateDTO
        {
            Metadata = new Dictionary<string, string>()
        };

        _mockAzuriteService.UpdateContainerAsync("test-container", Arg.Any<AzuriteContainerProperties>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var act = async () => await repository.UpdateContainerAsync("test-container", updateDto, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region ConvertToPublicAccessType Tests

    [Theory(Timeout = 15000)]
    [InlineData("container", AzuritePublicAccess.Container)]
    [InlineData("CONTAINER", AzuritePublicAccess.Container)]
    [InlineData("CoNtAiNeR", AzuritePublicAccess.Container)]
    [InlineData("blobcontainer", AzuritePublicAccess.Container)]
    [InlineData("BLOBCONTAINER", AzuritePublicAccess.Container)]
    [InlineData("blob", AzuritePublicAccess.Blob)]
    [InlineData("BLOB", AzuritePublicAccess.Blob)]
    [InlineData("none", AzuritePublicAccess.None)]
    [InlineData("NONE", AzuritePublicAccess.None)]
    public void ConvertToPublicAccessType_WithContainer_ShouldReturnContainerEnum(string input, AzuritePublicAccess expected)
    {
        // Act
        var result = StorageRepository.ConvertToPublicAccessType(input);

        // Assert
        result.Should().Be(expected);
    }
    
    [Fact(Timeout = 15000)]
    public void ConvertToPublicAccessType_WithInvalidValue_ShouldThrowAzuriteServiceException()
    {
        // Act
        var act = () => StorageRepository.ConvertToPublicAccessType("invalid");

        // Assert
        act.Should().Throw<AzuriteServiceException>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact(Timeout = 15000)]
    public void ConvertToPublicAccessType_WithEmptyString_ShouldThrowAzuriteServiceException()
    {
        // Act
        var act = () => StorageRepository.ConvertToPublicAccessType("");

        // Assert
        act.Should().Throw<AzuriteServiceException>();
    }

    #endregion
}
