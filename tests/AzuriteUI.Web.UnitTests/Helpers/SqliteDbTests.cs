using AzuriteUI.Web.Services.Azurite.Models;
using AzuriteUI.Web.Services.CacheDb;
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