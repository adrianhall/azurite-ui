using AzuriteUI.Web.Services.CacheSync.Models;

namespace AzuriteUI.Web.Services.CacheSync;

/// <summary>
/// An implementation of the <see cref="IQueueWorker"/> that implements the
/// cache sync procecessing logic.
/// </summary>
public class QueueWorker(IServiceProvider services, ILogger<QueueWorker> logger) : IQueueWorker
{
    /// <summary>
    /// A semaphore to ensure that only one instance of the worker is running at any given time.
    /// </summary>
    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    /// <inheritdoc />
    public async Task ExecuteAsync(QueuedWork work, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Executing queued work with ID {WorkId}", work.Id);
        
        var lockAcquired = false;
        try
        {
            await SyncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            lockAcquired = true;
            logger.LogDebug("Starting cache synchronization queue worker.");
            using var scope = services.CreateScope();
            var cacheSyncService = scope.ServiceProvider.GetRequiredService<ICacheSyncService>();
            await cacheSyncService.SynchronizeCacheAsync(cancellationToken).ConfigureAwait(false);
            logger.LogDebug("Cache synchronization queue worker completed.");
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Cache synchronization queue worker was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during cache synchronization.");
            throw;
        }
        finally
        {
            if (lockAcquired)
            {
                SyncLock.Release();
            }
        }
    }
}
