namespace AzuriteUI.Web.Services.CacheSync;

/// <summary>
/// A service definition for a service that synchronizes the cache with Azurite storage.
/// This is the one-shot version of the process.
/// </summary>
public interface ICacheSyncService
{
    /// <summary>
    /// Synchronizes the cache with the Azurite storage.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SynchronizeCacheAsync(CancellationToken cancellationToken = default);
}