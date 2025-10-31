using AzuriteUI.Web.Services.CacheSync.Models;
using System.Threading.Channels;

namespace AzuriteUI.Web.Services.CacheSync;

/// <summary>
/// Implementation of a queue manager that ensures at most 1 item in the queue + 1 being executed.
/// </summary>
/// <param name="worker">The queue worker that performs the actual work.</param>
/// <param name="logger">The logger for diagnostic messages.</param>
public class QueueManager(IQueueWorker worker, ILogger<QueueManager> logger) : IQueueManager, IDisposable
{
    /// <summary>
    /// The worker that performs the actual work for each queued item.
    /// </summary>
    private readonly IQueueWorker _worker = worker ?? throw new ArgumentNullException(nameof(worker));

    /// <summary>
    /// The logger for diagnostic and informational messages.
    /// </summary>
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// The channel used for thread-safe queue management of work items.
    /// </summary>
    private readonly Channel<QueuedWork> _channel = Channel.CreateUnbounded<QueuedWork>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    /// <summary>
    /// Semaphore to coordinate access to the enqueued item state and prevent race conditions.
    /// </summary>
    private readonly SemaphoreSlim _queueLock = new(1, 1);

    /// <summary>
    /// The work item waiting in the queue to be executed (null if queue is empty). Limited to at most 1 item.
    /// </summary>
    private QueuedWork? _enqueuedItem;

    /// <summary>
    /// The background task that processes items from the queue.
    /// </summary>
    private Task? _processingTask;

    /// <summary>
    /// Cancellation token source used to stop the queue processing task.
    /// </summary>
    private CancellationTokenSource? _processingCts;

    /// <summary>
    /// Cancellation token source used to cancel work item execution when immediate stop is requested.
    /// </summary>
    private CancellationTokenSource? _workCts;

    /// <summary>
    /// Flag indicating whether the queue is currently running and processing work items.
    /// </summary>
    private bool _isRunning;

    /// <summary>
    /// Flag indicating whether this instance has been disposed.
    /// </summary>
    private bool _disposed;

    /// <inheritdoc/>
    public event EventHandler<QueueManagerEventArgs>? QueueChanged;

    /// <inheritdoc/>
    public int QueueSize => _enqueuedItem is not null ? 1 : 0;

    /// <inheritdoc/>
    public List<QueuedWork> QueuedItems
    {
        get => _enqueuedItem is not null ? [_enqueuedItem] : [];
    }

    /// <inheritdoc/>
    /// <summary>
    /// The work item currently being executed (null if no work is being processed).
    /// </summary>
    public QueuedWork? CurrentItem { get; private set; }

    /// <inheritdoc/>
    public async Task<QueuedWork> EnqueueWorkAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _queueLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // If we already have an item enqueued (not being executed), return it
            if (_enqueuedItem != null)
            {
                _logger.LogDebug("Work already enqueued with ID {WorkId}. Returning existing work.", _enqueuedItem.Id);
                return _enqueuedItem;
            }

            // Create new work item
            var work = new QueuedWork();
            _enqueuedItem = work;

            // Write to channel
            await _channel.Writer.WriteAsync(work, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Enqueued work with ID {WorkId}", work.Id);
            SendQueueManagerEvent(QueueEvent.WorkEnqueued, work);

            return work;
        }
        finally
        {
            _queueLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task StartQueueAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _queueLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_isRunning)
            {
                _logger.LogWarning("Queue is already running");
                return;
            }

            _isRunning = true;
            _processingCts = new CancellationTokenSource();
            _workCts = new CancellationTokenSource();
            _processingTask = ProcessQueueAsync(_processingCts.Token);

