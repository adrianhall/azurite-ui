using AzuriteUI.Web.Services.Azurite;
using AzuriteUI.Web.Services.Azurite.Exceptions;
using AzuriteUI.Web.Services.Azurite.Models;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace AzuriteUI.Web.IntegrationTests.Services.Azurite;

/// <summary>
/// Integration tests for the <see cref="AzuriteService"/> class.
/// </summary>
[ExcludeFromCodeCoverage]
public class AzuriteService_Tests : IClassFixture<AzuriteFixture>, IAsyncLifetime
{
    private readonly AzuriteFixture _fixture;
    private readonly IAzuriteService _service;
    private readonly List<string> _containersToCleanup = [];

    public AzuriteService_Tests(AzuriteFixture fixture)
    {
        _fixture = fixture;
        _service = new AzuriteService(_fixture.ConnectionString, NullLogger<AzuriteService>.Instance);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _fixture.CleanupAsync();
        GC.SuppressFinalize(this);
    }

    #region ConnectionString Property
    [Fact]
    public void ConnectionString_ShouldReturnValidConnectionString()
    {
        // Arrange & Act
        var connectionString = _service.ConnectionString;

        // Assert
        connectionString.Should().NotBeNullOrWhiteSpace();
        connectionString.Should().Contain("AccountName=");
    }
    #endregion

    #region GetHealthStatusAsync
    [Fact]
    public async Task GetHealthStatusAsync_WhenAzuriteIsRunning_ShouldReturnHealthyStatus()
    {
        // Arrange
        // (Nothing to arrange, service is already running)

        // Act
        var result = await _service.GetHealthStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsHealthy.Should().BeTrue();
        result.ConnectionString.Should().Be(_service.ConnectionString);
        result.ResponseTimeMilliseconds.Should().BePositive();
        result.ErrorMessage.Should().BeNull();
    }
    #endregion

    #region CreateContainerAsync
    [Fact]
    public async Task CreateContainerAsync_WithValidName_ShouldCreateContainer()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var properties = new AzuriteContainerProperties();

