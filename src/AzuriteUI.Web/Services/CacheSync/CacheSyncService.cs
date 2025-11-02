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

                    var storedContainer = await context.UpsertContainerAsync(container, cacheCopyId, cancellationToken);
                    long totalSize = 0L;
                    int blobCount = 0;

                    var observable = service.GetBlobsAsync(container.Name, cancellationToken).ToObservable();
                    await observable
                        .Buffer(MaxBatchTime, MaxBatchSize)
                        .Select(batch => Observable.FromAsync(async () =>
                        {
                            UpdateSizeAndCount(batch, ref totalSize, ref blobCount);
                            await context.UpsertBlobsAsync(batch, storedContainer.Name, cacheCopyId, cancellationToken); 
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