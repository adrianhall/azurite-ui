using AzuriteUI.Web.Services.Azurite;
using AzuriteUI.Web.Services.Azurite.Exceptions;
using AzuriteUI.Web.Services.CacheDb;
using AzuriteUI.Web.Services.Repositories;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Testing;
using System.Text;

namespace AzuriteUI.Web.IntegrationTests.Services.Repositories;

/// <summary>
/// Integration tests for the <see cref="StorageRepository"/> class.
/// </summary>
[ExcludeFromCodeCoverage]
public class StorageRepository_Tests : IClassFixture<AzuriteFixture>, IAsyncLifetime
{
    private readonly AzuriteFixture _fixture;
    private readonly IAzuriteService _azuriteService;
    private readonly SqliteConnection _connection;
    private readonly CacheDbContext _context;
    private readonly IStorageRepository _repository;
    private readonly FakeLogger<StorageRepository> _logger;

    public StorageRepository_Tests(AzuriteFixture fixture)
    {
        _fixture = fixture;
        _azuriteService = new AzuriteService(_fixture.ConnectionString, new FakeLogger<AzuriteService>());

        // Create in-memory SQLite database for testing
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<CacheDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new CacheDbContext(options);
        _context.Database.EnsureCreated();

        _logger = new FakeLogger<StorageRepository>();
        _repository = new StorageRepository(_context, _azuriteService, _logger);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
        await _fixture.CleanupAsync();
        GC.SuppressFinalize(this);
    }

    #region Container Access - Containers Property
    [Fact(Timeout = 60000)]
    public async Task Containers_WhenContainerExistsInCache_ShouldReturnContainer()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        await _fixture.CreateContainerAsync(containerName);
        var azuriteContainer = await _azuriteService.GetContainerAsync(containerName);
        await _context.UpsertContainerAsync(azuriteContainer);

        // Act
        var containers = await _repository.Containers.ToListAsync();

