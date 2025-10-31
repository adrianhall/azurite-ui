using AzuriteUI.Web.Extensions;

namespace AzuriteUI.Web.Services.CacheSync;

/// <summary>
/// A hosted service that schedules cache synchronization work at regular intervals.
/// </summary>
/// <param name="queueManager">The queue manager to enqueue work.</param>
/// <param name="configuration">The application configuration.</param>
/// <param name="timeProvider">The time provider for creating timers.</param>
/// <param name="logger">The logger instance.</param>
public class CacheSyncScheduler(
    IQueueManager queueManager,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<CacheSyncScheduler> logger
) : IHostedService, IDisposable
{
    /// <summary>
    /// The timer for scheduling cache synchronization tasks.
    /// </summary>
    private ITimer? _timer;

    /// <summary>
    /// The interval between cache synchronization tasks.
    /// </summary>
    internal TimeSpan Interval => configuration.GetTimeSpan("CacheSync:Interval") ?? TimeSpan.FromMinutes(5);

    /// <summary>
    /// Indicates whether the scheduler should start automatically.
    /// </summary>
    internal bool AutoStart => configuration.GetValue<bool>("CacheSync:AutoStart", true);

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("CacheSyncScheduler is starting.");

        // Create timer with immediate first execution (dueTime = TimeSpan.Zero)
        // and then periodic execution at the configured interval
        _timer = timeProvider.CreateTimer(
            EnqueueWorkCallback,
            state: null,
            dueTime: TimeSpan.Zero,
            period: Interval);

        // Also start the queue.
        if (AutoStart)
        {
            logger.LogInformation("Auto-starting the cache synchronization queue.");
            await queueManager.StartQueueAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("CacheSyncScheduler is stopping.");

        // Dispose the timer to stop scheduling new work
        _timer?.Dispose();
        _timer = null;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Callback method invoked by the timer to enqueue cache synchronization work.
    /// </summary>
    /// <param name="state">The state object (not used).</param>
    private void EnqueueWorkCallback(object? state)
    {
        try
        {
            logger.LogDebug("Enqueueing cache synchronization work.");

            // Fire and forget - we don't await this
            // The queue manager will handle the work asynchronously
            _ = queueManager.EnqueueWorkAsync();

            logger.LogDebug("Cache synchronization work enqueued successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error enqueueing cache synchronization work.");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