            _logger.LogInformation("Queue started");
            SendQueueManagerEvent(QueueEvent.QueueStarted);
        }
        finally
        {
            _queueLock.Release();
        }
    }

    /// <inheritdoc/>
    public Task StopQueueAsync(CancellationToken cancellationToken = default)
    {
        return StopQueueAsync(finishProcessing: true, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task StopQueueAsync(bool finishProcessing, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _queueLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_isRunning)
            {
                _logger.LogWarning("Queue is not running");
                return;
            }

            _isRunning = false;

            // If we should NOT finish processing, cancel the work immediately
            if (!finishProcessing)
            {
                _logger.LogInformation("Cancelling currently executing work");
                CancelTokenSource(ref _workCts);
            }

            // Always cancel the processing loop
            CancelTokenSource(ref _processingCts);

            if (_processingTask is not null)
            {
                await SwallowCancellationsAsync(_processingTask).ConfigureAwait(false);
            }

            DisposeTokenSource(ref _processingCts);
            DisposeTokenSource(ref _workCts);
            _processingTask = null;

            _logger.LogInformation("Queue stopped (finishProcessing: {FinishProcessing})", finishProcessing);
            SendQueueManagerEvent(QueueEvent.QueueStopped);
        }
        finally
        {
            _queueLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task ClearQueueAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _queueLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Clear the enqueued item reference
            _enqueuedItem = null;

            // Read all pending items from the channel to clear it
            while (_channel.Reader.TryRead(out _))
            {
                // Discard items
            }

            _logger.LogInformation("Queue cleared");
        }
        finally
        {
            _queueLock.Release();
        }
    }

    /// <summary>
    /// The main processing loop that reads from the channel and executes work.
    /// </summary>
    internal async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Started processing queue");
        await LogExceptionWithCancellation(async (ct) =>
        {
            // We use a separate cancellation token for reading from the channel
            // so that we can stop accepting new work but finish the current work
            await foreach (var work in _channel.Reader.ReadAllAsync(ct))
            {
                // Process work item WITHOUT passing the cancellation token
                // This ensures the work completes even when stop is requested
                await ProcessWorkItemAsync(work).ConfigureAwait(false);

                // After completing work, check if we should stop
                if (ct.IsCancellationRequested)
                {
                    _logger.LogDebug("Queue processing stopping after completing current work");
                    break;
                }
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Processes a single work item.
    /// </summary>
    /// <remarks>
    /// Work items can be cancelled if StopQueueAsync is called with finishProcessing: false.
    /// Otherwise, they will complete normally even when the queue is stopping.
    /// </remarks>
    internal async Task ProcessWorkItemAsync(QueuedWork work)
    {
        CurrentItem = work;

        // Clear enqueued reference since we're now processing this item
        await _queueLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            _enqueuedItem = null;
        }
        finally
        {
            _queueLock.Release();
        }

        _logger.LogInformation("Started processing work with ID {WorkId}", work.Id);
        SendQueueManagerEvent(QueueEvent.WorkStarted, work);

        try
        {
            // Execute work with the work cancellation token
            // This allows immediate cancellation when StopQueueAsync(false) is called
            var workToken = _workCts?.Token ?? CancellationToken.None;
            await _worker.ExecuteAsync(CurrentItem, workToken).ConfigureAwait(false);

            _logger.LogInformation("Finished processing work with ID {WorkId}", work.Id);
            SendQueueManagerEvent(QueueEvent.WorkFinished, work);
        }
        catch (OperationCanceledException) when (_workCts?.IsCancellationRequested == true)
        {
            _logger.LogWarning("Work with ID {WorkId} was cancelled", work.Id);
            SendQueueManagerEvent(QueueEvent.WorkErrored, work, new OperationCanceledException("Work was cancelled due to immediate queue stop"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing work with ID {WorkId}", work.Id);
            SendQueueManagerEvent(QueueEvent.WorkErrored, work, ex);
        }
        finally
        {
            CurrentItem = null;
        }
    }

    /// <summary>
    /// Raises the QueueChanged event with the specified event type and optional data.
    /// </summary>
    internal void SendQueueManagerEvent(QueueEvent eventType, QueuedWork? work = null, Exception? exception = null)
    {
        LogExceptions("Error in QueueChanged event handler", () =>
        {
            var args = new QueueManagerEventArgs(eventType, work, exception);
            QueueChanged?.Invoke(this, args);
        });
    }

    /// <summary>
    /// A helper method that wraps some code in an exception logger and swallows the exception.
    /// </summary>
    /// <param name="errorMessage">The error message to log.</param>
    /// <param name="act">The action to perform.</param>
    internal void LogExceptions(string errorMessage, Action act)
    {
        try
        {
            act.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{errorMessage}", errorMessage);
        }
    }

    /// <summary>
    /// A helper methods that wraps an async task and swallows OperationCanceledExceptions
    /// </summary>
    /// <param name="task">The task to await</param>
    internal static async Task SwallowCancellationsAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected exception
        }
    }

    /// <summary>
    /// A helper method that wraps an async task and logs the exceptions.
    /// </summary>
    /// <param name="task">The task to execute.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that resolves when complete.</returns>
    internal async Task LogExceptionWithCancellation(Func<CancellationToken, Task> task, CancellationToken cancellationToken = default)
    {
        try
        {
            await task.Invoke(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Queue processing cancelled while waiting for work");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in queue processing loop");
        }
    }

    /// <summary>
    /// Cancels the given CancellationTokenSource if it isn't null.
    /// </summary>
    /// <param name="cts">The <see cref="CancellationTokenSource"/> to cancel.</param>
    internal static void CancelTokenSource(ref CancellationTokenSource? cts)
    {
        cts?.Cancel();
    }

    /// <summary>
    /// Disposes the given CancellationTokenSource if it isn't null.
    /// </summary>
    /// <param name="cts">The <see cref="CancellationTokenSource"/> to dispose.</param>
    internal static void DisposeTokenSource(ref CancellationTokenSource? cts)
    {
        cts?.Dispose();
        cts = null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CancelTokenSource(ref _processingCts);
        DisposeTokenSource(ref _processingCts);
        CancelTokenSource(ref _workCts);
        DisposeTokenSource(ref _workCts);
        _queueLock.Dispose();
        _channel.Writer.Complete();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
