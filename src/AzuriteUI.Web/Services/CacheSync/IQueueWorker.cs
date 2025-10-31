using AzuriteUI.Web.Services.CacheSync.Models;

namespace AzuriteUI.Web.Services.CacheSync;

/// <summary>
/// An interface representing a unit of work.
/// </summary>
public interface IQueueWorker
{
    /// <summary>
    /// Executes a single unit of work.
    /// </summary>
    /// <param name="work">The work to be executed.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that resolves when the work is complete.</returns>
    Task ExecuteAsync(QueuedWork work, CancellationToken cancellationToken = default);
}