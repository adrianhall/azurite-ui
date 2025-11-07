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

    [Fact(Timeout = 15000)]
    public async Task DeleteBlobAsync_WhenAzuriteReturns404_ShouldRemoveFromCacheAndNotThrow()
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
            Name = "cached-but-not-in-azurite.txt",
            ContainerName = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "blob-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Blobs.Add(blob);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        _mockAzuriteService.DeleteBlobAsync("test-container", "cached-but-not-in-azurite.txt", Arg.Any<CancellationToken>())
            .ThrowsAsync(new AzuriteServiceException("Blob not found") { StatusCode = StatusCodes.Status404NotFound });

        // Act
        await repository.DeleteBlobAsync("test-container", "cached-but-not-in-azurite.txt", CancellationToken.None);

        // Assert
        var cached = await context.Blobs.FirstOrDefaultAsync(b => b.ContainerName == "test-container" && b.Name == "cached-but-not-in-azurite.txt");
        cached.Should().BeNull();

        await _mockAzuriteService.Received(1).DeleteBlobAsync("test-container", "cached-but-not-in-azurite.txt", Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 15000)]
    public async Task DeleteBlobAsync_WhenAzuriteReturns400_ShouldThrowAndNotRemoveFromCache()
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
            Name = "blob-with-error.txt",
            ContainerName = "test-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "blob-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Blobs.Add(blob);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        _mockAzuriteService.DeleteBlobAsync("test-container", "blob-with-error.txt", Arg.Any<CancellationToken>())
            .ThrowsAsync(new AzuriteServiceException("Bad Request") { StatusCode = StatusCodes.Status400BadRequest });

        // Act
        var act = async () => await repository.DeleteBlobAsync("test-container", "blob-with-error.txt", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AzuriteServiceException>()
            .Where(ex => ex.StatusCode == StatusCodes.Status400BadRequest);

        // Verify blob still exists in cache
        var cached = await context.Blobs.FirstOrDefaultAsync(b => b.ContainerName == "test-container" && b.Name == "blob-with-error.txt");
        cached.Should().NotBeNull();
        cached!.ETag.Should().Be("blob-etag");
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

        var updateDto = new UpdateBlobDTO
        {
            ContainerName = "test-container",
            BlobName = "existing-blob.txt",
            Metadata = new Dictionary<string, string> { ["new"] = "value" },
            Tags = new Dictionary<string, string> { ["new-tag"] = "value" }
        };

        var updatedAzuriteBlob = CreateBlobItem("existing-blob.txt", etag: "new-etag");
        updatedAzuriteBlob.Metadata = new Dictionary<string, string> { ["new"] = "value" };
        updatedAzuriteBlob.Tags = new Dictionary<string, string> { ["new-tag"] = "value" };

        _mockAzuriteService.UpdateBlobAsync("test-container", "existing-blob.txt", Arg.Any<AzuriteBlobProperties>(), Arg.Any<CancellationToken>())
            .Returns(updatedAzuriteBlob);

        // Act
        var result = await repository.UpdateBlobAsync(updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("existing-blob.txt");
        result.ContainerName.Should().Be("test-container");
        result.ETag.Should().Be("new-etag");
        result.Metadata.Should().ContainKey("new").WhoseValue.Should().Be("value");
        result.Tags.Should().ContainKey("new-tag").WhoseValue.Should().Be("value");

        // Verify Azurite service was called with correct properties
        await _mockAzuriteService.Received(1).UpdateBlobAsync(
            "test-container",
            "existing-blob.txt",
            Arg.Is<AzuriteBlobProperties>(p =>
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

        var updateDto = new UpdateBlobDTO
        {
            ContainerName = "test-container",
            BlobName = "existing-blob.txt",
            Metadata = new Dictionary<string, string>(),
            Tags = new Dictionary<string, string>()
        };

        var updatedAzuriteBlob = CreateBlobItem("existing-blob.txt", etag: "new-etag");
        updatedAzuriteBlob.Metadata = new Dictionary<string, string>();
        updatedAzuriteBlob.Tags = new Dictionary<string, string>();

        _mockAzuriteService.UpdateBlobAsync("test-container", "existing-blob.txt", Arg.Any<AzuriteBlobProperties>(), Arg.Any<CancellationToken>())
            .Returns(updatedAzuriteBlob);

        // Act
        var result = await repository.UpdateBlobAsync(updateDto, CancellationToken.None);

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

        var updateDto = new UpdateBlobDTO
        {
            ContainerName = "test-container",
            BlobName = "existing-blob.txt",
            Metadata = new Dictionary<string, string> { ["new"] = "value" },
            Tags = new Dictionary<string, string>()
        };

        _mockAzuriteService.UpdateBlobAsync("test-container", "existing-blob.txt", Arg.Any<AzuriteBlobProperties>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AzuriteServiceException("Update failed"));

        // Act
        var act = async () => await repository.UpdateBlobAsync(updateDto, CancellationToken.None);

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

        var updateDto = new UpdateBlobDTO
        {
            ContainerName = "test-container",
            BlobName = "test-blob.txt",
            Metadata = new Dictionary<string, string>(),
            Tags = new Dictionary<string, string>()
        };

        _mockAzuriteService.UpdateBlobAsync("test-container", "test-blob.txt", Arg.Any<AzuriteBlobProperties>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var act = async () => await repository.UpdateBlobAsync(updateDto, cts.Token);

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

        var dto = new CreateContainerDTO
        {
            ContainerName = "new-container",
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
        var result = await repository.CreateContainerAsync(dto, CancellationToken.None);

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

        var dto = new CreateContainerDTO
        {
            ContainerName = "new-container",
            PublicAccess = null!,
            Metadata = new Dictionary<string, string>()
        };

        var azuriteContainer = CreateContainerItem("new-container");

        _mockAzuriteService.CreateContainerAsync("new-container", Arg.Any<AzuriteContainerProperties>(), Arg.Any<CancellationToken>())
            .Returns(azuriteContainer);

        // Act
        var result = await repository.CreateContainerAsync(dto, CancellationToken.None);

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

        var dto = new CreateContainerDTO
        {
            ContainerName = "new-container",
            PublicAccess = "none",
            Metadata = new Dictionary<string, string>()
        };

        var azuriteContainer = CreateContainerItem("new-container");

        _mockAzuriteService.CreateContainerAsync("new-container", Arg.Any<AzuriteContainerProperties>(), Arg.Any<CancellationToken>())
            .Returns(azuriteContainer);

        // Act
        var result = await repository.CreateContainerAsync(dto, CancellationToken.None);

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

        var dto = new CreateContainerDTO
        {
            ContainerName = "new-container",
            PublicAccess = "none",
            Metadata = new Dictionary<string, string>()
        };

        _mockAzuriteService.CreateContainerAsync("new-container", Arg.Any<AzuriteContainerProperties>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AzuriteServiceException("Container creation failed"));

        // Act
        var act = async () => await repository.CreateContainerAsync(dto, CancellationToken.None);

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

        var dto = new CreateContainerDTO
        {
            ContainerName = "new-container",
            PublicAccess = "none",
            Metadata = new Dictionary<string, string>()
        };

        _mockAzuriteService.CreateContainerAsync("new-container", Arg.Any<AzuriteContainerProperties>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var act = async () => await repository.CreateContainerAsync(dto, cts.Token);

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

    [Fact(Timeout = 15000)]
    public async Task DeleteContainerAsync_WhenAzuriteReturns404_ShouldRemoveFromCacheAndNotThrow()
    {
        // Arrange
        using var context = CreateDbContext();
        var container = new ContainerModel
        {
            Name = "cached-but-not-in-azurite",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "test-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        _mockAzuriteService.DeleteContainerAsync("cached-but-not-in-azurite", Arg.Any<CancellationToken>())
            .ThrowsAsync(new AzuriteServiceException("Container not found") { StatusCode = StatusCodes.Status404NotFound });

        // Act
        await repository.DeleteContainerAsync("cached-but-not-in-azurite", CancellationToken.None);

        // Assert
        var cached = await context.Containers.FirstOrDefaultAsync(c => c.Name == "cached-but-not-in-azurite");
        cached.Should().BeNull();

        await _mockAzuriteService.Received(1).DeleteContainerAsync("cached-but-not-in-azurite", Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 15000)]
    public async Task DeleteContainerAsync_WhenAzuriteReturns400_ShouldThrowWithoutRemoving()
    {
        // Arrange
        using var context = CreateDbContext();
        var container = new ContainerModel
        {
            Name = "cached-container",
            CachedCopyId = Guid.NewGuid().ToString("N"),
            ETag = "test-etag",
            LastModified = DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);
        _mockAzuriteService.DeleteContainerAsync("cached-container", Arg.Any<CancellationToken>())
            .ThrowsAsync(new AzuriteServiceException("Bad Request") { StatusCode = StatusCodes.Status400BadRequest });

        // Act
        Func<Task> act = async () => await repository.DeleteContainerAsync("cached-container", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AzuriteServiceException>();
        var cached = await context.Containers.FirstOrDefaultAsync(c => c.Name == "cached-container");
        cached.Should().NotBeNull();
        cached.ETag.Should().Be("test-etag");
        cached.CachedCopyId.Should().NotBeNullOrEmpty();
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

        var updateDto = new UpdateContainerDTO
        {
            ContainerName = "existing-container",
            Metadata = new Dictionary<string, string> { ["new"] = "value" }
        };

        var updatedAzuriteContainer = CreateContainerItem("existing-container", etag: "new-etag");
        updatedAzuriteContainer.Metadata = new Dictionary<string, string> { ["new"] = "value" };

        _mockAzuriteService.UpdateContainerAsync("existing-container", Arg.Any<AzuriteContainerProperties>(), Arg.Any<CancellationToken>())
            .Returns(updatedAzuriteContainer);

        // Act
        var result = await repository.UpdateContainerAsync(updateDto, CancellationToken.None);

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

        var updateDto = new UpdateContainerDTO
        {
            ContainerName = "existing-container",
            Metadata = new Dictionary<string, string> { ["key"] = "value" }
        };

        var updatedAzuriteContainer = CreateContainerItem("existing-container", etag: "new-etag");
        updatedAzuriteContainer.Metadata = new Dictionary<string, string> { ["key"] = "value" };

        _mockAzuriteService.UpdateContainerAsync("existing-container", Arg.Any<AzuriteContainerProperties>(), Arg.Any<CancellationToken>())
            .Returns(updatedAzuriteContainer);

        // Act
        var result = await repository.UpdateContainerAsync(updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        // Verify Metadata was passed to Azurite
        await _mockAzuriteService.Received(1).UpdateContainerAsync(
            "existing-container",
            Arg.Is<AzuriteContainerProperties>(p =>
                p.Metadata.ContainsKey("key")),
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

        var updateDto = new UpdateContainerDTO
        {
            ContainerName = "existing-container",
            Metadata = new Dictionary<string, string>()
        };

        var updatedAzuriteContainer = CreateContainerItem("existing-container", etag: "new-etag");
        updatedAzuriteContainer.Metadata = new Dictionary<string, string>();

        _mockAzuriteService.UpdateContainerAsync("existing-container", Arg.Any<AzuriteContainerProperties>(), Arg.Any<CancellationToken>())
            .Returns(updatedAzuriteContainer);

        // Act
        var result = await repository.UpdateContainerAsync(updateDto, CancellationToken.None);

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

        var updateDto = new UpdateContainerDTO
        {
            ContainerName = "existing-container",
            Metadata = new Dictionary<string, string> { ["new"] = "value" }
        };

        _mockAzuriteService.UpdateContainerAsync("existing-container", Arg.Any<AzuriteContainerProperties>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AzuriteServiceException("Update failed"));

        // Act
        var act = async () => await repository.UpdateContainerAsync(updateDto, CancellationToken.None);

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

        var updateDto = new UpdateContainerDTO
        {
            ContainerName = "test-container",
            Metadata = new Dictionary<string, string>()
        };

        _mockAzuriteService.UpdateContainerAsync("test-container", Arg.Any<AzuriteContainerProperties>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var act = async () => await repository.UpdateContainerAsync(updateDto, cts.Token);

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

    #region ValidateContainerName Tests

    [Fact(Timeout = 15000)]
    public void ValidateContainerName_WithValidName_ShouldNotThrow()
    {
        // Arrange
        var containerName = "valid-container-name";

        // Act
        var act = () => StorageRepository.ValidateContainerName(containerName);

        // Assert
        act.Should().NotThrow();
    }

    [Fact(Timeout = 15000)]
    public void ValidateContainerName_WithNullName_ShouldThrowAzuriteServiceException()
    {
        // Arrange
        string containerName = null!;

        // Act
        var act = () => StorageRepository.ValidateContainerName(containerName);

        // Assert
        act.Should().Throw<AzuriteServiceException>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact(Timeout = 15000)]
    public void ValidateContainerName_WithEmptyString_ShouldThrowAzuriteServiceException()
    {
        // Arrange
        var containerName = "";

        // Act
        var act = () => StorageRepository.ValidateContainerName(containerName);

        // Assert
        act.Should().Throw<AzuriteServiceException>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact(Timeout = 15000)]
    public void ValidateContainerName_WithWhitespaceOnly_ShouldThrowAzuriteServiceException()
    {
        // Arrange
        var containerName = "   ";

        // Act
        var act = () => StorageRepository.ValidateContainerName(containerName);

        // Assert
        act.Should().Throw<AzuriteServiceException>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact(Timeout = 15000)]
    public void ValidateContainerName_WithTabsAndSpaces_ShouldThrowAzuriteServiceException()
    {
        // Arrange
        var containerName = "\t\n  \t";

        // Act
        var act = () => StorageRepository.ValidateContainerName(containerName);

        // Assert
        act.Should().Throw<AzuriteServiceException>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion

    #region Uploads Property Tests

    [Fact(Timeout = 15000)]
    public async Task Uploads_WithNoUploads_ShouldReturnEmptyQueryable()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);

        // Act
        var result = await repository.Uploads.ToListAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task Uploads_WithSingleUpload_ShouldReturnMappedDTO()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var uploadId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddHours(-2);
        var lastActivityAt = DateTimeOffset.UtcNow.AddMinutes(-30);
        var upload = await CreateUploadModelAsync(
            context,
            "test-container",
            uploadId,
            "test-blob.txt",
            10240,
            "text/plain",
            createdAt,
            lastActivityAt
        );

        var repository = CreateRepository(context);

        // Act
        var result = await repository.Uploads.SingleAsync();

        // Assert
        result.Id.Should().Be(uploadId);
        result.Name.Should().Be("test-blob.txt");
        result.ContainerName.Should().Be("test-container");
        result.LastActivityAt.Should().BeCloseTo(lastActivityAt, TimeSpan.FromMilliseconds(100));
        result.Progress.Should().Be(0.0);
    }

    [Fact(Timeout = 15000)]
    public async Task Uploads_WithMultipleUploads_ShouldReturnAllMappedDTOs()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var uploadId1 = Guid.NewGuid();
        var uploadId2 = Guid.NewGuid();
        await CreateUploadModelAsync(context, "test-container", uploadId1, "blob-1.txt");
        await CreateUploadModelAsync(context, "test-container", uploadId2, "blob-2.txt");

        var repository = CreateRepository(context);

        // Act
        var result = await repository.Uploads.ToListAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.Id == uploadId1 && u.Name == "blob-1.txt");
        result.Should().Contain(u => u.Id == uploadId2 && u.Name == "blob-2.txt");
    }

    [Fact(Timeout = 15000)]
    public async Task Uploads_WithBlocks_ShouldCalculateProgressCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var uploadId = Guid.NewGuid();
        var upload = await CreateUploadModelAsync(
            context,
            "test-container",
            uploadId,
            "test-blob.txt",
            contentLength: 10000
        );

        // Add blocks totaling 5000 bytes (50% progress)
        var block1 = CreateUploadBlockModel(uploadId, "block1", blockSize: 3000);
        var block2 = CreateUploadBlockModel(uploadId, "block2", blockSize: 2000);
        context.UploadBlocks.AddRange(block1, block2);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act
        var result = await repository.Uploads.SingleAsync();

        // Assert
        result.Progress.Should().BeApproximately(50.0, 0.01);
    }

    [Fact(Timeout = 15000)]
    public async Task Uploads_WithZeroContentLength_ShouldReturnZeroProgress()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var uploadId = Guid.NewGuid();
        await CreateUploadModelAsync(
            context,
            "test-container",
            uploadId,
            "test-blob.txt",
            contentLength: 0
        );

        var repository = CreateRepository(context);

        // Act
        var result = await repository.Uploads.SingleAsync();

        // Assert
        result.Progress.Should().Be(0.0);
    }

    #endregion

    #region DownloadBlobAsync Tests

    [Fact(Timeout = 15000)]
    public async Task DownloadBlobAsync_WithExistingBlob_ShouldReturnBlobDownloadDTO()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var blob = CreateBlobModel("test-blob.txt", "test-container", contentLength: 2048);
        blob.ContentType = "text/plain";
        blob.ContentEncoding = "gzip";
        blob.ContentLanguage = "en-us";
        context.Blobs.Add(blob);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var contentStream = new MemoryStream([1, 2, 3, 4, 5]);
        var azuriteResult = new AzuriteBlobDownloadResult
        {
            Content = contentStream,
            ContentLength = 2048,
            ContentRange = null,
            ContentType = "text/plain",
            StatusCode = 200
        };

        _mockAzuriteService.DownloadBlobAsync("test-container", "test-blob.txt", null, Arg.Any<CancellationToken>())
            .Returns(azuriteResult);

        // Act
        var result = await repository.DownloadBlobAsync("test-container", "test-blob.txt", null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("test-blob.txt");
        result.ContainerName.Should().Be("test-container");
        result.Content.Should().NotBeNull();
        result.ContentLength.Should().Be(2048);
        result.ContentType.Should().Be("text/plain");
        result.ContentEncoding.Should().Be("gzip");
        result.ContentLanguage.Should().Be("en-us");
        result.StatusCode.Should().Be(200);
        result.ETag.Should().Be(blob.ETag);

        await _mockAzuriteService.Received(1).DownloadBlobAsync("test-container", "test-blob.txt", null, Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 15000)]
    public async Task DownloadBlobAsync_WithHttpRange_ShouldPassRangeToAzurite()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var blob = CreateBlobModel("test-blob.txt", "test-container", contentLength: 2048);
        context.Blobs.Add(blob);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var contentStream = new MemoryStream([1, 2, 3, 4, 5]);
        var azuriteResult = new AzuriteBlobDownloadResult
        {
            Content = contentStream,
            ContentLength = 500,
            ContentRange = "bytes 0-499/2048",
            ContentType = "text/plain",
            StatusCode = 206
        };

        _mockAzuriteService.DownloadBlobAsync("test-container", "test-blob.txt", "bytes=0-499", Arg.Any<CancellationToken>())
            .Returns(azuriteResult);

        // Act
        var result = await repository.DownloadBlobAsync("test-container", "test-blob.txt", "bytes=0-499", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(206);
        result.ContentRange.Should().Be("bytes 0-499/2048");

        await _mockAzuriteService.Received(1).DownloadBlobAsync("test-container", "test-blob.txt", "bytes=0-499", Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 15000)]
    public async Task DownloadBlobAsync_WithNonExistentBlob_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);

        // Act
        var act = async () => await repository.DownloadBlobAsync("test-container", "non-existent.txt", null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>()
            .Where(ex => ex.ResourceName == "test-container/non-existent.txt");
    }

    [Fact(Timeout = 15000)]
    public async Task DownloadBlobAsync_WhenAzuriteReturnsFailureStatus_ShouldThrowAzuriteServiceException()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var blob = CreateBlobModel("test-blob.txt", "test-container");
        context.Blobs.Add(blob);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var azuriteResult = new AzuriteBlobDownloadResult
        {
            Content = null,
            StatusCode = 500
        };

        _mockAzuriteService.DownloadBlobAsync("test-container", "test-blob.txt", null, Arg.Any<CancellationToken>())
            .Returns(azuriteResult);

        // Act
        var act = async () => await repository.DownloadBlobAsync("test-container", "test-blob.txt", null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AzuriteServiceException>()
            .Where(ex => ex.StatusCode == 500);
    }

    [Fact(Timeout = 15000)]
    public async Task DownloadBlobAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var blob = CreateBlobModel("test-blob.txt", "test-container");
        context.Blobs.Add(blob);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockAzuriteService.DownloadBlobAsync("test-container", "test-blob.txt", null, Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var act = async () => await repository.DownloadBlobAsync("test-container", "test-blob.txt", null, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region CreateUploadAsync Tests

    [Fact(Timeout = 15000)]
    public async Task CreateUploadAsync_WithValidInput_ShouldCreateUploadSession()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var repository = CreateRepository(context);

        var uploadDto = new CreateUploadRequestDTO
        {
            BlobName = "new-blob.txt",
            ContainerName = "test-container",
            ContentLength = 10240,
            ContentType = "text/plain",
            ContentEncoding = "gzip",
            ContentLanguage = "en-us",
            Metadata = new Dictionary<string, string> { ["key"] = "value" },
            Tags = new Dictionary<string, string> { ["tag"] = "value" }
        };

        // Act
        var uploadDTO = await repository.CreateUploadAsync(uploadDto, CancellationToken.None);

        // Assert
        uploadDTO.Should().NotBeNull();
        uploadDTO.UploadId.Should().NotBeEmpty();
        uploadDTO.BlobName.Should().Be("new-blob.txt");
        uploadDTO.ContainerName.Should().Be("test-container");
        uploadDTO.ContentLength.Should().Be(10240);
        uploadDTO.ContentType.Should().Be("text/plain");
    }

    [Fact(Timeout = 15000)]
    public async Task CreateUploadAsync_WithNonExistentContainer_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);

        var uploadDto = new CreateUploadRequestDTO
        {
            BlobName = "new-blob.txt",
            ContainerName = "non-existent-container",
            ContentLength = 10240
        };

        // Act
        var act = async () => await repository.CreateUploadAsync(uploadDto, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>()
            .Where(ex => ex.ResourceName == "non-existent-container");
    }

    [Fact(Timeout = 15000)]
    public async Task CreateUploadAsync_WithExistingBlob_ShouldThrowResourceExistsException()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var blob = CreateBlobModel("existing-blob.txt", "test-container");
        context.Blobs.Add(blob);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var uploadDto = new CreateUploadRequestDTO
        {
            BlobName = "existing-blob.txt",
            ContainerName = "test-container",
            ContentLength = 10240
        };

        // Act
        var act = async () => await repository.CreateUploadAsync(uploadDto, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ResourceExistsException>()
            .Where(ex => ex.ResourceName == "test-container/existing-blob.txt");
    }

    [Fact(Timeout = 15000)]
    public async Task CreateUploadAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var uploadDto = new CreateUploadRequestDTO
        {
            BlobName = "new-blob.txt",
            ContainerName = "test-container",
            ContentLength = 10240
        };

        // Act
        var act = async () => await repository.CreateUploadAsync(uploadDto, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region UploadBlockAsync Tests

    [Fact(Timeout = 15000)]
    public async Task UploadBlockAsync_WithValidBlock_ShouldUploadToAzuriteAndSaveToCache()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var uploadId = Guid.NewGuid();
        await CreateUploadModelAsync(context, "test-container", uploadId, "test-blob.txt");

        var repository = CreateRepository(context);

        var blockId = Convert.ToBase64String("block1"u8.ToArray());
        var contentStream = new MemoryStream([1, 2, 3, 4, 5]);

        var blockInfo = new AzuriteBlobBlockInfo
        {
            BlockId = blockId,
            ContentMD5 = "test-md5",
            StatusCode = 201
        };

        _mockAzuriteService.UploadBlockAsync("test-container", "test-blob.txt", blockId, contentStream, Arg.Any<CancellationToken>())
            .Returns(blockInfo);

        // Act
        await repository.UploadBlockAsync(uploadId, blockId, contentStream, null, CancellationToken.None);

        // Assert
        var uploadBlock = await context.UploadBlocks.FirstOrDefaultAsync(b => b.UploadId == uploadId && b.BlockId == blockId);
        uploadBlock.Should().NotBeNull();
        uploadBlock!.BlockId.Should().Be(blockId);
        uploadBlock.BlockSize.Should().Be(5);
        uploadBlock.ContentMD5.Should().Be("test-md5");

        await _mockAzuriteService.Received(1).UploadBlockAsync("test-container", "test-blob.txt", blockId, contentStream, Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 15000)]
    public async Task UploadBlockAsync_WithInvalidBlockId_ShouldThrowAzuriteServiceException()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);

        var uploadId = Guid.NewGuid();
        var contentStream = new MemoryStream([1, 2, 3]);

        // Act
        var act = async () => await repository.UploadBlockAsync(uploadId, "not-base64!", contentStream, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AzuriteServiceException>()
            .Where(ex => ex.StatusCode == StatusCodes.Status400BadRequest);
    }

    [Fact(Timeout = 15000)]
    public async Task UploadBlockAsync_WithNonExistentUpload_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);

        var uploadId = Guid.NewGuid();
        var blockId = Convert.ToBase64String("block1"u8.ToArray());
        var contentStream = new MemoryStream([1, 2, 3]);

        // Act
        var act = async () => await repository.UploadBlockAsync(uploadId, blockId, contentStream, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>()
            .Where(ex => ex.ResourceName == uploadId.ToString());
    }

    [Fact(Timeout = 15000)]
    public async Task UploadBlockAsync_WhenAzuriteFails_ShouldThrowAzuriteServiceException()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var uploadId = Guid.NewGuid();
        await CreateUploadModelAsync(context, "test-container", uploadId, "test-blob.txt");

        var repository = CreateRepository(context);

        var blockId = Convert.ToBase64String("block1"u8.ToArray());
        var contentStream = new MemoryStream([1, 2, 3]);

        var blockInfo = new AzuriteBlobBlockInfo
        {
            BlockId = blockId,
            StatusCode = 500
        };

        _mockAzuriteService.UploadBlockAsync("test-container", "test-blob.txt", blockId, contentStream, Arg.Any<CancellationToken>())
            .Returns(blockInfo);

        // Act
        var act = async () => await repository.UploadBlockAsync(uploadId, blockId, contentStream, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AzuriteServiceException>()
            .Where(ex => ex.StatusCode == 500);

        // Verify block was not saved to cache
        var uploadBlock = await context.UploadBlocks.FirstOrDefaultAsync(b => b.UploadId == uploadId && b.BlockId == blockId);
        uploadBlock.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public async Task UploadBlockAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var uploadId = Guid.NewGuid();
        await CreateUploadModelAsync(context, "test-container", uploadId, "test-blob.txt");

        var repository = CreateRepository(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var blockId = Convert.ToBase64String("block1"u8.ToArray());
        var contentStream = new MemoryStream([1, 2, 3]);

        _mockAzuriteService.UploadBlockAsync("test-container", "test-blob.txt", blockId, contentStream, Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var act = async () => await repository.UploadBlockAsync(uploadId, blockId, contentStream, null, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region GetUploadStatusAsync Tests

    [Fact(Timeout = 15000)]
    public async Task GetUploadStatusAsync_WithExistingUpload_ShouldReturnUploadStatusDTO()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var uploadId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddHours(-1);
        var lastActivityAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var upload = await CreateUploadModelAsync(
            context,
            "test-container",
            uploadId,
            "test-blob.txt",
            10240,
            "text/plain",
            createdAt,
            lastActivityAt
        );

        var block1 = CreateUploadBlockModel(uploadId, "block1", blockSize: 1024);
        var block2 = CreateUploadBlockModel(uploadId, "block2", blockSize: 2048);
        context.UploadBlocks.AddRange(block1, block2);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetUploadStatusAsync(uploadId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UploadId.Should().Be(uploadId);
        result.ContainerName.Should().Be("test-container");
        result.BlobName.Should().Be("test-blob.txt");
        result.ContentLength.Should().Be(10240);
        result.ContentType.Should().Be("text/plain");
        result.UploadedBlocks.Should().HaveCount(2);
        result.UploadedBlocks.Should().Contain("block1");
        result.UploadedBlocks.Should().Contain("block2");
        result.UploadedLength.Should().Be(3072);
        result.CreatedAt.Should().BeCloseTo(createdAt, TimeSpan.FromMilliseconds(100));
        result.LastActivityAt.Should().BeCloseTo(lastActivityAt, TimeSpan.FromMilliseconds(100));
    }

    [Fact(Timeout = 15000)]
    public async Task GetUploadStatusAsync_WithNoBlocks_ShouldReturnZeroUploadedLength()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var uploadId = Guid.NewGuid();
        await CreateUploadModelAsync(context, "test-container", uploadId, "test-blob.txt");

        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetUploadStatusAsync(uploadId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UploadedBlocks.Should().BeEmpty();
        result.UploadedLength.Should().Be(0);
    }

    [Fact(Timeout = 15000)]
    public async Task GetUploadStatusAsync_WithNonExistentUpload_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);

        var uploadId = Guid.NewGuid();

        // Act
        var act = async () => await repository.GetUploadStatusAsync(uploadId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>()
            .Where(ex => ex.ResourceName == uploadId.ToString());
    }

    [Fact(Timeout = 15000)]
    public async Task GetUploadStatusAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var uploadId = Guid.NewGuid();

        // Act
        var act = async () => await repository.GetUploadStatusAsync(uploadId, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region CommitUploadAsync Tests

    [Fact(Timeout = 15000)]
    public async Task CommitUploadAsync_WithAllBlocksUploaded_ShouldCommitAndReturnBlobDTO()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var uploadId = Guid.NewGuid();
        var upload = await CreateUploadModelAsync(context, "test-container", uploadId, "new-blob.txt");
        upload.ContentEncoding = "gzip";
        upload.ContentLanguage = "en-us";
        upload.ContentType = "text/plain";
        upload.Metadata = new Dictionary<string, string> { ["key"] = "value" };
        upload.Tags = new Dictionary<string, string> { ["tag"] = "value" };
        await context.SaveChangesAsync();

        var block1 = CreateUploadBlockModel(uploadId, "block1", blockSize: 1024);
        var block2 = CreateUploadBlockModel(uploadId, "block2", blockSize: 2048);
        context.UploadBlocks.AddRange(block1, block2);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var committedBlob = CreateBlobItem("new-blob.txt", etag: "commit-etag", contentLength: 3072);
        committedBlob.ContentEncoding = "gzip";
        committedBlob.ContentLanguage = "en-us";
        committedBlob.ContentType = "text/plain";
        committedBlob.Metadata = new Dictionary<string, string> { ["key"] = "value" };
        committedBlob.Tags = new Dictionary<string, string> { ["tag"] = "value" };

        _mockAzuriteService.UploadCommitAsync(
            "test-container",
            "new-blob.txt",
            Arg.Is<IEnumerable<string>>(blocks => blocks.SequenceEqual(new[] { "block1", "block2" })),
            Arg.Any<AzuriteBlobProperties>(),
            Arg.Any<CancellationToken>()
        ).Returns(committedBlob);

        // Act
        var result = await repository.CommitUploadAsync(uploadId, new[] { "block1", "block2" }, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("new-blob.txt");
        result.ContainerName.Should().Be("test-container");
        result.ETag.Should().Be("commit-etag");
        result.ContentEncoding.Should().Be("gzip");
        result.ContentLanguage.Should().Be("en-us");
        result.ContentType.Should().Be("text/plain");
        result.Metadata.Should().ContainKey("key").WhoseValue.Should().Be("value");
        result.Tags.Should().ContainKey("tag").WhoseValue.Should().Be("value");

        // Verify the blob was saved to cache
        var cachedBlob = await context.Blobs.FirstOrDefaultAsync(b => b.ContainerName == "test-container" && b.Name == "new-blob.txt");
        cachedBlob.Should().NotBeNull();

        await _mockAzuriteService.Received(1).UploadCommitAsync(
            "test-container",
            "new-blob.txt",
            Arg.Any<IEnumerable<string>>(),
            Arg.Is<AzuriteBlobProperties>(p =>
                p.ContentEncoding == "gzip" &&
                p.ContentLanguage == "en-us" &&
                p.ContentType == "text/plain" &&
                p.Metadata.ContainsKey("key") &&
                p.Tags.ContainsKey("tag")),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact(Timeout = 15000)]
    public async Task CommitUploadAsync_WithMissingBlocks_ShouldThrowAzuriteServiceException()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var uploadId = Guid.NewGuid();
        await CreateUploadModelAsync(context, "test-container", uploadId, "new-blob.txt");

        var block1 = CreateUploadBlockModel(uploadId, "block1");
        context.UploadBlocks.Add(block1);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act - trying to commit with blocks that haven't been uploaded
        var act = async () => await repository.CommitUploadAsync(uploadId, new[] { "block1", "block2", "block3" }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AzuriteServiceException>()
            .Where(ex => ex.StatusCode == StatusCodes.Status400BadRequest);
    }

    [Fact(Timeout = 15000)]
    public async Task CommitUploadAsync_WithNonExistentUpload_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);

        var uploadId = Guid.NewGuid();

        // Act
        var act = async () => await repository.CommitUploadAsync(uploadId, new[] { "block1" }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>()
            .Where(ex => ex.ResourceName == uploadId.ToString());
    }

    [Fact(Timeout = 15000)]
    public async Task CommitUploadAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var uploadId = Guid.NewGuid();
        await CreateUploadModelAsync(context, "test-container", uploadId, "new-blob.txt");

        var repository = CreateRepository(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockAzuriteService.UploadCommitAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<AzuriteBlobProperties>(),
            Arg.Any<CancellationToken>()
        ).ThrowsAsync(new OperationCanceledException());

        // Act
        var act = async () => await repository.CommitUploadAsync(uploadId, new[] { "block1" }, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region CancelUploadAsync Tests

    [Fact(Timeout = 15000)]
    public async Task CancelUploadAsync_WithExistingUpload_ShouldRemoveFromCache()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var uploadId = Guid.NewGuid();
        await CreateUploadModelAsync(context, "test-container", uploadId, "test-blob.txt");

        var repository = CreateRepository(context);

        // Act
        await repository.CancelUploadAsync(uploadId, CancellationToken.None);

        // Assert
        var upload = await context.Uploads.FirstOrDefaultAsync(u => u.UploadId == uploadId);
        upload.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public async Task CancelUploadAsync_WithNonExistentUpload_ShouldNotThrow()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);

        var uploadId = Guid.NewGuid();

        // Act
        var act = async () => await repository.CancelUploadAsync(uploadId, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact(Timeout = 15000)]
    public async Task CancelUploadAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var uploadId = Guid.NewGuid();

        // Act
        var act = async () => await repository.CancelUploadAsync(uploadId, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region ValidateBlockId Tests

    [Theory(Timeout = 15000)]
    [InlineData("YmxvY2sxMjM=")] // "block123" in base64
    [InlineData("dGVzdA==")] // "test" in base64
    [InlineData("MTIzNDU2Nzg5MA==")] // "1234567890" in base64
    public void ValidateBlockId_WithValidBase64_ShouldNotThrow(string blockId)
    {
        // Act
        var act = () => StorageRepository.ValidateBlockId(blockId);

        // Assert
        act.Should().NotThrow();
    }

    [Fact(Timeout = 15000)]
    public void ValidateBlockId_WithInvalidBase64_ShouldThrowAzuriteServiceException()
    {
        // Act
        var act = () => StorageRepository.ValidateBlockId("not-base64!");

        // Assert
        act.Should().Throw<AzuriteServiceException>()
            .Where(ex => ex.StatusCode == StatusCodes.Status400BadRequest);
    }

    [Fact(Timeout = 15000)]
    public void ValidateBlockId_WithTooLongBlockId_ShouldThrowAzuriteServiceException()
    {
        // Arrange - Create a base64 string that decodes to more than 64 bytes
        var longBytes = new byte[65];
        Array.Fill(longBytes, (byte)0x41);
        var longBlockId = Convert.ToBase64String(longBytes);

        // Act
        var act = () => StorageRepository.ValidateBlockId(longBlockId);

        // Assert
        act.Should().Throw<AzuriteServiceException>()
            .Where(ex => ex.StatusCode == StatusCodes.Status400BadRequest);
    }

    [Fact(Timeout = 15000)]
    public void ValidateBlockId_WithEmptyString_ShouldNotThrow()
    {
        // Act
        var act = () => StorageRepository.ValidateBlockId("");

        // Assert - Empty string is valid base64 (decodes to 0 bytes)
        act.Should().NotThrow();
    }

    #endregion

    #region DisposeDownloadStream Tests

    [Fact(Timeout = 15000)]
    public void DisposeDownloadStream_WithValidStream_ShouldDisposeAndSetToNull()
    {
        // Arrange
        var stream = new MemoryStream([1, 2, 3]);
        var dto = new BlobDownloadDTO
        {
            Name = "test",
            ContainerName = "test",
            ETag = "test",
            LastModified = DateTimeOffset.UtcNow,
            Content = stream,
            StatusCode = 200
        };

        // Act
        StorageRepository.DisposeDownloadStream(dto);

        // Assert
        dto.Content.Should().BeNull();
        var act = () => stream.ReadByte();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact(Timeout = 15000)]
    public void DisposeDownloadStream_WithNullStream_ShouldNotThrow()
    {
        // Arrange
        var dto = new BlobDownloadDTO
        {
            Name = "test",
            ContainerName = "test",
            ETag = "test",
            LastModified = DateTimeOffset.UtcNow,
            Content = null,
            StatusCode = 200
        };

        // Act
        var act = () => StorageRepository.DisposeDownloadStream(dto);

        // Assert
        act.Should().NotThrow();
        dto.Content.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void DisposeDownloadStream_WithStreamThatThrowsOnDispose_ShouldNotThrow()
    {
        // Arrange - Create a custom stream that throws on dispose
        var throwingStream = new ThrowingStream();

        var dto = new BlobDownloadDTO
        {
            Name = "test",
            ContainerName = "test",
            ETag = "test",
            LastModified = DateTimeOffset.UtcNow,
            Content = throwingStream,
            StatusCode = 200
        };

        // Act
        var act = () => StorageRepository.DisposeDownloadStream(dto);

        // Assert - The method swallows exceptions to avoid masking original errors
        act.Should().NotThrow();
    }

    private class ThrowingStream : MemoryStream
    {
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                throw new IOException("Dispose failed");
            }
            base.Dispose(disposing);
        }
    }

    #endregion

    #region ValidateBlobName Tests

    [Fact(Timeout = 15000)]
    public void ValidateBlobName_WithValidBlobName_ShouldNotThrowException()
    {
        // Arrange
        var validBlobName = "test-blob.txt";

        // Act
        Action act = () => StorageRepository.ValidateBlobName(validBlobName);

        // Assert
        act.Should().NotThrow();
    }

    [Fact(Timeout = 15000)]
    public void ValidateBlobName_WithBlobNameContainingSlashes_ShouldNotThrowException()
    {
        // Arrange
        var validBlobName = "folder/subfolder/test-blob.txt";

        // Act
        Action act = () => StorageRepository.ValidateBlobName(validBlobName);

        // Assert
        act.Should().NotThrow();
    }

    [Fact(Timeout = 15000)]
    public void ValidateBlobName_WithBlobNameContainingSpecialCharacters_ShouldNotThrowException()
    {
        // Arrange
        var validBlobName = "test_blob-2024.01.01.txt";

        // Act
        Action act = () => StorageRepository.ValidateBlobName(validBlobName);

        // Assert
        act.Should().NotThrow();
    }

    [Fact(Timeout = 15000)]
    public void ValidateBlobName_WithNullBlobName_ShouldThrowAzuriteServiceException()
    {
        // Arrange
        string? nullBlobName = null;

        // Act
        Action act = () => StorageRepository.ValidateBlobName(nullBlobName!);

        // Assert
        act.Should().Throw<AzuriteServiceException>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact(Timeout = 15000)]
    public void ValidateBlobName_WithEmptyBlobName_ShouldThrowAzuriteServiceException()
    {
        // Arrange
        var emptyBlobName = string.Empty;

        // Act
        Action act = () => StorageRepository.ValidateBlobName(emptyBlobName);

        // Assert
        act.Should().Throw<AzuriteServiceException>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact(Timeout = 15000)]
    public void ValidateBlobName_WithWhitespaceBlobName_ShouldThrowAzuriteServiceException()
    {
        // Arrange
        var whitespaceBlobName = "   ";

        // Act
        Action act = () => StorageRepository.ValidateBlobName(whitespaceBlobName);

        // Assert
        act.Should().Throw<AzuriteServiceException>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact(Timeout = 15000)]
    public void ValidateBlobName_WithTabCharacterBlobName_ShouldThrowAzuriteServiceException()
    {
        // Arrange
        var tabBlobName = "\t";

        // Act
        Action act = () => StorageRepository.ValidateBlobName(tabBlobName);

        // Assert
        act.Should().Throw<AzuriteServiceException>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact(Timeout = 15000)]
    public void ValidateBlobName_WithNewlineBlobName_ShouldThrowAzuriteServiceException()
    {
        // Arrange
        var newlineBlobName = "\n";

        // Act
        Action act = () => StorageRepository.ValidateBlobName(newlineBlobName);

        // Assert
        act.Should().Throw<AzuriteServiceException>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact(Timeout = 15000)]
    public void ValidateBlobName_WithSingleCharacterBlobName_ShouldNotThrowException()
    {
        // Arrange
        var singleCharBlobName = "a";

        // Act
        Action act = () => StorageRepository.ValidateBlobName(singleCharBlobName);

        // Assert
        act.Should().NotThrow();
    }

    [Fact(Timeout = 15000)]
    public void ValidateBlobName_WithVeryLongBlobName_ShouldNotThrowException()
    {
        // Arrange
        var longBlobName = new string('a', 1000);

        // Act
        Action act = () => StorageRepository.ValidateBlobName(longBlobName);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region GetDashboardDataAsync Tests
    #region Basic Functionality Tests

    [Fact(Timeout = 15000)]
    public async Task GetDashboardDataAsync_WithEmptyDatabase_ShouldReturnZeroStats()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetDashboardDataAsync();

        // Assert
        result.Should().NotBeNull();
        result.Stats.Should().NotBeNull();
        result.Stats.Containers.Should().Be(0);
        result.Stats.Blobs.Should().Be(0);
        result.Stats.TotalBlobSize.Should().Be(0);
        result.Stats.TotalImageSize.Should().Be(0);
        result.RecentContainers.Should().BeEmpty();
        result.RecentBlobs.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task GetDashboardDataAsync_WithVariousCounts_ShouldCalculateCorrectStats()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create 3 containers
        for (int i = 0; i < 3; i++)
        {
            await CreateContainerModelAsync(context, $"container{i}");
        }

        // Create 5 blobs with known sizes
        var containerName = "container0";
        long expectedTotalSize = 0;
        for (int i = 0; i < 5; i++)
        {
            long size = (i + 1) * 100;
            var blob = CreateBlobModel($"blob{i}.txt", containerName, contentLength: size);
            context.Blobs.Add(blob);
            expectedTotalSize += size;
        }
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetDashboardDataAsync();

        // Assert
        result.Stats.Containers.Should().Be(3);
        result.Stats.Blobs.Should().Be(5);
        result.Stats.TotalBlobSize.Should().Be(expectedTotalSize);
    }

    #endregion

    #region Image Size Calculation Tests

    [Fact(Timeout = 15000)]
    public async Task GetDashboardDataAsync_WithImageBlobs_ShouldCalculateImageSizeCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var containerName = await CreateContainerModelAsync(context, "test-container");

        // Create image blobs
        var imageBlob1 = CreateBlobModel("image1.jpg", "test-container", contentLength: 1000);
        imageBlob1.ContentType = "image/jpeg";
        context.Blobs.Add(imageBlob1);

        var imageBlob2 = CreateBlobModel("image2.png", "test-container", contentLength: 2000);
        imageBlob2.ContentType = "image/png";
        context.Blobs.Add(imageBlob2);

        // Create non-image blobs
        var textBlob = CreateBlobModel("text.txt", "test-container", contentLength: 500);
        textBlob.ContentType = "text/plain";
        context.Blobs.Add(textBlob);

        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetDashboardDataAsync();

        // Assert
        result.Stats.TotalBlobSize.Should().Be(3500);
        result.Stats.TotalImageSize.Should().Be(3000);
    }

    [Fact(Timeout = 15000)]
    public async Task GetDashboardDataAsync_WithNoImages_ShouldHaveZeroImageSize()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var textBlob = CreateBlobModel("text.txt", "test-container", contentLength: 500);
        textBlob.ContentType = "text/plain";
        context.Blobs.Add(textBlob);

        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetDashboardDataAsync();

        // Assert
        result.Stats.TotalBlobSize.Should().Be(500);
        result.Stats.TotalImageSize.Should().Be(0);
    }

    [Fact(Timeout = 15000)]
    public async Task GetDashboardDataAsync_WithMixedImageTypes_ShouldIncludeAllImageContentTypes()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var imageTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml" };
        long expectedImageSize = 0;

        foreach (var contentType in imageTypes)
        {
            var blob = CreateBlobModel($"image-{contentType.Replace("/", "-")}", "test-container", contentLength: 100);
            blob.ContentType = contentType;
            context.Blobs.Add(blob);
            expectedImageSize += 100;
        }

        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetDashboardDataAsync();

        // Assert
        result.Stats.TotalImageSize.Should().Be(expectedImageSize);
    }

    #endregion

    #region Container LastModified Tests

    [Fact(Timeout = 15000)]
    public async Task GetDashboardDataAsync_ContainerLastModified_ShouldUseMaxOfContainerAndBlobTimes()
    {
        // Arrange
        using var context = CreateDbContext();
        var containerTime = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var blobTime = new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero);

        await CreateContainerModelAsync(context, "test-container", lastModified: containerTime);

        var blob = CreateBlobModel("blob.txt", "test-container", lastModified: blobTime);
        context.Blobs.Add(blob);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetDashboardDataAsync();

        // Assert
        result.RecentContainers.Should().HaveCount(1);
        var container = result.RecentContainers.First();
        container.LastModified.Should().Be(blobTime, "should use the blob's more recent timestamp");
    }

    [Fact(Timeout = 15000)]
    public async Task GetDashboardDataAsync_ContainerLastModified_WhenContainerIsNewer_ShouldUseContainerTime()
    {
        // Arrange
        using var context = CreateDbContext();
        var blobTime = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var containerTime = new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero);

        await CreateContainerModelAsync(context, "test-container", lastModified: containerTime);

        var blob = CreateBlobModel("blob.txt", "test-container", lastModified: blobTime);
        context.Blobs.Add(blob);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetDashboardDataAsync();

        // Assert
        result.RecentContainers.Should().HaveCount(1);
        var container = result.RecentContainers.First();
        container.LastModified.Should().Be(containerTime, "should use the container's more recent timestamp");
    }

    [Fact(Timeout = 15000)]
    public async Task GetDashboardDataAsync_ContainerWithoutBlobs_ShouldUseContainerLastModified()
    {
        // Arrange
        using var context = CreateDbContext();
        var containerTime = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

        await CreateContainerModelAsync(context, "empty-container", lastModified: containerTime);

        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetDashboardDataAsync();

        // Assert
        result.RecentContainers.Should().HaveCount(1);
        var container = result.RecentContainers.First();
        container.Name.Should().Be("empty-container");
        container.LastModified.Should().Be(containerTime);
        container.BlobCount.Should().Be(0);
        container.TotalSize.Should().Be(0);
    }

    #endregion

    #region Recent Containers Tests

    [Fact(Timeout = 15000)]
    public async Task GetDashboardDataAsync_RecentContainers_ShouldLimitToTen()
    {
        // Arrange
        using var context = CreateDbContext();

        // Create 15 containers with sequential timestamps
        for (int i = 0; i < 15; i++)
        {
            var timestamp = new DateTimeOffset(2024, 1, 1, 12, i, 0, TimeSpan.Zero);
            await CreateContainerModelAsync(context, $"container{i:D2}", lastModified: timestamp);
        }

        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetDashboardDataAsync();

        // Assert
        result.Stats.Containers.Should().Be(15);
        result.RecentContainers.Should().HaveCount(10);
    }

    [Fact(Timeout = 15000)]
    public async Task GetDashboardDataAsync_RecentContainers_ShouldBeOrderedByLastModifiedDescending()
    {
        // Arrange
        using var context = CreateDbContext();

        for (int i = 0; i < 5; i++)
        {
            var timestamp = new DateTimeOffset(2024, 1, 1, 12, i, 0, TimeSpan.Zero);
            await CreateContainerModelAsync(context, $"container{i}", lastModified: timestamp);
        }

        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetDashboardDataAsync();

        // Assert
        result.RecentContainers.Should().HaveCount(5);
        var lastModifiedDates = result.RecentContainers.Select(c => c.LastModified).ToList();
        lastModifiedDates.Should().BeInDescendingOrder();
    }

    [Fact(Timeout = 15000)]
    public async Task GetDashboardDataAsync_RecentContainers_ShouldIncludeBlobCountAndTotalSize()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        long expectedTotalSize = 0;
        for (int i = 0; i < 3; i++)
        {
            long size = (i + 1) * 100;
            var blob = CreateBlobModel($"blob{i}.txt", "test-container", contentLength: size);
            context.Blobs.Add(blob);
            expectedTotalSize += size;
        }
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetDashboardDataAsync();

        // Assert
        result.RecentContainers.Should().HaveCount(1);
        var container = result.RecentContainers.First();
        container.BlobCount.Should().Be(3);
        container.TotalSize.Should().Be(expectedTotalSize);
    }

    #endregion

    #region Recent Blobs Tests

    [Fact(Timeout = 15000)]
    public async Task GetDashboardDataAsync_RecentBlobs_ShouldLimitToTen()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        // Create 15 blobs with sequential timestamps
        for (int i = 0; i < 15; i++)
        {
            var timestamp = new DateTimeOffset(2024, 1, 1, 12, i, 0, TimeSpan.Zero);
            var blob = CreateBlobModel($"blob{i:D2}.txt", "test-container", lastModified: timestamp);
            context.Blobs.Add(blob);
        }
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetDashboardDataAsync();

        // Assert
        result.Stats.Blobs.Should().Be(15);
        result.RecentBlobs.Should().HaveCount(10);
    }

    [Fact(Timeout = 15000)]
    public async Task GetDashboardDataAsync_RecentBlobs_ShouldBeOrderedByLastModifiedDescending()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        for (int i = 0; i < 5; i++)
        {
            var timestamp = new DateTimeOffset(2024, 1, 1, 12, i, 0, TimeSpan.Zero);
            var blob = CreateBlobModel($"blob{i}.txt", "test-container", lastModified: timestamp);
            context.Blobs.Add(blob);
        }
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetDashboardDataAsync();

        // Assert
        result.RecentBlobs.Should().HaveCount(5);
        var lastModifiedDates = result.RecentBlobs.Select(b => b.LastModified).ToList();
        lastModifiedDates.Should().BeInDescendingOrder();
    }

    [Fact(Timeout = 15000)]
    public async Task GetDashboardDataAsync_RecentBlobs_ShouldIncludeAllRequiredProperties()
    {
        // Arrange
        using var context = CreateDbContext();
        await CreateContainerModelAsync(context, "test-container");

        var blob = CreateBlobModel("test-blob.txt", "test-container", contentLength: 1024);
        blob.ContentType = "text/plain";
        context.Blobs.Add(blob);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetDashboardDataAsync();

        // Assert
        result.RecentBlobs.Should().HaveCount(1);
        var blobInfo = result.RecentBlobs.First();
        blobInfo.Name.Should().Be("test-blob.txt");
        blobInfo.ContainerName.Should().Be("test-container");
        blobInfo.ContentType.Should().Be("text/plain");
        blobInfo.ContentLength.Should().Be(1024);
        blobInfo.LastModified.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    #endregion
    #endregion
}

