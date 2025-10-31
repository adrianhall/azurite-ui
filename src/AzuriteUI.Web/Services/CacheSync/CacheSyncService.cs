using AzuriteUI.Web.Extensions;
using AzuriteUI.Web.Services.Azurite;
using AzuriteUI.Web.Services.Azurite.Models;
using AzuriteUI.Web.Services.CacheDb;
using AzuriteUI.Web.Services.CacheDb.Models;
using Microsoft.EntityFrameworkCore;
using System.Reactive.Linq;

namespace AzuriteUI.Web.Services.CacheSync;

/// <summary>
/// A service that synchronizes the cache with Azurite storage.
/// </summary>
/// <param name="context">The database context for the cache database.</param>
/// <param name="service">The Azurite service for interacting with Azurite storage.</param>
/// <param name="logger">The logger for logging events and errors.</param>
public class CacheSyncService(CacheDbContext context, IAzuriteService service, ILogger<CacheSyncService> logger) : ICacheSyncService
{
    /// <summary>
    /// Maximum number of blobs to batch together for database insertion.
    /// </summary>
    private const int MaxBatchSize = 100;

    /// <summary>
    /// Maximum time to wait before flushing a batch, in milliseconds.
    /// </summary>
    private static readonly TimeSpan MaxBatchTime = TimeSpan.FromMilliseconds(500);

    /// <inheritdoc />
    public async Task SynchronizeCacheAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting cache synchronization");
        var cacheCopyId = Guid.NewGuid().ToString("N");

        try
        {
            int containerCount = 0;
            int totalBlobCount = 0;

            await foreach (var container in service.GetContainersAsync(cancellationToken))
            {
                try
                {
                    logger.LogDebug("Processing container: {ContainerName}", container.Name);

                    var storedContainer = await StoreContainerInContextAsync(container, cacheCopyId, cancellationToken);
                    long totalSize = 0L;
                    int blobCount = 0;

                    var observable = service.GetBlobsAsync(container.Name, cancellationToken).ToObservable();
                    await observable
                        .Buffer(MaxBatchTime, MaxBatchSize)
                        .Select(batch => Observable.FromAsync(async () =>
                        {
                            UpdateSizeAndCount(batch, ref totalSize, ref blobCount);
                            await StoreBlobsInContextAsync(storedContainer.Name, batch, cacheCopyId, cancellationToken);
                        }))
                        .Concat()
                        .LastOrDefaultAsync();

                    // Update the container with the calculated blob count and total size
                    storedContainer.BlobCount = blobCount;
                    storedContainer.TotalSize = totalSize;
                    context.Containers.Update(storedContainer);
                    await context.SaveChangesAsync(cancellationToken);

                    containerCount++;
                    totalBlobCount += blobCount;

                    logger.LogInformation(
                        "Completed container: {ContainerName} with {BlobCount} blobs ({TotalSize} bytes)",
                        storedContainer.Name, blobCount, totalSize);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing container: {ContainerName}", container.Name);
                    throw;
                }
            }

            // Clear old cache entries that don't match this sync operation
            await CleanupOldCacheEntriesAsync(cacheCopyId, cancellationToken);

            logger.LogInformation(
                "Cache synchronization completed successfully. Processed {ContainerCount} containers with {TotalBlobCount} total blobs",
                containerCount, totalBlobCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cache synchronization failed");
            throw;
        }
    }

