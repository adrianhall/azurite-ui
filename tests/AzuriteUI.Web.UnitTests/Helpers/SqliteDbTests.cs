using AzuriteUI.Web.Services.Azurite.Models;
using AzuriteUI.Web.Services.CacheDb;
using AzuriteUI.Web.Services.CacheDb.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AzuriteUI.Web.UnitTests.Helpers;

public abstract class SqliteDbTests : IDisposable
{
    protected readonly SqliteConnection _connection;
    private bool _disposed;

    protected SqliteDbTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected CacheDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CacheDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new CacheDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    #region Azurite Item Helpers

    protected static AzuriteContainerItem CreateContainerItem(
        string name = "test-container",
        string etag = "\"0x8D9\"",
        DateTimeOffset? lastModified = null)
    {
        return new AzuriteContainerItem
        {
            Name = name,
            ETag = etag,
            LastModified = lastModified ?? DateTimeOffset.UtcNow,
            DefaultEncryptionScope = "$account-encryption-key",
            HasImmutabilityPolicy = false,
            HasImmutableStorageWithVersioning = false,
            HasLegalHold = false,
            Metadata = new Dictionary<string, string>(),
            PreventEncryptionScopeOverride = false,
            PublicAccess = AzuritePublicAccess.None,
            RemainingRetentionDays = null
        };
    }

    protected static AzuriteBlobItem CreateBlobItem(
        string name = "test-blob.txt",
        string etag = "\"0x8D9\"",
        long contentLength = 1024,
        DateTimeOffset? lastModified = null)
    {
        return new AzuriteBlobItem
        {
            Name = name,
            ETag = etag,
            LastModified = lastModified ?? DateTimeOffset.UtcNow,
            BlobType = AzuriteBlobType.Block,
            ContentEncoding = string.Empty,
            ContentLanguage = string.Empty,
            ContentLength = contentLength,
            ContentType = "text/plain",
            CreatedOn = DateTimeOffset.UtcNow.AddHours(-1),
            ExpiresOn = null,
            HasLegalHold = false,
            LastAccessedOn = null,
            Metadata = new Dictionary<string, string>(),
            RemainingRetentionDays = null,
            Tags = new Dictionary<string, string>()
        };
    }

    #endregion

    #region Database Model Helpers

    /// <summary>
    /// Creates and persists a ContainerModel to the database.
    /// </summary>
    protected static async Task<ContainerModel> CreateContainerModelAsync(
        CacheDbContext context,
        string name = "test-container",
        string? etag = null,
        string? cacheCopyId = null,
        DateTimeOffset? lastModified = null)
    {
        var container = new ContainerModel
        {
            Name = name,
            CachedCopyId = cacheCopyId ?? Guid.NewGuid().ToString("N"),
            ETag = etag ?? "container-etag",
            LastModified = lastModified ?? DateTimeOffset.UtcNow
        };
        context.Containers.Add(container);
        await context.SaveChangesAsync();
        return container;
    }

    /// <summary>
    /// Creates and persists an UploadModel to the database.
    /// </summary>
    protected static async Task<UploadModel> CreateUploadModelAsync(
        CacheDbContext context,
        string containerName,
        Guid? uploadId = null,
        string? blobName = null,
        long contentLength = 10240,
        string contentType = "text/plain",
        DateTimeOffset? createdAt = null,
        DateTimeOffset? lastActivityAt = null)
    {
        var upload = new UploadModel
        {
            UploadId = uploadId ?? Guid.NewGuid(),
            ContainerName = containerName,
            BlobName = blobName ?? "test-blob.txt",
            ContentLength = contentLength,
            ContentType = contentType,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
            LastActivityAt = lastActivityAt ?? DateTimeOffset.UtcNow
        };
        context.Uploads.Add(upload);
        await context.SaveChangesAsync();
        return upload;
    }

    /// <summary>
    /// Creates a BlobModel (does not persist to database).
    /// </summary>
    protected static BlobModel CreateBlobModel(
        string name,
        string containerName,
        string? etag = null,
        string? cacheCopyId = null,
        long contentLength = 1024,
        DateTimeOffset? lastModified = null,
        DateTimeOffset? createdOn = null)
    {
        return new BlobModel
        {
            Name = name,
            ContainerName = containerName,
            CachedCopyId = cacheCopyId ?? Guid.NewGuid().ToString("N"),
            ETag = etag ?? "test-etag",
            LastModified = lastModified ?? DateTimeOffset.UtcNow,
            ContentLength = contentLength,
            ContentType = "text/plain",
            CreatedOn = createdOn ?? DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates an UploadBlockModel (does not persist to database).
    /// </summary>
    protected static UploadBlockModel CreateUploadBlockModel(
        Guid uploadId,
        string blockId,
        long blockSize = 1024,
        string? contentMD5 = null,
        DateTimeOffset? uploadedAt = null)
    {
        return new UploadBlockModel
        {
            UploadId = uploadId,
            BlockId = blockId,
            BlockSize = blockSize,
            ContentMD5 = contentMD5,
            UploadedAt = uploadedAt ?? DateTimeOffset.UtcNow
        };
    }

    #endregion

    #region Dispose

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _connection?.Dispose();
            }
            _disposed = true;
        }
    }

    #endregion
}