        // Assert
        containers.Should().NotBeEmpty()
            .And.Contain(c => c.Name == containerName);
    }

    [Fact(Timeout = 60000)]
    public async Task Containers_WhenNoContainersInCache_ShouldReturnEmptyList()
    {
        // Arrange
        // (No containers in cache)

        // Act
        var containers = await _repository.Containers.ToListAsync();

        // Assert
        containers.Should().BeEmpty();
    }

    [Fact(Timeout = 60000)]
    public async Task Containers_ShouldMapPropertiesCorrectly()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var metadata = new Dictionary<string, string> { ["key1"] = "value1" };
        await _fixture.CreateContainerAsync(containerName, metadata);
        var azuriteContainer = await _azuriteService.GetContainerAsync(containerName);
        await _context.UpsertContainerAsync(azuriteContainer);

        // Act
        var container = await _repository.Containers.FirstAsync(c => c.Name == containerName);

        // Assert
        container.Should().NotBeNull();
        container.Name.Should().Be(containerName);
        container.ETag.Should().NotBeNullOrWhiteSpace();
        container.LastModified.Should().BeAfter(DateTimeOffset.MinValue);
        container.Metadata.Should().BeEquivalentTo(metadata);
        container.PublicAccess.Should().Be("none");
    }
    #endregion

    #region Container Access - CreateContainerAsync
    [Fact(Timeout = 60000)]
    public async Task CreateContainerAsync_WithValidName_ShouldCreateContainerInAzuriteAndCache()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var dto = new CreateContainerDTO
        {
            ContainerName = containerName,
            Metadata = new Dictionary<string, string> { ["test"] = "value" }
        };

        // Act
        var result = await _repository.CreateContainerAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(containerName);
        result.Metadata.Should().BeEquivalentTo(dto.Metadata);

        // Verify in Azurite
        var azuriteContainer = await _azuriteService.GetContainerAsync(containerName);
        azuriteContainer.Should().NotBeNull();
        azuriteContainer.Name.Should().Be(containerName);

        // Verify in cache
        var cachedContainer = await _context.Containers.FirstOrDefaultAsync(c => c.Name == containerName);
        cachedContainer.Should().NotBeNull();
        cachedContainer!.Name.Should().Be(containerName);
    }

    [Fact(Timeout = 60000)]
    public async Task CreateContainerAsync_WithPublicAccess_ShouldSetPublicAccess()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var dto = new CreateContainerDTO
        {
            ContainerName = containerName,
            PublicAccess = "blob"
        };

        // Act
        var result = await _repository.CreateContainerAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.PublicAccess.Should().Be("blob");
    }

    [Fact(Timeout = 60000)]
    public async Task CreateContainerAsync_WhenContainerAlreadyExists_ShouldThrowResourceExistsException()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        await _fixture.CreateContainerAsync(containerName);
        var dto = new CreateContainerDTO { ContainerName = containerName };

        // Act
        Func<Task> act = async () => await _repository.CreateContainerAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ResourceExistsException>();
    }
    #endregion

    #region Container Access - DeleteContainerAsync
    [Fact(Timeout = 60000)]
    public async Task DeleteContainerAsync_WhenContainerExists_ShouldDeleteFromAzuriteAndCache()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var dto = new CreateContainerDTO { ContainerName = containerName };
        await _repository.CreateContainerAsync(dto);

        // Act
        await _repository.DeleteContainerAsync(containerName);

        // Assert
        // Verify deleted from Azurite
        Func<Task> act = async () => await _azuriteService.GetContainerAsync(containerName);
        await act.Should().ThrowAsync<ResourceNotFoundException>();

        // Verify deleted from cache
        var cachedContainer = await _context.Containers.FirstOrDefaultAsync(c => c.Name == containerName);
        cachedContainer.Should().BeNull();
    }

    [Fact(Timeout = 60000)]
    public async Task DeleteContainerAsync_WhenContainerDoesNotExist_ShouldNotThrow()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";

        // Act
        Func<Task> act = async () => await _repository.DeleteContainerAsync(containerName);

        // Assert
        await act.Should().NotThrowAsync();
    }
    #endregion

    #region Container Access - GetContainerAsync
    [Fact(Timeout = 60000)]
    public async Task GetContainerAsync_WhenContainerExistsInCache_ShouldReturnContainer()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var dto = new CreateContainerDTO
        {
            ContainerName = containerName,
            Metadata = new Dictionary<string, string> { ["key"] = "value" }
        };
        await _repository.CreateContainerAsync(dto);

        // Act
        var result = await _repository.GetContainerAsync(containerName);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(containerName);
        result.Metadata.Should().BeEquivalentTo(dto.Metadata);
    }

    [Fact(Timeout = 60000)]
    public async Task GetContainerAsync_WhenContainerNotInCache_ShouldReturnNull()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";

        // Act
        var result = await _repository.GetContainerAsync(containerName);

        // Assert
        result.Should().BeNull();
    }
    #endregion

    #region Container Access - UpdateContainerAsync
    [Fact(Timeout = 60000)]
    public async Task UpdateContainerAsync_WithNewMetadata_ShouldUpdateInAzuriteAndCache()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });

        var updateDto = new UpdateContainerDTO
        {
            ContainerName = containerName,
            Metadata = new Dictionary<string, string>
            {
                ["updated"] = "true",
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("o")
            }
        };

        // Act
        var result = await _repository.UpdateContainerAsync(updateDto);

        // Assert
        result.Should().NotBeNull();
        result.Metadata.Should().Contain(new KeyValuePair<string, string>("updated", "true"))
            .And.ContainKey("timestamp");

        // Verify in cache
        var cachedContainer = await _context.Containers.FirstAsync(c => c.Name == containerName);
        cachedContainer.Metadata.Should().BeEquivalentTo(updateDto.Metadata);
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateContainerAsync_WhenContainerDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var updateDto = new UpdateContainerDTO { ContainerName = containerName };

        // Act
        Func<Task> act = async () => await _repository.UpdateContainerAsync(updateDto);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }
    #endregion

    #region Blob Access - Blobs Property
    [Fact(Timeout = 60000)]
    public async Task Blobs_WhenBlobExistsInCache_ShouldReturnBlob()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });
        await _fixture.CreateBlobAsync(containerName, blobName, "test content");
        var azuriteBlob = await _azuriteService.GetBlobAsync(containerName, blobName);
        await _context.UpsertBlobAsync(azuriteBlob, containerName);

        // Act
        var blobs = await _repository.Blobs.ToListAsync();

        // Assert
        blobs.Should().NotBeEmpty()
            .And.Contain(b => b.Name == blobName && b.ContainerName == containerName);
    }

    [Fact(Timeout = 60000)]
    public async Task Blobs_WhenNoBlobsInCache_ShouldReturnEmptyList()
    {
        // Arrange
        // (No blobs in cache)

        // Act
        var blobs = await _repository.Blobs.ToListAsync();

        // Assert
        blobs.Should().BeEmpty();
    }

    [Fact(Timeout = 60000)]
    public async Task Blobs_ShouldMapPropertiesCorrectly()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        var content = "test content";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });
        await _fixture.CreateBlobAsync(containerName, blobName, content);
        var azuriteBlob = await _azuriteService.GetBlobAsync(containerName, blobName);
        await _context.UpsertBlobAsync(azuriteBlob, containerName);

        // Act
        var blob = await _repository.Blobs.FirstAsync(b => b.Name == blobName);

        // Assert
        blob.Should().NotBeNull();
        blob.Name.Should().Be(blobName);
        blob.ContainerName.Should().Be(containerName);
        blob.ContentType.Should().Be("text/plain");
        blob.ContentLength.Should().Be(content.Length);
        blob.BlobType.Should().Be("block");
    }
    #endregion

    #region Blob Access - DeleteBlobAsync
    [Fact(Timeout = 60000)]
    public async Task DeleteBlobAsync_WhenBlobExists_ShouldDeleteFromAzuriteAndCache()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });
        await _fixture.CreateBlobAsync(containerName, blobName, "test content");
        var azuriteBlob = await _azuriteService.GetBlobAsync(containerName, blobName);
        await _context.UpsertBlobAsync(azuriteBlob, containerName);

        // Act
        await _repository.DeleteBlobAsync(containerName, blobName);

        // Assert
        // Verify deleted from Azurite
        var exists = await _fixture.BlobExistsAsync(containerName, blobName);
        exists.Should().BeFalse();

        // Verify deleted from cache
        var cachedBlob = await _context.Blobs.FirstOrDefaultAsync(b => b.Name == blobName && b.ContainerName == containerName);
        cachedBlob.Should().BeNull();
    }

    [Fact(Timeout = 60000)]
    public async Task DeleteBlobAsync_WhenBlobDoesNotExist_ShouldNotThrow()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });

        // Act
        Func<Task> act = async () => await _repository.DeleteBlobAsync(containerName, blobName);

        // Assert
        await act.Should().NotThrowAsync();
    }
    #endregion

    #region Blob Access - GetBlobAsync
    [Fact(Timeout = 60000)]
    public async Task GetBlobAsync_WhenBlobExistsInCache_ShouldReturnBlob()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });
        await _fixture.CreateBlobAsync(containerName, blobName, "test content");
        var azuriteBlob = await _azuriteService.GetBlobAsync(containerName, blobName);
        await _context.UpsertBlobAsync(azuriteBlob, containerName);

        // Act
        var result = await _repository.GetBlobAsync(containerName, blobName);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(blobName);
        result.ContainerName.Should().Be(containerName);
    }

    [Fact(Timeout = 60000)]
    public async Task GetBlobAsync_WhenBlobNotInCache_ShouldReturnNull()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });

        // Act
        var result = await _repository.GetBlobAsync(containerName, blobName);

        // Assert
        result.Should().BeNull();
    }
    #endregion

    #region Blob Access - UpdateBlobAsync
    [Fact(Timeout = 60000)]
    public async Task UpdateBlobAsync_WithNewMetadata_ShouldUpdateInAzuriteAndCache()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });
        await _fixture.CreateBlobAsync(containerName, blobName, "test content");
        var azuriteBlob = await _azuriteService.GetBlobAsync(containerName, blobName);
        await _context.UpsertBlobAsync(azuriteBlob, containerName);

        var updateDto = new UpdateBlobDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            Metadata = new Dictionary<string, string> { ["updated"] = "true" },
            Tags = new Dictionary<string, string> { ["environment"] = "test" }
        };

        // Act
        var result = await _repository.UpdateBlobAsync(updateDto);

        // Assert
        result.Should().NotBeNull();
        result.Metadata.Should().BeEquivalentTo(updateDto.Metadata);
        result.Tags.Should().BeEquivalentTo(updateDto.Tags);

        // Verify in cache
        var cachedBlob = await _context.Blobs.FirstAsync(b => b.Name == blobName && b.ContainerName == containerName);
        cachedBlob.Metadata.Should().BeEquivalentTo(updateDto.Metadata);
        cachedBlob.Tags.Should().BeEquivalentTo(updateDto.Tags);
    }

    [Fact(Timeout = 60000)]
    public async Task UpdateBlobAsync_WhenBlobDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });
        var updateDto = new UpdateBlobDTO
        {
            ContainerName = containerName,
            BlobName = blobName
        };

        // Act
        Func<Task> act = async () => await _repository.UpdateBlobAsync(updateDto);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }
    #endregion

    #region Upload and Download - DownloadBlobAsync
    [Fact(Timeout = 60000)]
    public async Task DownloadBlobAsync_WhenBlobExists_ShouldDownloadBlob()
    {
        // Arrange
        var expectedContent = "Hello, StorageRepository!";
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });
        await _fixture.CreateBlobAsync(containerName, blobName, expectedContent);
        var azuriteBlob = await _azuriteService.GetBlobAsync(containerName, blobName);
        await _context.UpsertBlobAsync(azuriteBlob, containerName);

        // Act
        var result = await _repository.DownloadBlobAsync(containerName, blobName);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(blobName);
        result.ContainerName.Should().Be(containerName);
        result.ContentLength.Should().Be(expectedContent.Length);
        result.ContentType.Should().Be("text/plain");
        result.StatusCode.Should().Be(200);
        result.Content.Should().NotBeNull();

        using var reader = new StreamReader(result.Content!);
        var content = await reader.ReadToEndAsync();
        content.Should().Be(expectedContent);
    }

    [Fact(Timeout = 60000)]
    public async Task DownloadBlobAsync_WithRange_ShouldDownloadPartialContent()
    {
        // Arrange
        var expectedContent = "0123456789";
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });
        await _fixture.CreateBlobAsync(containerName, blobName, expectedContent);
        var azuriteBlob = await _azuriteService.GetBlobAsync(containerName, blobName);
        await _context.UpsertBlobAsync(azuriteBlob, containerName);

        // Act
        var result = await _repository.DownloadBlobAsync(containerName, blobName, "bytes=0-4");

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(206);
        result.ContentRange.Should().Be("bytes 0-4/10");

        using var reader = new StreamReader(result.Content!);
        var content = await reader.ReadToEndAsync();
        content.Should().Be("01234");
    }

    [Fact(Timeout = 60000)]
    public async Task DownloadBlobAsync_WhenBlobNotInCache_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });

        // Act
        Func<Task> act = async () => await _repository.DownloadBlobAsync(containerName, blobName);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }
    #endregion

    #region Upload and Download - CreateUploadAsync
    [Fact(Timeout = 60000)]
    public async Task CreateUploadAsync_WithValidRequest_ShouldCreateUploadSession()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });

        var uploadDto = new CreateUploadRequestDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            ContentLength = 1024,
            ContentType = "text/plain",
            Metadata = new Dictionary<string, string> { ["test"] = "value" }
        };

        // Act
        var result = await _repository.CreateUploadAsync(uploadDto);

        // Assert
        result.Should().NotBeNull();
        result.UploadId.Should().NotBeEmpty();
        result.ContainerName.Should().Be(containerName);
        result.BlobName.Should().Be(blobName);
        result.ContentLength.Should().Be(1024);
        result.UploadedLength.Should().Be(0);
        result.UploadedBlocks.Should().BeEmpty();

        // Verify in database
        var upload = await _context.Uploads.FirstOrDefaultAsync(u => u.UploadId == result.UploadId);
        upload.Should().NotBeNull();
        upload!.BlobName.Should().Be(blobName);
        upload.ContainerName.Should().Be(containerName);
        upload.ContentLength.Should().Be(1024);
    }

    [Fact(Timeout = 60000)]
    public async Task CreateUploadAsync_WhenContainerDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";

        var uploadDto = new CreateUploadRequestDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            ContentLength = 1024
        };

        // Act
        Func<Task> act = async () => await _repository.CreateUploadAsync(uploadDto);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact(Timeout = 60000)]
    public async Task CreateUploadAsync_WhenBlobAlreadyExists_ShouldThrowResourceExistsException()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });
        await _fixture.CreateBlobAsync(containerName, blobName, "existing content");
        var azuriteBlob = await _azuriteService.GetBlobAsync(containerName, blobName);
        await _context.UpsertBlobAsync(azuriteBlob, containerName);

        var uploadDto = new CreateUploadRequestDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            ContentLength = 1024
        };

        // Act
        Func<Task> act = async () => await _repository.CreateUploadAsync(uploadDto);

        // Assert
        await act.Should().ThrowAsync<ResourceExistsException>();
    }
    #endregion

    #region Upload and Download - UploadBlockAsync
    [Fact(Timeout = 60000)]
    public async Task UploadBlockAsync_WithValidBlock_ShouldUploadBlockAndUpdateCache()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });

        var uploadDto = new CreateUploadRequestDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            ContentLength = 100
        };
        var result = await _repository.CreateUploadAsync(uploadDto);
        var uploadId = result.UploadId;

        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block-1"));
        var content = new MemoryStream(Encoding.UTF8.GetBytes("Block content"));

        // Act
        await _repository.UploadBlockAsync(uploadId, blockId, content);

        // Assert
        // Verify in database
        var block = await _context.UploadBlocks.FirstOrDefaultAsync(b => b.UploadId == uploadId && b.BlockId == blockId);
        block.Should().NotBeNull();
        block!.BlockSize.Should().Be(content.Length);
    }

    [Fact(Timeout = 60000)]
    public async Task UploadBlockAsync_WhenUploadSessionDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var uploadId = Guid.NewGuid();
        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block-1"));
        var content = new MemoryStream(Encoding.UTF8.GetBytes("Block content"));

        // Act
        Func<Task> act = async () => await _repository.UploadBlockAsync(uploadId, blockId, content);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact(Timeout = 60000)]
    public async Task UploadBlockAsync_WithInvalidBlockId_ShouldThrowAzuriteServiceException()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });

        var uploadDto = new CreateUploadRequestDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            ContentLength = 100
        };
        var result = await _repository.CreateUploadAsync(uploadDto);
        var uploadId = result.UploadId;

        var invalidBlockId = "not-base64!@#";
        var content = new MemoryStream(Encoding.UTF8.GetBytes("Block content"));

        // Act
        Func<Task> act = async () => await _repository.UploadBlockAsync(uploadId, invalidBlockId, content);

        // Assert
        await act.Should().ThrowAsync<AzuriteServiceException>();
    }
    #endregion

    #region Upload and Download - GetUploadStatusAsync
    [Fact(Timeout = 60000)]
    public async Task GetUploadStatusAsync_WhenUploadSessionExists_ShouldReturnStatus()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });

        var uploadDto = new CreateUploadRequestDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            ContentLength = 200,
            ContentType = "text/plain"
        };
        var result = await _repository.CreateUploadAsync(uploadDto);
        var uploadId = result.UploadId;

        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block-1"));
        var content = new MemoryStream(Encoding.UTF8.GetBytes("Block content"));
        await _repository.UploadBlockAsync(uploadId, blockId, content);

        // Act
        var status = await _repository.GetUploadStatusAsync(uploadId);

        // Assert
        status.Should().NotBeNull();
        status.UploadId.Should().Be(uploadId);
        status.ContainerName.Should().Be(containerName);
        status.BlobName.Should().Be(blobName);
        status.ContentLength.Should().Be(200);
        status.UploadedBlocks.Should().Contain(blockId);
        status.UploadedLength.Should().Be(content.Length);
    }

    [Fact(Timeout = 60000)]
    public async Task GetUploadStatusAsync_WhenUploadSessionDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var uploadId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await _repository.GetUploadStatusAsync(uploadId);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }
    #endregion

    #region Upload and Download - CommitUploadAsync
    [Fact(Timeout = 60000)]
    public async Task CommitUploadAsync_WithValidBlocks_ShouldCommitBlobAndUpdateCache()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });

        var uploadDto = new CreateUploadRequestDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            ContentLength = 100,
            ContentType = "text/plain",
            Metadata = new Dictionary<string, string> { ["test"] = "commit" }
        };
        var createResult = await _repository.CreateUploadAsync(uploadDto);
        var uploadId = createResult.UploadId;

        var blockId1 = Convert.ToBase64String(Encoding.UTF8.GetBytes("block-1"));
        var content1 = new MemoryStream(Encoding.UTF8.GetBytes("First "));
        await _repository.UploadBlockAsync(uploadId, blockId1, content1);

        var blockId2 = Convert.ToBase64String(Encoding.UTF8.GetBytes("block-2"));
        var content2 = new MemoryStream(Encoding.UTF8.GetBytes("Second"));
        await _repository.UploadBlockAsync(uploadId, blockId2, content2);

        // Act
        var result = await _repository.CommitUploadAsync(uploadId, [blockId1, blockId2]);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(blobName);
        result.ContainerName.Should().Be(containerName);
        result.ContentType.Should().Be("text/plain");
        result.Metadata.Should().ContainKey("test");

        // Verify blob exists in Azurite
        var exists = await _fixture.BlobExistsAsync(containerName, blobName);
        exists.Should().BeTrue();

        // Verify blob in cache
        var cachedBlob = await _context.Blobs.FirstOrDefaultAsync(b => b.Name == blobName && b.ContainerName == containerName);
        cachedBlob.Should().NotBeNull();

        // Verify content
        var downloadResult = await _repository.DownloadBlobAsync(containerName, blobName);
        using var reader = new StreamReader(downloadResult.Content!);
        var downloadedContent = await reader.ReadToEndAsync();
        downloadedContent.Should().Be("First Second");

        // Note: Upload session cleanup is intentionally not implemented in CommitUploadAsync
        // The upload session remains in the database for potential audit/tracking purposes
    }

    [Fact(Timeout = 60000)]
    public async Task CommitUploadAsync_WithMissingBlocks_ShouldThrowAzuriteServiceException()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });

        var uploadDto = new CreateUploadRequestDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            ContentLength = 100
        };
        var result = await _repository.CreateUploadAsync(uploadDto);
        var uploadId = result.UploadId;

        var blockId1 = Convert.ToBase64String(Encoding.UTF8.GetBytes("block-1"));
        var content1 = new MemoryStream(Encoding.UTF8.GetBytes("First"));
        await _repository.UploadBlockAsync(uploadId, blockId1, content1);

        var blockId2 = Convert.ToBase64String(Encoding.UTF8.GetBytes("block-2"));

        // Act - try to commit with a block that wasn't uploaded
        Func<Task> act = async () => await _repository.CommitUploadAsync(uploadId, [blockId1, blockId2]);

        // Assert
        await act.Should().ThrowAsync<AzuriteServiceException>();
    }

    [Fact(Timeout = 60000)]
    public async Task CommitUploadAsync_WhenUploadSessionDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var uploadId = Guid.NewGuid();
        var blockIds = new[] { Convert.ToBase64String(Encoding.UTF8.GetBytes("block-1")) };

        // Act
        Func<Task> act = async () => await _repository.CommitUploadAsync(uploadId, blockIds);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }
    #endregion

    #region Upload and Download - CancelUploadAsync
    [Fact(Timeout = 60000)]
    public async Task CancelUploadAsync_WhenUploadSessionExists_ShouldRemoveSession()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });

        var uploadDto = new CreateUploadRequestDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            ContentLength = 100
        };
        var result = await _repository.CreateUploadAsync(uploadDto);
        var uploadId = result.UploadId;

        // Act
        await _repository.CancelUploadAsync(uploadId);

        // Assert
        var upload = await _context.Uploads.FirstOrDefaultAsync(u => u.UploadId == uploadId);
        upload.Should().BeNull();
    }

    [Fact(Timeout = 60000)]
    public async Task CancelUploadAsync_WhenUploadSessionDoesNotExist_ShouldNotThrow()
    {
        // Arrange
        var uploadId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await _repository.CancelUploadAsync(uploadId);

        // Assert
        await act.Should().NotThrowAsync();
    }
    #endregion

    #region Upload and Download - Uploads Property
    [Fact(Timeout = 60000)]
    public async Task Uploads_WhenUploadSessionsExist_ShouldReturnUploads()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });

        var uploadDto = new CreateUploadRequestDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            ContentLength = 1000
        };
        var result = await _repository.CreateUploadAsync(uploadDto);
        var uploadId = result.UploadId;

        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block-1"));
        var content = new MemoryStream(Encoding.UTF8.GetBytes("Block content"));
        await _repository.UploadBlockAsync(uploadId, blockId, content);

        // Act
        var uploads = await _repository.Uploads.ToListAsync();

        // Assert
        uploads.Should().NotBeEmpty();
        var upload = uploads.First(u => u.Id == uploadId);
        upload.ContainerName.Should().Be(containerName);
        upload.Name.Should().Be(blobName);
        upload.Progress.Should().BeGreaterThan(0);
    }

    [Fact(Timeout = 60000)]
    public async Task Uploads_WhenNoUploadSessions_ShouldReturnEmptyList()
    {
        // Arrange
        // (No upload sessions)

        // Act
        var uploads = await _repository.Uploads.ToListAsync();

        // Assert
        uploads.Should().BeEmpty();
    }
    #endregion

    #region Write-Through Caching Tests
    [Fact(Timeout = 60000)]
    public async Task WriteThrough_CreateContainer_ShouldUpdateBothAzuriteAndCache()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var dto = new CreateContainerDTO
        {
            ContainerName = containerName,
            Metadata = new Dictionary<string, string> { ["source"] = "test" }
        };

        // Act
        var result = await _repository.CreateContainerAsync(dto);

        // Assert
        // Verify Azurite has the container
        var azuriteContainer = await _azuriteService.GetContainerAsync(containerName);
        azuriteContainer.Should().NotBeNull();
        azuriteContainer.Metadata.Should().ContainKey("source");

        // Verify cache has the container
        var cachedContainer = await _context.Containers.FirstOrDefaultAsync(c => c.Name == containerName);
        cachedContainer.Should().NotBeNull();
        cachedContainer!.Metadata.Should().ContainKey("source");

        // Verify repository returns cached data
        var repoContainer = await _repository.GetContainerAsync(containerName);
        repoContainer.Should().NotBeNull();
        repoContainer!.Metadata.Should().BeEquivalentTo(dto.Metadata);
    }

    [Fact(Timeout = 60000)]
    public async Task WriteThrough_UpdateBlob_ShouldSyncAzuriteAndCache()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });
        await _fixture.CreateBlobAsync(containerName, blobName, "test content");
        var azuriteBlob = await _azuriteService.GetBlobAsync(containerName, blobName);
        await _context.UpsertBlobAsync(azuriteBlob, containerName);

        var updateDto = new UpdateBlobDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            Metadata = new Dictionary<string, string> { ["version"] = "2" }
        };

        // Act
        var result = await _repository.UpdateBlobAsync(updateDto);

        // Assert
        // Verify Azurite has updated metadata
        var updatedAzuriteBlob = await _azuriteService.GetBlobAsync(containerName, blobName);
        updatedAzuriteBlob.Metadata.Should().ContainKey("version");

        // Verify cache has updated metadata
        var cachedBlob = await _context.Blobs.FirstAsync(b => b.Name == blobName && b.ContainerName == containerName);
        cachedBlob.Metadata.Should().ContainKey("version");

        // Verify ETags changed
        result.ETag.Should().NotBe(azuriteBlob.ETag);
    }

    [Fact(Timeout = 60000)]
    public async Task WriteThrough_DeleteBlob_ShouldRemoveFromBothAzuriteAndCache()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });
        await _fixture.CreateBlobAsync(containerName, blobName, "test content");
        var azuriteBlob = await _azuriteService.GetBlobAsync(containerName, blobName);
        await _context.UpsertBlobAsync(azuriteBlob, containerName);

        // Act
        await _repository.DeleteBlobAsync(containerName, blobName);

        // Assert
        // Verify not in Azurite
        var exists = await _fixture.BlobExistsAsync(containerName, blobName);
        exists.Should().BeFalse();

        // Verify not in cache
        var cachedBlob = await _context.Blobs.FirstOrDefaultAsync(b => b.Name == blobName && b.ContainerName == containerName);
        cachedBlob.Should().BeNull();

        // Verify repository query returns null
        var repoBlob = await _repository.GetBlobAsync(containerName, blobName);
        repoBlob.Should().BeNull();
    }

    [Fact(Timeout = 60000)]
    public async Task WriteThrough_CommitUpload_ShouldCreateBlobInBothSystems()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _repository.CreateContainerAsync(new CreateContainerDTO { ContainerName = containerName });

        var uploadDto = new CreateUploadRequestDTO
        {
            ContainerName = containerName,
            BlobName = blobName,
            ContentLength = 50
        };
        var createResult = await _repository.CreateUploadAsync(uploadDto);
        var uploadId = createResult.UploadId;

        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block-1"));
        var content = new MemoryStream(Encoding.UTF8.GetBytes("Test content"));
        await _repository.UploadBlockAsync(uploadId, blockId, content);

        // Act
        var result = await _repository.CommitUploadAsync(uploadId, [blockId]);

        // Assert
        // Verify in Azurite
        var azuriteBlob = await _azuriteService.GetBlobAsync(containerName, blobName);
        azuriteBlob.Should().NotBeNull();

        // Verify in cache
        var cachedBlob = await _context.Blobs.FirstOrDefaultAsync(b => b.Name == blobName && b.ContainerName == containerName);
        cachedBlob.Should().NotBeNull();

        // Verify repository query returns the blob
        var repoBlob = await _repository.GetBlobAsync(containerName, blobName);
        repoBlob.Should().NotBeNull();
        repoBlob!.Name.Should().Be(blobName);
    }
    #endregion

    #region GetDashboardData Tests    
    #region Basic Tests

    [Fact(Timeout = 60000)]
    public async Task GetDashboardDataAsync_WithZeroContainers_ShouldReturnEmptyDashboard()
    {
        // Arrange
        // (No containers or blobs in the database)

        // Act
        var result = await _repository.GetDashboardDataAsync();

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

    [Fact(Timeout = 60000)]
    public async Task GetDashboardDataAsync_WithTenContainersAndBlobs_ShouldReturnAll()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            var containerName = $"container{i:D2}";
            await _fixture.CreateContainerAsync(containerName);
            var azuriteContainer = await _azuriteService.GetContainerAsync(containerName);
            await _context.UpsertContainerAsync(azuriteContainer);

            await _fixture.CreateBlobAsync(containerName, $"blob{i}.txt", $"content{i}");
            var azuriteBlob = await _azuriteService.GetBlobAsync(containerName, $"blob{i}.txt");
            await _context.UpsertBlobAsync(azuriteBlob, containerName);

            // Small delay to ensure different timestamps
            await Task.Delay(50);
        }

        // Act
        var result = await _repository.GetDashboardDataAsync();

        // Assert
        result.Should().NotBeNull();
        result.Stats.Containers.Should().Be(10);
        result.Stats.Blobs.Should().Be(10);
        result.Stats.TotalBlobSize.Should().BeGreaterThan(0);
        result.RecentContainers.Should().HaveCount(10);
        result.RecentBlobs.Should().HaveCount(10);
    }

    [Fact(Timeout = 60000)]
    public async Task GetDashboardDataAsync_WithTwentyContainersAndBlobs_ShouldReturnOnlyTenRecent()
    {
        // Arrange
        for (int i = 0; i < 20; i++)
        {
            var containerName = $"container{i:D2}";
            await _fixture.CreateContainerAsync(containerName);
            var azuriteContainer = await _azuriteService.GetContainerAsync(containerName);
            await _context.UpsertContainerAsync(azuriteContainer);

            await _fixture.CreateBlobAsync(containerName, $"blob{i}.txt", $"content{i}");
            var azuriteBlob = await _azuriteService.GetBlobAsync(containerName, $"blob{i}.txt");
            await _context.UpsertBlobAsync(azuriteBlob, containerName);

            // Small delay to ensure different timestamps
            await Task.Delay(50);
        }

        // Act
        var result = await _repository.GetDashboardDataAsync();

        // Assert
        result.Should().NotBeNull();
        result.Stats.Containers.Should().Be(20, "stats should include all containers");
        result.Stats.Blobs.Should().Be(20, "stats should include all blobs");
        result.RecentContainers.Should().HaveCount(10, "should return only 10 most recent containers");
        result.RecentBlobs.Should().HaveCount(10, "should return only 10 most recent blobs");
    }

    #endregion

    #region Container LastModified Tests

    [Fact(Timeout = 60000)]
    public async Task GetDashboardDataAsync_ContainerLastModified_ShouldUseMaxOfContainerAndBlobTimes()
    {
        // Arrange
        var containerName = "test-container";
        await _fixture.CreateContainerAsync(containerName);
        var azuriteContainer = await _azuriteService.GetContainerAsync(containerName);
        await _context.UpsertContainerAsync(azuriteContainer);
        var containerLastModified = azuriteContainer.LastModified;

        // Add a delay and create a blob (should have a later timestamp)
        await Task.Delay(200);
        await _fixture.CreateBlobAsync(containerName, "blob.txt", "content");
        var azuriteBlob = await _azuriteService.GetBlobAsync(containerName, "blob.txt");
        await _context.UpsertBlobAsync(azuriteBlob, containerName);

        // Get the updated container (Azure updates container LastModified when blobs are added)
        var updatedContainer = await _azuriteService.GetContainerAsync(containerName);
        await _context.UpsertContainerAsync(updatedContainer);

        // Act
        var result = await _repository.GetDashboardDataAsync();

        // Assert
        result.Should().NotBeNull();
        result.RecentContainers.Should().HaveCount(1);
        var container = result.RecentContainers.First();

        // The dashboard should use the max of container and blob timestamps
        var expectedTimestamp = azuriteBlob.LastModified > updatedContainer.LastModified
            ? azuriteBlob.LastModified
            : updatedContainer.LastModified;
        container.LastModified.Should().Be(expectedTimestamp);
    }

    [Fact(Timeout = 60000)]
    public async Task GetDashboardDataAsync_ContainerWithoutBlobs_ShouldUseContainerLastModified()
    {
        // Arrange
        var containerName = "empty-container";
        await _fixture.CreateContainerAsync(containerName);
        var azuriteContainer = await _azuriteService.GetContainerAsync(containerName);
        await _context.UpsertContainerAsync(azuriteContainer);

        // Act
        var result = await _repository.GetDashboardDataAsync();

        // Assert
        result.Should().NotBeNull();
        result.RecentContainers.Should().HaveCount(1);
        var container = result.RecentContainers.First();
        container.Name.Should().Be(containerName);
        container.LastModified.Should().Be(azuriteContainer.LastModified);
        container.BlobCount.Should().Be(0);
        container.TotalSize.Should().Be(0);
    }

    #endregion

    #region Image Size Calculation Tests

    [Fact(Timeout = 60000)]
    public async Task GetDashboardDataAsync_ImageSize_ShouldIncludeOnlyImageContentTypes()
    {
        // Arrange
        var containerName = "test-container";
        await _fixture.CreateContainerAsync(containerName);
        var azuriteContainer = await _azuriteService.GetContainerAsync(containerName);
        await _context.UpsertContainerAsync(azuriteContainer);

        // Create image blobs
        await _fixture.CreateBlobAsync(containerName, "image1.jpg", "fake-jpg-content", "image/jpeg");
        var blob1 = await _azuriteService.GetBlobAsync(containerName, "image1.jpg");
        await _context.UpsertBlobAsync(blob1, containerName);

        await _fixture.CreateBlobAsync(containerName, "image2.png", "fake-png-content", "image/png");
        var blob2 = await _azuriteService.GetBlobAsync(containerName, "image2.png");
        await _context.UpsertBlobAsync(blob2, containerName);

        // Create non-image blobs
        await _fixture.CreateBlobAsync(containerName, "text.txt", "text-content", "text/plain");
        var blob3 = await _azuriteService.GetBlobAsync(containerName, "text.txt");
        await _context.UpsertBlobAsync(blob3, containerName);

        // Act
        var result = await _repository.GetDashboardDataAsync();

        // Assert
        result.Should().NotBeNull();
        result.Stats.Blobs.Should().Be(3);
        result.Stats.TotalBlobSize.Should().BeGreaterThan(0);
        result.Stats.TotalImageSize.Should().BeGreaterThan(0);
        result.Stats.TotalImageSize.Should().BeLessThan(result.Stats.TotalBlobSize);

        long expectedImageSize = blob1.ContentLength + blob2.ContentLength;
        result.Stats.TotalImageSize.Should().Be(expectedImageSize);
    }

    [Fact(Timeout = 60000)]
    public async Task GetDashboardDataAsync_ImageSize_WithNoImages_ShouldBeZero()
    {
        // Arrange
        var containerName = "test-container";
        await _fixture.CreateContainerAsync(containerName);
        var azuriteContainer = await _azuriteService.GetContainerAsync(containerName);
        await _context.UpsertContainerAsync(azuriteContainer);

        await _fixture.CreateBlobAsync(containerName, "text.txt", "text-content", "text/plain");
        var blob = await _azuriteService.GetBlobAsync(containerName, "text.txt");
        await _context.UpsertBlobAsync(blob, containerName);

        // Act
        var result = await _repository.GetDashboardDataAsync();

        // Assert
        result.Should().NotBeNull();
        result.Stats.TotalImageSize.Should().Be(0);
        result.Stats.TotalBlobSize.Should().BeGreaterThan(0);
    }

    #endregion

    #region Blob Ordering and Limiting Tests

    [Fact(Timeout = 60000)]
    public async Task GetDashboardDataAsync_RecentBlobs_ShouldBeOrderedByLastModifiedDescending()
    {
        // Arrange
        var containerName = "test-container";
        await _fixture.CreateContainerAsync(containerName);
        var azuriteContainer = await _azuriteService.GetContainerAsync(containerName);
        await _context.UpsertContainerAsync(azuriteContainer);

        for (int i = 0; i < 5; i++)
        {
            await _fixture.CreateBlobAsync(containerName, $"blob{i}.txt", $"content{i}");
            var blob = await _azuriteService.GetBlobAsync(containerName, $"blob{i}.txt");
            await _context.UpsertBlobAsync(blob, containerName);
            await Task.Delay(100); // Ensure different timestamps
        }

        // Act
        var result = await _repository.GetDashboardDataAsync();

        // Assert
        result.Should().NotBeNull();
        result.RecentBlobs.Should().HaveCount(5);

        var lastModifiedDates = result.RecentBlobs.Select(b => b.LastModified).ToList();
        lastModifiedDates.Should().BeInDescendingOrder();
    }

    [Fact(Timeout = 60000)]
    public async Task GetDashboardDataAsync_ContainerInfo_ShouldIncludeCorrectBlobCountAndTotalSize()
    {
        // Arrange
        var containerName = "test-container";
        await _fixture.CreateContainerAsync(containerName);
        var azuriteContainer = await _azuriteService.GetContainerAsync(containerName);
        await _context.UpsertContainerAsync(azuriteContainer);

        long expectedSize = 0;
        for (int i = 0; i < 3; i++)
        {
            var content = $"content{i}";
            await _fixture.CreateBlobAsync(containerName, $"blob{i}.txt", content);
            var blob = await _azuriteService.GetBlobAsync(containerName, $"blob{i}.txt");
            await _context.UpsertBlobAsync(blob, containerName);
            expectedSize += blob.ContentLength;
        }

        // Act
        var result = await _repository.GetDashboardDataAsync();

        // Assert
        result.Should().NotBeNull();
        result.RecentContainers.Should().HaveCount(1);
        var container = result.RecentContainers.First();
        container.BlobCount.Should().Be(3);
        container.TotalSize.Should().Be(expectedSize);
    }

    #endregion
    #endregion
}
