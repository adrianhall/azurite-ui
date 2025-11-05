using AzuriteUI.Web.Services.Azurite.Models;
using AzuriteUI.Web.Services.CacheDb.Models;
using Microsoft.EntityFrameworkCore;

namespace AzuriteUI.Web.Services.CacheDb;

/// <summary>
/// The database context for the cache Sqlite database.
/// </summary>
/// <param name="options">the database context options</param>
public class CacheDbContext(DbContextOptions<CacheDbContext> options) : DbContext(options)
{
    /// <summary>
    /// The blobs registered within Azurite.
    /// </summary>
    public DbSet<BlobModel> Blobs => Set<BlobModel>();

    /// <summary>
    /// The containers registered within Azurite.
    /// </summary>
    public DbSet<ContainerModel> Containers => Set<ContainerModel>();

    /// <summary>
    /// The schema versions applied to the database.
    /// </summary>
    public DbSet<SchemaVersion> SchemaVersions => Set<SchemaVersion>();

    /// <summary>
    /// The in-progress uploads.
    /// </summary>
    public DbSet<UploadModel> Uploads => Set<UploadModel>();

    /// <summary>
    /// The uploaded blocks for in-progress uploads.
    /// </summary>
    public DbSet<UploadBlockModel> UploadBlocks => Set<UploadBlockModel>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CacheDbContext).Assembly);
    }

    /// <summary>
    /// Removes the specified blob from the database.
    /// </summary>
    /// <param name="containerName">The name of the container the blob belongs to.</param>
    /// <param name="blobName">The name of the blob to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that resolves when the blob is removed.</returns>
    public async Task RemoveBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        var existing = await Blobs.FirstOrDefaultAsync(b => b.Name == blobName && b.ContainerName == containerName, cancellationToken);
        if (existing is not null)
        {
            Blobs.Remove(existing);
            await SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Removes the specified container from the database.
    /// </summary>
    /// <remarks>
    /// The data is assured removed - if the data doesn't exist, it is silently ignored.
    /// </remarks>
    /// <param name="containerName">The name of the container.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that resolves when the container is removed.</returns>
    public async Task RemoveContainerAsync(string containerName, CancellationToken cancellationToken = default)
    {
        var existing = await Containers.FirstOrDefaultAsync(c => c.Name == containerName, cancellationToken);
        if (existing is not null)
        {
            Containers.Remove(existing);
            await SaveChangesAsync(cancellationToken);
        }
    }
   
    /// <summary>
    /// Inserts or updates a blob in the database.
    /// </summary>
    /// <param name="blob">The blob to insert or update.</param>
    /// <param name="containerName">The name of the container the blob belongs to.</param>
    /// <param name="cacheCopyId">The cache copy ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <return>The upserted blob.</return>
    public async Task<BlobModel> UpsertBlobAsync(AzuriteBlobItem blob, string containerName, string cacheCopyId, CancellationToken cancellationToken = default)
    {
        var existing = await Blobs.FirstOrDefaultAsync(b => b.Name == blob.Name && b.ContainerName == containerName, cancellationToken);

        if (existing is not null)
        {
            // Update existing blob
            existing.CachedCopyId = cacheCopyId;
            existing.ETag = blob.ETag;
            existing.HasLegalHold = blob.HasLegalHold;
            existing.LastModified = blob.LastModified;
            existing.Metadata = blob.Metadata;
            existing.RemainingRetentionDays = blob.RemainingRetentionDays;
            existing.BlobType = blob.BlobType;
            existing.ContentEncoding = blob.ContentEncoding;
            existing.ContentLanguage = blob.ContentLanguage;
            existing.ContentLength = blob.ContentLength;
            existing.ContentType = blob.ContentType;
            existing.CreatedOn = blob.CreatedOn ?? DateTimeOffset.MinValue;
            existing.ExpiresOn = blob.ExpiresOn;
            existing.LastAccessedOn = blob.LastAccessedOn;
            existing.Tags = blob.Tags;

            Blobs.Update(existing);
        }
        else
        {
            // Create new blob
            existing = new BlobModel
            {
                Name = blob.Name,
                ContainerName = containerName,
                CachedCopyId = cacheCopyId,
                ETag = blob.ETag,
                HasLegalHold = blob.HasLegalHold,
                LastModified = blob.LastModified,
                Metadata = blob.Metadata,
                RemainingRetentionDays = blob.RemainingRetentionDays,
                BlobType = blob.BlobType,
                ContentEncoding = blob.ContentEncoding,
                ContentLanguage = blob.ContentLanguage,
                ContentLength = blob.ContentLength,
                ContentType = blob.ContentType,
                CreatedOn = blob.CreatedOn ?? DateTimeOffset.MinValue,
                ExpiresOn = blob.ExpiresOn,
                LastAccessedOn = blob.LastAccessedOn,
                Tags = blob.Tags
            };

            await Blobs.AddAsync(existing, cancellationToken);
        }

        await SaveChangesAsync(cancellationToken);

        // Update the container model based on the current state of blobs
        var blobCount = await Blobs.CountAsync(b => b.ContainerName == containerName, cancellationToken);
        var totalSize = await Blobs.Where(b => b.ContainerName == containerName).SumAsync(b => (long?)b.ContentLength, cancellationToken) ?? 0L;
        
        // There is always a container, since there is a foreign key constraint between containers and blobs.
        var container = await Containers.SingleAsync(c => c.Name == containerName, cancellationToken);
        container.BlobCount = blobCount;
        container.TotalSize = totalSize;
        Containers.Update(container);
        await SaveChangesAsync(cancellationToken);

        return existing;
    }

    /// <summary>
    /// Inserts or updates a blob in the database.
    /// </summary>
    /// <param name="blob">The blob to insert or update.</param>
    /// <param name="containerName">The name of the container the blob belongs to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <return>The upserted blob.</return>
    public Task<BlobModel> UpsertBlobAsync(AzuriteBlobItem blob, string containerName, CancellationToken cancellationToken = default)
        => UpsertBlobAsync(blob, containerName, Guid.NewGuid().ToString(), cancellationToken);

    /// <summary>
    /// Stores a batch of blobs in the database context.
    /// </summary>
    /// <param name="containerName">The name of the container holding these blobs.</param>
    /// <param name="blobs">The batch of blob transfer models from Azurite.</param>
    /// <param name="cacheCopyId">The cache copy identifier for this sync operation.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    internal async Task UpsertBlobsAsync(IList<AzuriteBlobItem> blobs, string containerName, string cacheCopyId, CancellationToken cancellationToken = default)
    {
        if (blobs.Count == 0)
        {
            return;
        }

        var blobNames = blobs.Select(b => b.Name).ToList();
        var existingBlobs = await Blobs
            .Where(b => b.ContainerName == containerName && blobNames.Contains(b.Name))
            .ToDictionaryAsync(b => b.Name, cancellationToken);

        foreach (var blob in blobs)
        {
            if (existingBlobs.TryGetValue(blob.Name, out var existing))
            {
                // Update existing blob
                existing.CachedCopyId = cacheCopyId;
                existing.ETag = blob.ETag;
                existing.HasLegalHold = blob.HasLegalHold;
                existing.LastModified = blob.LastModified;
                existing.Metadata = blob.Metadata;
                existing.RemainingRetentionDays = blob.RemainingRetentionDays;
                existing.BlobType = blob.BlobType;
                existing.ContentEncoding = blob.ContentEncoding;
                existing.ContentLanguage = blob.ContentLanguage;
                existing.ContentLength = blob.ContentLength;
                existing.ContentType = blob.ContentType;
                existing.CreatedOn = blob.CreatedOn ?? DateTimeOffset.MinValue;
                existing.ExpiresOn = blob.ExpiresOn;
                existing.LastAccessedOn = blob.LastAccessedOn;
                existing.Tags = blob.Tags;

                Blobs.Update(existing);
            }
            else
            {
                // Create new blob
                var newBlob = new BlobModel
                {
                    Name = blob.Name,
                    ContainerName = containerName,
                    CachedCopyId = cacheCopyId,
                    ETag = blob.ETag,
                    HasLegalHold = blob.HasLegalHold,
                    LastModified = blob.LastModified,
                    Metadata = blob.Metadata,
                    RemainingRetentionDays = blob.RemainingRetentionDays,
                    BlobType = blob.BlobType,
                    ContentEncoding = blob.ContentEncoding,
                    ContentLanguage = blob.ContentLanguage,
                    ContentLength = blob.ContentLength,
                    ContentType = blob.ContentType,
                    CreatedOn = blob.CreatedOn ?? DateTimeOffset.MinValue,
                    ExpiresOn = blob.ExpiresOn,
                    LastAccessedOn = blob.LastAccessedOn,
                    Tags = blob.Tags
                };

                await Blobs.AddAsync(newBlob, cancellationToken);
            }
        }

        await SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts or updates a container in the database.  Throws if a conflict occurs.
    /// </summary>
    /// <param name="container">The container to upsert.</param>
    /// <param name="cacheCopyId">The cache copy ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The upserted container.</returns>
    public async Task<ContainerModel> UpsertContainerAsync(AzuriteContainerItem container, string cacheCopyId, CancellationToken cancellationToken = default)
    {
        var existing = await Containers.FirstOrDefaultAsync(c => c.Name == container.Name, cancellationToken);

        if (existing is not null)
        {
            // Update existing container
            existing.CachedCopyId = cacheCopyId;
            existing.ETag = container.ETag;
            existing.HasLegalHold = container.HasLegalHold;
            existing.LastModified = container.LastModified;
            existing.Metadata = container.Metadata;
            existing.RemainingRetentionDays = container.RemainingRetentionDays;
            existing.DefaultEncryptionScope = container.DefaultEncryptionScope;
            existing.HasImmutabilityPolicy = container.HasImmutabilityPolicy;
            existing.HasImmutableStorageWithVersioning = container.HasImmutableStorageWithVersioning;
            existing.PublicAccess = container.PublicAccess;
            existing.PreventEncryptionScopeOverride = container.PreventEncryptionScopeOverride;

            Containers.Update(existing);
        }
        else
        {
            // Create new container
            existing = new ContainerModel
            {
                Name = container.Name,
                CachedCopyId = cacheCopyId,
                ETag = container.ETag,
                HasLegalHold = container.HasLegalHold,
                LastModified = container.LastModified,
                Metadata = container.Metadata,
                RemainingRetentionDays = container.RemainingRetentionDays,
                DefaultEncryptionScope = container.DefaultEncryptionScope,
                HasImmutabilityPolicy = container.HasImmutabilityPolicy,
                HasImmutableStorageWithVersioning = container.HasImmutableStorageWithVersioning,
                PublicAccess = container.PublicAccess,
                PreventEncryptionScopeOverride = container.PreventEncryptionScopeOverride
            };

            await Containers.AddAsync(existing, cancellationToken);
        }

        await SaveChangesAsync(cancellationToken);
        return existing;
    }

    /// <summary>
    /// Inserts or updates a container in the database.  Throws if a conflict occurs.
    /// </summary>
    /// <param name="container">The container to upsert.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The upserted container.</returns>
    public Task<ContainerModel> UpsertContainerAsync(AzuriteContainerItem container, CancellationToken cancellationToken = default)
        => UpsertContainerAsync(container, Guid.NewGuid().ToString(), cancellationToken);

    /// <summary>
    /// Inserts or updates an upload block in the database.
    /// </summary>
    /// <param name="blockModel">The model to be inserted or updated.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The updated upload block model</returns>
    public async Task<UploadBlockModel> UpsertUploadBlockAsync(UploadBlockModel blockModel, CancellationToken cancellationToken = default)
    {
        var existingBlock = await UploadBlocks.FirstOrDefaultAsync(b => b.BlockId == blockModel.BlockId && b.UploadId == blockModel.UploadId, cancellationToken);
        if (existingBlock is not null)
        {
            existingBlock.BlockSize = blockModel.BlockSize;
            existingBlock.ContentMD5 = blockModel.ContentMD5;
            existingBlock.UploadedAt = blockModel.UploadedAt;
            UploadBlocks.Update(existingBlock);
        }
        else
        {
            existingBlock = blockModel;
            UploadBlocks.Add(existingBlock);
        }

        // Find the associated upload and update its LastModified time
        var upload = await Uploads.FirstOrDefaultAsync(u => u.UploadId == blockModel.UploadId, cancellationToken);
        if (upload is not null)
        {
            upload.LastActivityAt = DateTimeOffset.UtcNow;
            Uploads.Update(upload);
        }

        await SaveChangesAsync(cancellationToken);
        return existingBlock;
    }
}