    /// <summary>
    /// Stores a container in the database context.
    /// </summary>
    /// <param name="containerTransfer">The container transfer model from Azurite.</param>
    /// <param name="cacheCopyId">The cache copy identifier for this sync operation.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The stored container model.</returns>
    internal async Task<ContainerModel> StoreContainerInContextAsync(
        AzuriteContainerItem containerTransfer,
        string cacheCopyId,
        CancellationToken cancellationToken)
    {
        var existing = await context.Containers.FirstOrDefaultAsync(c => c.Name == containerTransfer.Name, cancellationToken);

        if (existing is not null)
        {
            // Update existing container
            existing.CachedCopyId = cacheCopyId;
            existing.ETag = containerTransfer.ETag;
            existing.HasLegalHold = containerTransfer.HasLegalHold;
            existing.LastModified = containerTransfer.LastModified;
            existing.Metadata = containerTransfer.Metadata;
            existing.RemainingRetentionDays = containerTransfer.RemainingRetentionDays;
            existing.DefaultEncryptionScope = containerTransfer.DefaultEncryptionScope;
            existing.HasImmutabilityPolicy = containerTransfer.HasImmutabilityPolicy;
            existing.HasImmutableStorageWithVersioning = containerTransfer.HasImmutableStorageWithVersioning;
            existing.PublicAccess = containerTransfer.PublicAccess;
            existing.PreventEncryptionScopeOverride = containerTransfer.PreventEncryptionScopeOverride;

            context.Containers.Update(existing);
        }
        else
        {
            // Create new container
            existing = new ContainerModel
            {
                Name = containerTransfer.Name,
                CachedCopyId = cacheCopyId,
                ETag = containerTransfer.ETag,
                HasLegalHold = containerTransfer.HasLegalHold,
                LastModified = containerTransfer.LastModified,
                Metadata = containerTransfer.Metadata,
                RemainingRetentionDays = containerTransfer.RemainingRetentionDays,
                DefaultEncryptionScope = containerTransfer.DefaultEncryptionScope,
                HasImmutabilityPolicy = containerTransfer.HasImmutabilityPolicy,
                HasImmutableStorageWithVersioning = containerTransfer.HasImmutableStorageWithVersioning,
                PublicAccess = containerTransfer.PublicAccess,
                PreventEncryptionScopeOverride = containerTransfer.PreventEncryptionScopeOverride
            };

            await context.Containers.AddAsync(existing, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogDebug("Stored container: {ContainerName}", containerTransfer.Name);

        return existing;
    }

    /// <summary>
    /// Stores a batch of blobs in the database context.
    /// </summary>
    /// <param name="containerName">The name of the container holding these blobs.</param>
    /// <param name="blobs">The batch of blob transfer models from Azurite.</param>
    /// <param name="cacheCopyId">The cache copy identifier for this sync operation.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    internal async Task StoreBlobsInContextAsync(
        string containerName,
        IList<AzuriteBlobItem> blobs,
        string cacheCopyId,
        CancellationToken cancellationToken)
    {
        if (blobs.Count == 0)
        {
            return;
        }

        var blobNames = blobs.Select(b => b.Name).ToList();
        var existingBlobs = await context.Blobs
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

                context.Blobs.Update(existing);
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

                await context.Blobs.AddAsync(newBlob, cancellationToken);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogDebug("Stored {BlobCount} blobs in container: {ContainerName}", blobs.Count, containerName);
    }

    /// <summary>
    /// Updates the running totals of size and count for a batch of blobs using atomic operations.
    /// </summary>
    /// <param name="blobs">The batch of blobs to count.</param>
    /// <param name="totalSize">The running total size in bytes.</param>
    /// <param name="blobCount">The running blob count.</param>
    internal static void UpdateSizeAndCount(IList<AzuriteBlobItem> blobs, ref long totalSize, ref int blobCount)
    {
        long localSize = 0;
        int localCount = 0;

        foreach (var blob in blobs)
        {
            localSize += blob.ContentLength;
            localCount++;
        }

        Interlocked.Add(ref totalSize, localSize);
        Interlocked.Add(ref blobCount, localCount);
    }

    /// <summary>
    /// Removes old cache entries that don't belong to the current sync operation.
    /// </summary>
    /// <param name="cacheCopyId">The current cache copy identifier.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    internal async Task CleanupOldCacheEntriesAsync(string cacheCopyId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Cleaning up old cache entries");

        // Remove any containers/blobs not part of the current cache copy
        var containersToRemove = await context.Containers
            .Where(c => c.CachedCopyId != cacheCopyId)
            .ToListAsync(cancellationToken);

        var blobsToRemove = await context.Blobs
            .Where(b => b.CachedCopyId != cacheCopyId)
            .ToListAsync(cancellationToken);

        context.Containers.RemoveRange(containersToRemove);
        context.Blobs.RemoveRange(blobsToRemove);

        // Save the changes to the database
        await context.SaveChangesAsync(cancellationToken);

        // Also clean up stale uploads while we're doing cleanup
        await CleanupStaleUploadsAsync(cancellationToken);
    }

    /// <summary>
    /// Cleans up stale upload sessions that have had no activity in the specified timeout period.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    internal async Task CleanupStaleUploadsAsync(CancellationToken cancellationToken = default)
    {
        var uploadTimeout = TimeSpan.FromMinutes(15);
        var cutoffTime = DateTimeOffset.UtcNow - uploadTimeout;

        logger.LogDebug("Cleaning up stale uploads");

        // Find uploads with no activity in the last 15 minutes
        var staleUploads = await context.Uploads
            .Where(u => u.LastActivityAt < cutoffTime)
            .ToListAsync(cancellationToken);

        if (staleUploads.Count > 0)
        {
            logger.LogInformation("Found {Count} stale uploads to clean up", staleUploads.Count);

            // Delete stale uploads and their blocks (cascade delete handles blocks)
            context.Uploads.RemoveRange(staleUploads);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Cleaned up {Count} stale uploads", staleUploads.Count);
        }
    }
}