        // Act
        var result = await _service.CreateContainerAsync(containerName, properties);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(containerName);
    }

    [Fact]
    public async Task CreateContainerAsync_WithMetadata_ShouldCreateContainerWithMetadata()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var properties = new AzuriteContainerProperties
        {
            Metadata = new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            }
        };

        // Act
        var result = await _service.CreateContainerAsync(containerName, properties);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(containerName);
        result.Metadata.Should().BeEquivalentTo(properties.Metadata);
    }

    [Fact]
    public async Task CreateContainerAsync_WhenContainerAlreadyExists_ShouldThrowResourceExistsException()
    {
        // Arrange
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");
        var properties = new AzuriteContainerProperties();

        // Act
        Func<Task> act = async () => await _service.CreateContainerAsync(containerName, properties);

        // Assert
        await act.Should().ThrowAsync<ResourceExistsException>();
    }

    [Fact]
    public async Task CreateContainerAsync_WithInvalidContainerName_ShouldThrowAzuriteServiceException()
    {
        // Arrange
        // Container names must be lowercase; uppercase letters are invalid
        var invalidContainerName = "InvalidContainerName";
        var properties = new AzuriteContainerProperties();

        // Act
        Func<Task> act = async () => await _service.CreateContainerAsync(invalidContainerName, properties);

        // Assert
        var exception = await act.Should().ThrowAsync<AzuriteServiceException>();
        exception.Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateContainerAsync_WithContainerNameTooShort_ShouldThrowAzuriteServiceException()
    {
        // Arrange
        // Container names must be at least 3 characters
        var invalidContainerName = "ab";
        var properties = new AzuriteContainerProperties();

        // Act
        Func<Task> act = async () => await _service.CreateContainerAsync(invalidContainerName, properties);

        // Assert
        var exception = await act.Should().ThrowAsync<AzuriteServiceException>();
        exception.Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateContainerAsync_WithInvalidCharacters_ShouldThrowAzuriteServiceException()
    {
        // Arrange
        // Container names cannot contain underscores or special characters
        var invalidContainerName = "test_container";
        var properties = new AzuriteContainerProperties();

        // Act
        Func<Task> act = async () => await _service.CreateContainerAsync(invalidContainerName, properties);

        // Assert
        var exception = await act.Should().ThrowAsync<AzuriteServiceException>();
        exception.Which.StatusCode.Should().Be(400);
    }
    #endregion

    #region DeleteContainerAsync
    [Fact]
    public async Task DeleteContainerAsync_WhenContainerExists_ShouldDeleteContainer()
    {
        // Arrange
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");

        // Act
        await _service.DeleteContainerAsync(containerName);

        // Assert
        Func<Task> act = async () => await _service.GetContainerAsync(containerName);
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task DeleteContainerAsync_WhenContainerDoesNotExist_ShouldNotThrow()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";

        // Act
        Func<Task> act = async () => await _service.DeleteContainerAsync(containerName);

        // Assert
        await act.Should().NotThrowAsync();
    }
    #endregion

    #region GetContainerAsync
    [Fact]
    public async Task GetContainerAsync_WhenContainerExists_ShouldReturnContainer()
    {
        // Arrange
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");

        // Act
        var result = await _service.GetContainerAsync(containerName);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(containerName);
    }

    [Fact]
    public async Task GetContainerAsync_WhenContainerHasMetadata_ShouldReturnMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}", metadata: metadata);

        // Act
        var result = await _service.GetContainerAsync(containerName);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(containerName);
        result.Metadata.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public async Task GetContainerAsync_WhenContainerDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";

        // Act
        Func<Task> act = async () => await _service.GetContainerAsync(containerName);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }
    #endregion

    #region GetContainersAsync
    [Fact]
    public async Task GetContainersAsync_WhenContainersExist_ShouldReturnContainers()
    {
        // Arrange
        var containerName1 = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");
        var containerName2 = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");

        // Act
        var results = await _service.GetContainersAsync().ToListAsync();

        // Assert
        results.Should().NotBeEmpty()
            .And.HaveCount(2)
            .And.Contain(x => x.Name == containerName1)
            .And.Contain(x => x.Name == containerName2);
    }

    [Fact]
    public async Task GetContainersAsync_WhenNoContainersExist_ShouldReturnEmptySequence()
    {
        // Arrange
        await _fixture.CleanupAsync();

        // Act
        var results = await _service.GetContainersAsync().ToListAsync();

        // Assert
        results.Should().BeEmpty();
    }
    #endregion

    #region UpdateContainerAsync
    [Fact]
    public async Task UpdateContainerAsync_WithNewMetadata_ShouldUpdateContainer()
    {
        // Arrange
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");
        var updatedProperties = new AzuriteContainerProperties
        {
            Metadata = new Dictionary<string, string>
            {
                ["updated"] = "true",
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("o")
            }
        };

        // Act
        var result = await _service.UpdateContainerAsync(containerName, updatedProperties);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(containerName);
        result.Metadata.Should()
            .Contain(new KeyValuePair<string, string>("updated", "true"))
            .And.ContainKey("timestamp"); 
    }

    [Fact]
    public async Task UpdateContainerAsync_WhenContainerDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var properties = new AzuriteContainerProperties();

        // Act
        Func<Task> act = async () => await _service.UpdateContainerAsync(containerName, properties);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }
    #endregion

    #region DeleteBlobAsync
    [Fact]
    public async Task DeleteBlobAsync_WhenBlobExists_ShouldDeleteBlob()
    {
        // Arrange
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");
        var blobName = await _fixture.CreateBlobAsync(containerName, $"test-blob-{Guid.NewGuid():N}.txt", "test content");

        // Act
        await _service.DeleteBlobAsync(containerName, blobName);

        // Assert
        bool exists = await _fixture.BlobExistsAsync(containerName, blobName);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteBlobAsync_WhenBlobDoesNotExist_ShouldNotThrow()
    {
        // Arrange
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";

        // Act
        Func<Task> act = async () => await _service.DeleteBlobAsync(containerName, blobName);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteBlobAsync_WhenContainerDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";

        // Act
        Func<Task> act = async () => await _service.DeleteBlobAsync(containerName, blobName);

        // Assert
        await act.Should().NotThrowAsync();
    }
    #endregion

    #region DownloadBlobAsync
    [Fact]
    public async Task DownloadBlobAsync_WhenBlobExists_ShouldDownloadBlob()
    {
        // Arrange
        var expectedContent = "Hello, Azurite!";
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");
        var blobName = await _fixture.CreateBlobAsync(containerName, $"test-blob-{Guid.NewGuid():N}.txt", expectedContent);

        // Act
        var result = await _service.DownloadBlobAsync(containerName, blobName);

        // Assert
        result.Should().NotBeNull();
        result.ContentLength.Should().Be(expectedContent.Length);
        result.ContentType.Should().Be("text/plain");
        result.ContentRange.Should().BeNull();
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);

        result.Content.Should().NotBeNull();
        using var reader = new StreamReader(result.Content);
        var content = await reader.ReadToEndAsync();
        content.Should().Be(expectedContent);
    }

    [Fact]
    public async Task DownloadBlobAsync_WithRange_ShouldDownloadPartialContent()
    {
        // Arrange
        var expectedContent = "0123456789";
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");
        var blobName = await _fixture.CreateBlobAsync(containerName, $"test-blob-{Guid.NewGuid():N}.txt", expectedContent);

        // Act
        var result = await _service.DownloadBlobAsync(containerName, blobName, "bytes=0-4");

        // Assert
        result.Should().NotBeNull();
        result.ContentLength.Should().Be(5);
        result.ContentType.Should().Be("text/plain");
        result.ContentRange.Should().Be("bytes 0-4/10");
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(206);

        result.Content.Should().NotBeNull();
        using var reader = new StreamReader(result.Content);
        var content = await reader.ReadToEndAsync();
        content.Should().Be("01234");
    }

    [Fact]
    public async Task DownloadBlobAsync_WhenBlobDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";

        // Act
        Func<Task> act = async () => await _service.DownloadBlobAsync(containerName, blobName);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task DownloadBlobAsync_WhenContainerDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";

        // Act
        Func<Task> act = async () => await _service.DownloadBlobAsync(containerName, blobName);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }
    #endregion

    #region GetBlobAsync
    [Fact]
    public async Task GetBlobAsync_WhenBlobExists_ShouldReturnBlob()
    {
        // Arrange
        var expectedContent = "test content";
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");
        var blobName = await _fixture.CreateBlobAsync(containerName, $"test-blob-{Guid.NewGuid():N}.txt", expectedContent);

        // Act
        var result = await _service.GetBlobAsync(containerName, blobName);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(blobName);
        result.ETag.Should().NotBeNullOrWhiteSpace();
        result.ContentLength.Should().Be(expectedContent.Length);
        result.ContentType.Should().Be("text/plain");
    }

    [Fact]
    public async Task GetBlobAsync_WhenBlobDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";

        // Act
        Func<Task> act = async () => await _service.GetBlobAsync(containerName, blobName);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task GetBlobAsync_WhenContainerDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";

        // Act
        Func<Task> act = async () => await _service.GetBlobAsync(containerName, blobName);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }
    #endregion

    #region GetBlobsAsync
    [Fact]
    public async Task GetBlobsAsync_WhenBlobsExist_ShouldReturnBlobs()
    {
        // Arrange
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");
        var blobName1 = await _fixture.CreateBlobAsync(containerName, $"test-blob-{Guid.NewGuid():N}.txt", "content 1");
        var blobName2 = await _fixture.CreateBlobAsync(containerName, $"test-blob-{Guid.NewGuid():N}.txt", "content 2");

        // Act
        var results = await _service.GetBlobsAsync(containerName).ToListAsync();

        // Assert
        results.Should().NotBeEmpty().And.HaveCount(2)
            .And.Contain(b => b.Name == blobName1)
            .And.Contain(b => b.Name == blobName2);
    }

    [Fact]
    public async Task GetBlobsAsync_WhenNoBlobsExist_ShouldReturnEmptySequence()
    {
        // Arrange
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");

        // Act
        var results = await _service.GetBlobsAsync(containerName).ToListAsync();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBlobsAsync_WhenContainerDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";

        // Act
        Func<Task> act = async () => await _service.GetBlobsAsync(containerName).ToListAsync();

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }
    #endregion

    #region UpdateBlobAsync
    [Fact]
    public async Task UpdateBlobAsync_WithNewMetadata_ShouldUpdateBlob()
    {
        // Arrange
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");
        var blobName = await _fixture.CreateBlobAsync(containerName, $"test-blob-{Guid.NewGuid():N}.txt", "test content");

        var updatedProperties = new AzuriteBlobProperties
        {
            Metadata = new Dictionary<string, string>
            {
                ["updated"] = "true"
            },
            Tags = new Dictionary<string, string>
            {
                ["environment"] = "test"
            }
        };

        // Act
        var result = await _service.UpdateBlobAsync(containerName, blobName, updatedProperties);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(blobName);
        result.Metadata.Should().BeEquivalentTo(updatedProperties.Metadata);
        result.Tags.Should().BeEquivalentTo(updatedProperties.Tags);
    }

    [Fact]
    public async Task UpdateBlobAsync_WhenBlobDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        var properties = new AzuriteBlobProperties();

        // Act
        Func<Task> act = async () => await _service.UpdateBlobAsync(containerName, blobName, properties);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task UpdateBlobAsync_WhenContainerDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        var properties = new AzuriteBlobProperties();

        // Act
        Func<Task> act = async () => await _service.UpdateBlobAsync(containerName, blobName, properties);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }
    #endregion

    #region UploadCheckAsync
    [Fact]
    public async Task UploadCheckAsync_WhenContainerExistsAndBlobDoesNotExist_ShouldNotThrow()
    {
        // Arrange
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";

        // Act
        Func<Task> act = async () => await _service.UploadCheckAsync(containerName, blobName);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UploadCheckAsync_WhenBlobExists_ShouldThrowResourceExistsException()
    {
        // Arrange
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");
        var blobName = await _fixture.CreateBlobAsync(containerName, $"test-blob-{Guid.NewGuid():N}.txt", "test content");

        // Act
        Func<Task> act = async () => await _service.UploadCheckAsync(containerName, blobName);

        // Assert
        await act.Should().ThrowAsync<ResourceExistsException>();
    }

    [Fact]
    public async Task UploadCheckAsync_WhenContainerDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";

        // Act
        Func<Task> act = async () => await _service.UploadCheckAsync(containerName, blobName);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }
    #endregion

    #region UploadBlockAsync
    [Fact]
    public async Task UploadBlockAsync_WithValidBlock_ShouldUploadBlock()
    {
        // Arrange
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _service.UploadCheckAsync(containerName, blobName);
        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block-1"));
        var content = new MemoryStream(Encoding.UTF8.GetBytes("Block content"));

        // Act
        var result = await _service.UploadBlockAsync(containerName, blobName, blockId, content);

        // Assert
        result.Should().NotBeNull();
        result.BlockId.Should().Be(blockId);
        result.StatusCode.Should().Be(201);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UploadBlockAsync_WhenContainerDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block-1"));
        var content = new MemoryStream(Encoding.UTF8.GetBytes("Block content"));

        // Act
        Func<Task> act = async () => await _service.UploadBlockAsync(containerName, blobName, blockId, content);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }
    #endregion

    #region UploadCommitAsync
    [Fact]
    public async Task UploadCommitAsync_WithValidBlocks_ShouldCommitBlob()
    {
        // Arrange
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _service.UploadCheckAsync(containerName, blobName);

        var blockId1 = Convert.ToBase64String(Encoding.UTF8.GetBytes("block-1"));
        var content1 = new MemoryStream(Encoding.UTF8.GetBytes("First "));
        await _service.UploadBlockAsync(containerName, blobName, blockId1, content1);

        var blockId2 = Convert.ToBase64String(Encoding.UTF8.GetBytes("block-2"));
        var content2 = new MemoryStream(Encoding.UTF8.GetBytes("Second"));
        await _service.UploadBlockAsync(containerName, blobName, blockId2, content2);

        var properties = new AzuriteBlobProperties
        {
            ContentType = "text/plain",
            Metadata = new Dictionary<string, string> { ["test"] = "value" }
        };

        // Act
        var result = await _service.UploadCommitAsync(containerName, blobName, [blockId1, blockId2], properties);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(blobName);
        result.ContentType.Should().Be("text/plain");
        result.Metadata.Should().ContainKey("test");

        // Verify the content
        var downloadResult = await _service.DownloadBlobAsync(containerName, blobName);

        downloadResult.Content.Should().NotBeNull();
        using var reader = new StreamReader(downloadResult.Content!);
        var downloadedContent = await reader.ReadToEndAsync();
        downloadedContent.Should().Be("First Second");
    }

    [Fact]
    public async Task UploadCommitAsync_WithEmptyBlockList_ShouldCreateEmptyBlob()
    {
        // Arrange
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        await _service.UploadCheckAsync(containerName, blobName);

        var properties = new AzuriteBlobProperties
        {
            ContentType = "text/plain"
        };

        // Act
        var result = await _service.UploadCommitAsync(containerName, blobName, [], properties);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(blobName);
        result.ContentLength.Should().Be(0);
    }

    [Fact]
    public async Task UploadCommitAsync_WhenContainerDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var containerName = $"test-container-{Guid.NewGuid():N}";
        var blobName = $"test-blob-{Guid.NewGuid():N}.txt";
        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block-1"));
        var properties = new AzuriteBlobProperties
        {
            ContentType = "text/plain"
        };

        // Act
        Func<Task> act = async () => await _service.UploadCommitAsync(containerName, blobName, [blockId], properties);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task UploadCommitAsync_WithNullContentTypeEncodingAndLanguage_ShouldUseDefaults()
    {
        // Arrange
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");
        var blobName = $"test-blob-{Guid.NewGuid():N}.bin";
        await _service.UploadCheckAsync(containerName, blobName);

        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block-1"));
        var content = new MemoryStream(Encoding.UTF8.GetBytes("Binary content"));
        await _service.UploadBlockAsync(containerName, blobName, blockId, content);

        var properties = new AzuriteBlobProperties
        {
            ContentType = null,
            ContentEncoding = null,
            ContentLanguage = null,
            Metadata = new Dictionary<string, string> { ["test"] = "null-defaults" }
        };

        // Act
        var result = await _service.UploadCommitAsync(containerName, blobName, [blockId], properties);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(blobName);
        result.ContentType.Should().Be("application/octet-stream");
        result.ContentEncoding.Should().BeEmpty();
        result.ContentLanguage.Should().Be("en-US");
        result.Metadata.Should().ContainKey("test");
    }

    [Fact]
    public async Task UploadCommitAsync_WithSpecificContentTypeEncodingAndLanguage_ShouldUseProvidedValues()
    {
        // Arrange
        var containerName = await _fixture.CreateContainerAsync($"test-container-{Guid.NewGuid():N}");
        var blobName = $"test-blob-{Guid.NewGuid():N}.json";
        await _service.UploadCheckAsync(containerName, blobName);

        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block-1"));
        var content = new MemoryStream(Encoding.UTF8.GetBytes("{\"key\":\"value\"}"));
        await _service.UploadBlockAsync(containerName, blobName, blockId, content);

        var properties = new AzuriteBlobProperties
        {
            ContentType = "application/json",
            ContentEncoding = "gzip",
            ContentLanguage = "fr-FR",
            Metadata = new Dictionary<string, string> { ["test"] = "custom-values" }
        };

        // Act
        var result = await _service.UploadCommitAsync(containerName, blobName, [blockId], properties);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(blobName);
        result.ContentType.Should().Be("application/json");
        result.ContentEncoding.Should().Be("gzip");
        result.ContentLanguage.Should().Be("fr-FR");
        result.Metadata.Should().ContainKey("test");
    }
    #endregion
}
