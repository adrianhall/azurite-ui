using AzuriteUI.Web.Services.CacheSync;
using AzuriteUI.Web.Services.CacheSync.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AzuriteUI.Web.UnitTests.Services.CacheSync;

[ExcludeFromCodeCoverage]
public class QueueWorker_Tests
{
    private readonly FakeLogger<QueueWorker> _logger;
    private readonly ICacheSyncService _cacheSyncService;
    private readonly IServiceProvider _serviceProvider;
    private readonly QueueWorker _worker;

    public QueueWorker_Tests()
    {
        _logger = new FakeLogger<QueueWorker>();
        _cacheSyncService = Substitute.For<ICacheSyncService>();
        var services = new ServiceCollection();
        services.AddSingleton(_cacheSyncService);
        _serviceProvider = services.BuildServiceProvider();
        _worker = new QueueWorker(_serviceProvider, _logger);
    }

    #region ExecuteAsync Tests

    [Fact(Timeout = 15000)]
    public async Task ExecuteAsync_WithValidWork_ShouldCallCacheSyncService()
    {
        // Arrange
        var work = new QueuedWork();

        // Act
        await _worker.ExecuteAsync(work);

        // Assert
        await _cacheSyncService.Received(1).SynchronizeCacheAsync(Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 15000)]
    public async Task ExecuteAsync_WithCancellationToken_ShouldPassTokenToCacheSyncService()
    {
        // Arrange
        var work = new QueuedWork();
        var cts = new CancellationTokenSource();

        // Act
        await _worker.ExecuteAsync(work, cts.Token);

        // Assert
        await _cacheSyncService.Received(1).SynchronizeCacheAsync(cts.Token);
    }

    [Fact(Timeout = 15000)]
    public async Task ExecuteAsync_WhenCacheSyncServiceThrowsException_ShouldThrowAndLogError()
    {
        // Arrange
        _cacheSyncService.SynchronizeCacheAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Test exception"));
        var work = new QueuedWork();

        // Act
        Func<Task> act = async () => await _worker.ExecuteAsync(work);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _logger.LatestRecord.Should().NotBeNull();
        _logger.LatestRecord.Level.Should().Be(LogLevel.Error);
    }

    [Fact(Timeout = 15000)]
    public async Task ExecuteAsync_WhenCacheSyncServiceThrowsOperationCanceledException_ShouldThrowAndLogWarning()
    {
        // Arrange
        _cacheSyncService.SynchronizeCacheAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        var work = new QueuedWork();

        // Act
        Func<Task> act = async () => await _worker.ExecuteAsync(work);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        _logger.LatestRecord.Should().NotBeNull();
        _logger.LatestRecord.Level.Should().Be(LogLevel.Warning);
    }

    [Fact(Timeout = 15000)]
    public async Task ExecuteAsync_WhenCancellationRequestedBeforeExecution_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var work = new QueuedWork();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await _worker.ExecuteAsync(work, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact(Timeout = 15000)]
    public async Task ExecuteAsync_WithMultipleConcurrentCalls_ShouldExecuteSequentially()
    {
        // Arrange
        var cacheSyncService = Substitute.For<ICacheSyncService>();
        var executionOrder = new List<int>();
        var tcs1 = new TaskCompletionSource<bool>();
        var tcs2 = new TaskCompletionSource<bool>();

        cacheSyncService.SynchronizeCacheAsync(Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var callNumber = executionOrder.Count + 1;
                executionOrder.Add(callNumber);

                if (callNumber == 1)
                {
                    // First call waits for signal
                    await tcs1.Task;
                }
                else if (callNumber == 2)
                {
                    // Second call completes immediately
                    tcs2.SetResult(true);
                }
            });

        var services = new ServiceCollection();
        services.AddSingleton(cacheSyncService);
        var serviceProvider = services.BuildServiceProvider();
        var worker = new QueueWorker(serviceProvider, _logger);
        var work1 = new QueuedWork();
        var work2 = new QueuedWork();

        // Act
        var task1 = Task.Run(async () => await worker.ExecuteAsync(work1));
        await Task.Delay(50); // Give task1 time to acquire the lock
        var task2 = Task.Run(async () => await worker.ExecuteAsync(work2));
        await Task.Delay(50); // Give task2 time to wait on the lock

        // Verify task2 hasn't started yet (only 1 call to sync service)
        await cacheSyncService.Received(1).SynchronizeCacheAsync(Arg.Any<CancellationToken>());

        // Release task1
        tcs1.SetResult(true);
        await task1;

        // Wait for task2 to complete
        await tcs2.Task;
        await task2;

        // Assert
        executionOrder.Should().ContainInOrder(1, 2);
        await cacheSyncService.Received(2).SynchronizeCacheAsync(Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 15000)]
    public async Task ExecuteAsync_WhenServiceProviderCannotResolveService_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var worker = new QueueWorker(serviceProvider, _logger);
        var work = new QueuedWork();

        // Act
        Func<Task> act = async () => await worker.ExecuteAsync(work);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact(Timeout = 15000)]
    public async Task ExecuteAsync_AfterSuccessfulExecution_ShouldAllowSubsequentExecution()
    {
        // Arrange
        var work1 = new QueuedWork();
        var work2 = new QueuedWork();

        // Act
        await _worker.ExecuteAsync(work1);
        await _worker.ExecuteAsync(work2);

        // Assert
        await _cacheSyncService.Received(2).SynchronizeCacheAsync(Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 15000)]
    public async Task ExecuteAsync_AfterFailedExecution_ShouldAllowSubsequentExecution()
    {
        // Arrange
        var cacheSyncService = Substitute.For<ICacheSyncService>();
        var callCount = 0;
        cacheSyncService.SynchronizeCacheAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("First call fails");
                }
                return Task.CompletedTask;
            });
        var services = new ServiceCollection();
        services.AddSingleton(cacheSyncService);
        var serviceProvider = services.BuildServiceProvider();
        var worker = new QueueWorker(serviceProvider, _logger);
        var work1 = new QueuedWork();
        var work2 = new QueuedWork();

        // Act
        Func<Task> act1 = async () => await worker.ExecuteAsync(work1);
        await act1.Should().ThrowAsync<InvalidOperationException>();

        await worker.ExecuteAsync(work2);

        // Assert
        await cacheSyncService.Received(2).SynchronizeCacheAsync(Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 15000)]
    public async Task ExecuteAsync_WhenScopeDisposed_ShouldNotThrow()
    {
        // Arrange
        var work = new QueuedWork();

        // Act
        await _worker.ExecuteAsync(work);

        // Assert - If we reach here without exception, the scope was properly disposed
        await _cacheSyncService.Received(1).SynchronizeCacheAsync(Arg.Any<CancellationToken>());
    }

    #endregion
}
