using AzuriteUI.Web.Services.CacheSync.Models;

namespace AzuriteUI.Web.Services.CacheSync;

/// <summary>
/// The interface for the queue manager.  Use this to introduce work to be processed.
/// </summary>
public interface IQueueManager
{
    /// <summary>
    /// An event handler for queue manager events.
    /// </summary>
    event EventHandler<QueueManagerEventArgs> QueueChanged;

    /// <summary>
    /// The number of events within the queue.
    /// </summary>
    int QueueSize { get; }

    /// <summary>
    /// The list of items within the queued work.
    /// </summary>
    List<QueuedWork> QueuedItems { get; }

    /// <summary>
    /// The current item being run now.
    /// </summary>
    QueuedWork? CurrentItem { get; }

    /// <summary>
    /// Clears the queue
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that resolves when the work is completed.</returns>
    Task ClearQueueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests a new bit of work.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The unique ID for the queued work.</returns>
    Task<QueuedWork> EnqueueWorkAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the queue processing.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that resolves when the queue is started.</returns>
    Task StartQueueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the queue processing.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that resolves when the queue is stopped.</returns>
    Task StopQueueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the queue processing.
    /// </summary>
    /// <param name="finishProcessing">If true, allows the currently executing work to complete before stopping. If false, cancels the work immediately.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that resolves when the queue is stopped.</returns>
    Task StopQueueAsync(bool finishProcessing, CancellationToken cancellationToken = default);
}
