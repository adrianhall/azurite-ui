using AzuriteUI.Web.Services.CacheSync;
using AzuriteUI.Web.Services.CacheSync.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AzuriteUI.Web.UnitTests.Services.CacheSync;

[ExcludeFromCodeCoverage]
public class QueueManager_Tests
{
    private readonly FakeLogger<QueueManager> _logger = new();
    private readonly IQueueWorker _worker = Substitute.For<IQueueWorker>();

    #region Constructor Tests

    [Fact(Timeout = 15000)]
    public void Constructor_WithNullWorker_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new QueueManager(null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(Timeout = 15000)]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new QueueManager(_worker, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(Timeout = 15000)]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var queueManager = new QueueManager(_worker, _logger);

        // Assert
        queueManager.Should().NotBeNull();
        queueManager.QueueSize.Should().Be(0);
        queueManager.CurrentItem.Should().BeNull();
    }

    #endregion

    #region QueueSize Property Tests

    [Fact(Timeout = 15000)]
    public void QueueSize_WhenEmpty_ShouldReturnZero()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);

        // Act
        var size = queueManager.QueueSize;

        // Assert
        size.Should().Be(0);
    }

    [Fact(Timeout = 15000)]
    public async Task QueueSize_WithEnqueuedItem_ShouldReturnOne()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);

        // Act
        await queueManager.EnqueueWorkAsync();
        var size = queueManager.QueueSize;

        // Assert
        size.Should().Be(1);
    }

    [Fact(Timeout = 15000)]
    public async Task QueueSize_WithMultipleEnqueueAttempts_ShouldReturnOne()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);

        // Act
        await queueManager.EnqueueWorkAsync();
        await queueManager.EnqueueWorkAsync();
        var size = queueManager.QueueSize;

        // Assert
        size.Should().Be(1);
    }

    #endregion

    #region QueuedItems Property Tests

    [Fact(Timeout = 15000)]
    public void QueuedItems_WhenEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);

        // Act
        var items = queueManager.QueuedItems;

        // Assert
        items.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task QueuedItems_WithEnqueuedItem_ShouldReturnListWithOneItem()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);

        // Act
        var work = await queueManager.EnqueueWorkAsync();
        var items = queueManager.QueuedItems;

        // Assert
        items.Should().HaveCount(1);
        items[0].Should().BeSameAs(work);
    }

    #endregion

    #region CurrentItem Property Tests

    [Fact(Timeout = 15000)]
    public void CurrentItem_Initially_ShouldBeNull()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);

        // Act
        var currentItem = queueManager.CurrentItem;

        // Assert
        currentItem.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public async Task CurrentItem_WhileProcessing_ShouldBeSet()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        var tcs = new TaskCompletionSource<bool>();

        _worker.ExecuteAsync(Arg.Any<QueuedWork>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                tcs.SetResult(true);
                await Task.Delay(1000);
            });

        var work = await queueManager.EnqueueWorkAsync();
        await queueManager.StartQueueAsync();

        // Wait for worker to start
        await tcs.Task;

        // Act
        var currentItem = queueManager.CurrentItem;

        // Assert
        currentItem.Should().BeSameAs(work);

        // Cleanup
        await queueManager.StopQueueAsync(finishProcessing: false);
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task CurrentItem_AfterProcessingComplete_ShouldBeNull()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);

        await queueManager.EnqueueWorkAsync();
        await queueManager.StartQueueAsync();

        // Wait for processing to complete
        await Task.Delay(100);

        // Act
        var currentItem = queueManager.CurrentItem;

        // Assert
        currentItem.Should().BeNull();

        // Cleanup
        await queueManager.StopQueueAsync();
        queueManager.Dispose();
    }

    #endregion

    #region EnqueueWorkAsync Tests

    [Fact(Timeout = 15000)]
    public async Task EnqueueWorkAsync_WithValidInput_ShouldEnqueueWork()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);

        // Act
        var work = await queueManager.EnqueueWorkAsync();

        // Assert
        work.Should().NotBeNull();
        work.Id.Should().NotBeEmpty();
        queueManager.QueueSize.Should().Be(1);
    }

    [Fact(Timeout = 15000)]
    public async Task EnqueueWorkAsync_WithExistingEnqueuedItem_ShouldReturnExistingWork()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);

        // Act
        var work1 = await queueManager.EnqueueWorkAsync();
        var work2 = await queueManager.EnqueueWorkAsync();

        // Assert
        work1.Should().BeSameAs(work2);
        queueManager.QueueSize.Should().Be(1);
    }

    [Fact(Timeout = 15000)]
    public async Task EnqueueWorkAsync_ShouldRaiseQueueChangedEvent()
    {
        // Arrange
        DateTimeOffset beforeEnqueue = DateTimeOffset.UtcNow;
        var queueManager = new QueueManager(_worker, _logger);
        QueueManagerEventArgs? eventArgs = null;
        queueManager.QueueChanged += (sender, args) => eventArgs = args;

        // Act
        var work = await queueManager.EnqueueWorkAsync();

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.EventType.Should().Be(QueueEvent.WorkEnqueued);
        eventArgs.QueuedWork.Should().BeSameAs(work);
        eventArgs.EventTime.Should().BeAfter(beforeEnqueue);
    }

    [Fact(Timeout = 15000)]
    public async Task EnqueueWorkAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await queueManager.EnqueueWorkAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact(Timeout = 15000)]
    public async Task EnqueueWorkAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        queueManager.Dispose();

        // Act
        Func<Task> act = async () => await queueManager.EnqueueWorkAsync();

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region StartQueueAsync Tests

    [Fact(Timeout = 15000)]
    public async Task StartQueueAsync_WithValidInput_ShouldStartQueue()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);

        // Act
        await queueManager.StartQueueAsync();

        // Assert - Queue should be running (verified by being able to stop it)
        await queueManager.StopQueueAsync();

        // Cleanup
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task StartQueueAsync_ShouldRaiseQueueChangedEvent()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        QueueManagerEventArgs? eventArgs = null;
        queueManager.QueueChanged += (sender, args) => eventArgs = args;

        // Act
        await queueManager.StartQueueAsync();

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.EventType.Should().Be(QueueEvent.QueueStarted);

        // Cleanup
        await queueManager.StopQueueAsync();
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task StartQueueAsync_WhenAlreadyRunning_ShouldNotStartAgain()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        await queueManager.StartQueueAsync();

        // Act
        await queueManager.StartQueueAsync();

        // Assert - Should log warning (check logs)
        _logger.LatestRecord.Level.Should().Be(LogLevel.Warning);

        // Cleanup
        await queueManager.StopQueueAsync();
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task StartQueueAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        queueManager.Dispose();

        // Act
        Func<Task> act = async () => await queueManager.StartQueueAsync();

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact(Timeout = 15000)]
    public async Task StartQueueAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await queueManager.StartQueueAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Cleanup
        queueManager.Dispose();
    }

    #endregion

    #region StopQueueAsync Tests

    [Fact(Timeout = 15000)]
    public async Task StopQueueAsync_WithDefaultParameters_ShouldStopQueueAndFinishProcessing()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        await queueManager.StartQueueAsync();

        // Act
        await queueManager.StopQueueAsync();

        // Assert - Should be stopped (no exception on dispose)
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task StopQueueAsync_WithFinishProcessingTrue_ShouldAllowCurrentWorkToComplete()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        var workCompleted = false;
        var tcs = new TaskCompletionSource<bool>();

        _worker.ExecuteAsync(Arg.Any<QueuedWork>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var token = callInfo.ArgAt<CancellationToken>(1);
                tcs.SetResult(true);
                await Task.Delay(200, CancellationToken.None);
                workCompleted = !token.IsCancellationRequested;
            });

        await queueManager.EnqueueWorkAsync();
        await queueManager.StartQueueAsync();

        // Wait for worker to start
        await tcs.Task;

        // Act
        await queueManager.StopQueueAsync(finishProcessing: true);

        // Assert
        workCompleted.Should().BeTrue();

        // Cleanup
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task StopQueueAsync_WithFinishProcessingFalse_ShouldCancelCurrentWork()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        var tcs = new TaskCompletionSource<bool>();
        var cancellationReceived = false;

        _worker.ExecuteAsync(Arg.Any<QueuedWork>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var token = callInfo.ArgAt<CancellationToken>(1);
                tcs.SetResult(true);
                try
                {
                    await Task.Delay(5000, token);
                }
                catch (OperationCanceledException)
                {
                    cancellationReceived = true;
                    throw;
                }
            });

        await queueManager.EnqueueWorkAsync();
        await queueManager.StartQueueAsync();

        // Wait for worker to start
        await tcs.Task;

        // Act
        await queueManager.StopQueueAsync(finishProcessing: false);

        // Assert - Worker should have received a cancelled token
        cancellationReceived.Should().BeTrue();

        // Cleanup
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task StopQueueAsync_ShouldRaiseQueueChangedEvent()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        QueueManagerEventArgs? eventArgs = null;
        queueManager.QueueChanged += (sender, args) =>
        {
            if (args.EventType == QueueEvent.QueueStopped)
            {
                eventArgs = args;
            }
        };
        await queueManager.StartQueueAsync();

        // Act
        await queueManager.StopQueueAsync();

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.EventType.Should().Be(QueueEvent.QueueStopped);

        // Cleanup
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task StopQueueAsync_WhenNotRunning_ShouldNotThrow()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);

        // Act
        await queueManager.StopQueueAsync();

        // Assert - Should log warning
        _logger.LatestRecord.Level.Should().Be(LogLevel.Warning);

        // Cleanup
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task StopQueueAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        queueManager.Dispose();

        // Act
        Func<Task> act = async () => await queueManager.StopQueueAsync();

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact(Timeout = 15000)]
    public async Task StopQueueAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        await queueManager.StartQueueAsync();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await queueManager.StopQueueAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Cleanup
        await queueManager.StopQueueAsync();
        queueManager.Dispose();
    }

    #endregion

    #region ClearQueueAsync Tests

    [Fact(Timeout = 15000)]
    public async Task ClearQueueAsync_WithEnqueuedItems_ShouldClearQueue()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        await queueManager.EnqueueWorkAsync();

        // Act
        await queueManager.ClearQueueAsync();

        // Assert
        queueManager.QueueSize.Should().Be(0);
        queueManager.QueuedItems.Should().BeEmpty();

        // Cleanup
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task ClearQueueAsync_WithEmptyQueue_ShouldNotThrow()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);

        // Act
        Func<Task> act = async () => await queueManager.ClearQueueAsync();

        // Assert
        await act.Should().NotThrowAsync();

        // Cleanup
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task ClearQueueAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        queueManager.Dispose();

        // Act
        Func<Task> act = async () => await queueManager.ClearQueueAsync();

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact(Timeout = 15000)]
    public async Task ClearQueueAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await queueManager.ClearQueueAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Cleanup
        queueManager.Dispose();
    }

    #endregion

    #region ProcessQueueAsync Tests

    [Fact(Timeout = 15000)]
    public async Task ProcessQueueAsync_WithEnqueuedWork_ShouldProcessWork()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);

        await queueManager.EnqueueWorkAsync();
        await queueManager.StartQueueAsync();

        // Wait for processing
        await Task.Delay(100);

        // Act & Assert
        await _worker.Received(1).ExecuteAsync(Arg.Any<QueuedWork>(), Arg.Any<CancellationToken>());

        // Cleanup
        await queueManager.StopQueueAsync();
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task ProcessQueueAsync_WithMultipleEnqueuedWorks_ShouldProcessOnlyOne()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);

        await queueManager.EnqueueWorkAsync();
        await queueManager.EnqueueWorkAsync(); // Should return existing work
        await queueManager.StartQueueAsync();

        // Wait for processing
        await Task.Delay(100);

        // Act & Assert
        await _worker.Received(1).ExecuteAsync(Arg.Any<QueuedWork>(), Arg.Any<CancellationToken>());

        // Cleanup
        await queueManager.StopQueueAsync();
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task ProcessQueueAsync_WithCancellation_ShouldStopProcessing()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        var cts = new CancellationTokenSource();

        // Act
        var task = queueManager.ProcessQueueAsync(cts.Token);
        cts.Cancel();
        await task;

        // Assert - Should complete without throwing
        task.IsCompleted.Should().BeTrue();

        // Cleanup
        queueManager.Dispose();
    }

    #endregion

    #region ProcessWorkItemAsync Tests

    [Fact(Timeout = 15000)]
    public async Task ProcessWorkItemAsync_WithValidWork_ShouldExecuteWorker()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        var work = new QueuedWork();

        // Act
        await queueManager.ProcessWorkItemAsync(work);

        // Assert
        await _worker.Received(1).ExecuteAsync(work, Arg.Any<CancellationToken>());

        // Cleanup
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task ProcessWorkItemAsync_ShouldRaiseWorkStartedEvent()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        QueueManagerEventArgs? eventArgs = null;
        queueManager.QueueChanged += (sender, args) =>
        {
            if (args.EventType == QueueEvent.WorkStarted)
            {
                eventArgs = args;
            }
        };
        var work = new QueuedWork();

        // Act
        await queueManager.ProcessWorkItemAsync(work);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.EventType.Should().Be(QueueEvent.WorkStarted);
        eventArgs.QueuedWork.Should().BeSameAs(work);

        // Cleanup
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task ProcessWorkItemAsync_OnSuccess_ShouldRaiseWorkFinishedEvent()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        QueueManagerEventArgs? eventArgs = null;
        queueManager.QueueChanged += (sender, args) =>
        {
            if (args.EventType == QueueEvent.WorkFinished)
            {
                eventArgs = args;
            }
        };
        var work = new QueuedWork();

        // Act
        await queueManager.ProcessWorkItemAsync(work);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.EventType.Should().Be(QueueEvent.WorkFinished);
        eventArgs.QueuedWork.Should().BeSameAs(work);

        // Cleanup
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task ProcessWorkItemAsync_OnWorkerException_ShouldRaiseWorkErroredEvent()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        var exception = new InvalidOperationException("Test exception");
        _worker.ExecuteAsync(Arg.Any<QueuedWork>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);
        QueueManagerEventArgs? eventArgs = null;
        queueManager.QueueChanged += (sender, args) =>
        {
            if (args.EventType == QueueEvent.WorkErrored)
            {
                eventArgs = args;
            }
        };
        var work = new QueuedWork();

        // Act
        await queueManager.ProcessWorkItemAsync(work);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.EventType.Should().Be(QueueEvent.WorkErrored);
        eventArgs.QueuedWork.Should().BeSameAs(work);
        eventArgs.Exception.Should().BeSameAs(exception);

        // Cleanup
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task ProcessWorkItemAsync_OnWorkerException_ShouldLogError()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        var exception = new InvalidOperationException("Test exception");
        _worker.ExecuteAsync(Arg.Any<QueuedWork>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);
        var work = new QueuedWork();

        // Act
        await queueManager.ProcessWorkItemAsync(work);

        // Assert
        _logger.LatestRecord.Level.Should().Be(LogLevel.Error);

        // Cleanup
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task ProcessWorkItemAsync_AfterCompletion_ShouldClearCurrentItem()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        var work = new QueuedWork();

        // Act
        await queueManager.ProcessWorkItemAsync(work);

        // Assert
        queueManager.CurrentItem.Should().BeNull();

        // Cleanup
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task ProcessWorkItemAsync_OnException_ShouldClearCurrentItem()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        _worker.ExecuteAsync(Arg.Any<QueuedWork>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Test exception"));
        var work = new QueuedWork();

        // Act
        await queueManager.ProcessWorkItemAsync(work);

        // Assert
        queueManager.CurrentItem.Should().BeNull();

        // Cleanup
        queueManager.Dispose();
    }

    #endregion

    #region SendQueueManagerEvent Tests

    [Fact(Timeout = 15000)]
    public void SendQueueManagerEvent_WithValidEvent_ShouldInvokeEventHandler()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        QueueManagerEventArgs? eventArgs = null;
        queueManager.QueueChanged += (sender, args) => eventArgs = args;

        // Act
        queueManager.SendQueueManagerEvent(QueueEvent.QueueStarted);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.EventType.Should().Be(QueueEvent.QueueStarted);

        // Cleanup
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public void SendQueueManagerEvent_WithWorkAndException_ShouldIncludeInEventArgs()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        var work = new QueuedWork();
        var exception = new InvalidOperationException("Test exception");
        QueueManagerEventArgs? eventArgs = null;
        queueManager.QueueChanged += (sender, args) => eventArgs = args;

        // Act
        queueManager.SendQueueManagerEvent(QueueEvent.WorkErrored, work, exception);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.EventType.Should().Be(QueueEvent.WorkErrored);
        eventArgs.QueuedWork.Should().BeSameAs(work);
        eventArgs.Exception.Should().BeSameAs(exception);

        // Cleanup
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public void SendQueueManagerEvent_WithExceptionInHandler_ShouldNotThrow()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        queueManager.QueueChanged += (sender, args) => throw new InvalidOperationException("Handler exception");

        // Act
        Action act = () => queueManager.SendQueueManagerEvent(QueueEvent.QueueStarted);

        // Assert
        act.Should().NotThrow();
        _logger.LatestRecord.Level.Should().Be(LogLevel.Error);

        // Cleanup
        queueManager.Dispose();
    }

    #endregion

    #region LogExceptions Tests

    [Fact(Timeout = 15000)]
    public void LogExceptions_WithSuccessfulAction_ShouldExecuteAction()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        var actionExecuted = false;

        // Act
        queueManager.LogExceptions("Test error", () => actionExecuted = true);

        // Assert
        actionExecuted.Should().BeTrue();

        // Cleanup
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public void LogExceptions_WithExceptionInAction_ShouldLogError()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);

        // Act
        queueManager.LogExceptions("Test error", () => throw new InvalidOperationException("Test exception"));

        // Assert
        _logger.LatestRecord.Level.Should().Be(LogLevel.Error);

        // Cleanup
        queueManager.Dispose();
    }

    #endregion

    #region SwallowCancellationsAsync Tests

    [Fact(Timeout = 15000)]
    public async Task SwallowCancellationsAsync_WithSuccessfulTask_ShouldComplete()
    {
        // Arrange
        var task = Task.CompletedTask;

        // Act
        Func<Task> act = async () => await QueueManager.SwallowCancellationsAsync(task);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact(Timeout = 15000)]
    public async Task SwallowCancellationsAsync_WithCancelledTask_ShouldNotThrow()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var task = Task.FromCanceled(cts.Token);

        // Act
        Func<Task> act = async () => await QueueManager.SwallowCancellationsAsync(task);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact(Timeout = 15000)]
    public async Task SwallowCancellationsAsync_WithFailedTask_ShouldThrow()
    {
        // Arrange
        var task = Task.FromException(new InvalidOperationException("Test exception"));

        // Act
        Func<Task> act = async () => await QueueManager.SwallowCancellationsAsync(task);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region LogExceptionWithCancellation Tests

    [Fact(Timeout = 15000)]
    public async Task LogExceptionWithCancellation_WithSuccessfulTask_ShouldComplete()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        var taskExecuted = false;

        // Act
        await queueManager.LogExceptionWithCancellation(async _ =>
        {
            await Task.Yield();
            taskExecuted = true;
        });

        // Assert
        taskExecuted.Should().BeTrue();

        // Cleanup
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task LogExceptionWithCancellation_WithCancellation_ShouldLogDebug()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        await queueManager.LogExceptionWithCancellation(async ct =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        }, cts.Token);

        // Assert
        _logger.LatestRecord.Level.Should().Be(LogLevel.Debug);

        // Cleanup
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task LogExceptionWithCancellation_WithException_ShouldLogError()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);

        // Act
        await queueManager.LogExceptionWithCancellation(async _ =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Test exception");
        });

        // Assert
        _logger.LatestRecord.Level.Should().Be(LogLevel.Error);

        // Cleanup
        queueManager.Dispose();
    }

    #endregion

    #region CancelTokenSource Tests

    [Fact(Timeout = 15000)]
    public void CancelTokenSource_WithNullTokenSource_ShouldNotThrow()
    {
        // Arrange
        CancellationTokenSource? cts = null;

        // Act
        Action act = () => QueueManager.CancelTokenSource(ref cts);

        // Assert
        act.Should().NotThrow();
        cts.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void CancelTokenSource_WithValidTokenSource_ShouldCancelToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        QueueManager.CancelTokenSource(ref cts);

        // Assert
        token.IsCancellationRequested.Should().BeTrue();
        cts.Should().NotBeNull(); // Reference should still exist
    }

    [Fact(Timeout = 15000)]
    public void CancelTokenSource_WithAlreadyCancelledToken_ShouldNotThrow()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var token = cts.Token;

        // Act
        Action act = () => QueueManager.CancelTokenSource(ref cts);

        // Assert
        act.Should().NotThrow();
        token.IsCancellationRequested.Should().BeTrue();
        cts.Should().NotBeNull();
    }

    [Fact(Timeout = 15000)]
    public void CancelTokenSource_CalledTwiceOnSameVariable_ShouldNotThrow()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        // Act
        QueueManager.CancelTokenSource(ref cts);
        Action act = () => QueueManager.CancelTokenSource(ref cts);

        // Assert
        act.Should().NotThrow();
        cts.Should().NotBeNull();
        cts!.Token.IsCancellationRequested.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public void CancelTokenSource_WithRegisteredCallback_ShouldInvokeCallback()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var callbackInvoked = false;
        cts.Token.Register(() => callbackInvoked = true);

        // Act
        QueueManager.CancelTokenSource(ref cts);

        // Assert
        callbackInvoked.Should().BeTrue();
        cts.Should().NotBeNull();
    }

    [Fact(Timeout = 15000)]
    public void CancelTokenSource_WithLinkedTokenSource_ShouldCancelLinkedToken()
    {
        // Arrange
        var primaryCts = new CancellationTokenSource();
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(primaryCts.Token);
        var linkedToken = linkedCts.Token;

        // Act
        QueueManager.CancelTokenSource(ref linkedCts);

        // Assert
        linkedToken.IsCancellationRequested.Should().BeTrue();
        primaryCts.Token.IsCancellationRequested.Should().BeFalse(); // Primary should not be affected

        // Cleanup
        primaryCts.Dispose();
        linkedCts?.Dispose();
    }

    [Fact(Timeout = 15000)]
    public void CancelTokenSource_ShouldNotSetReferenceToNull()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        // Act
        QueueManager.CancelTokenSource(ref cts);

        // Assert
        cts.Should().NotBeNull();

        // Cleanup
        cts.Dispose();
    }

    #endregion

    #region DisposeTokenSource Tests

    [Fact(Timeout = 15000)]
    public void DisposeTokenSource_WithNullTokenSource_ShouldNotThrow()
    {
        // Arrange
        CancellationTokenSource? cts = null;

        // Act
        Action act = () => QueueManager.DisposeTokenSource(ref cts);

        // Assert
        act.Should().NotThrow();
        cts.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void DisposeTokenSource_WithValidTokenSource_ShouldDisposeAndSetToNull()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        // Act
        QueueManager.DisposeTokenSource(ref cts);

        // Assert
        cts.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void DisposeTokenSource_WithAlreadyDisposedTokenSource_ShouldSetToNull()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Dispose();

        // Act
        Action act = () => QueueManager.DisposeTokenSource(ref cts);

        // Assert
        act.Should().NotThrow();
        cts.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void DisposeTokenSource_CalledTwiceOnSameVariable_ShouldNotThrow()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        // Act
        QueueManager.DisposeTokenSource(ref cts);
        Action act = () => QueueManager.DisposeTokenSource(ref cts);

        // Assert
        act.Should().NotThrow();
        cts.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void DisposeTokenSource_WithMultipleReferences_ShouldOnlySetPassedReferenceToNull()
    {
        // Arrange
        var cts1 = new CancellationTokenSource();
        var cts2 = cts1;

        // Act
        QueueManager.DisposeTokenSource(ref cts1);

        // Assert
        cts1.Should().BeNull();
        cts2.Should().NotBeNull(); // cts2 still holds the reference, but the object is disposed
    }

    [Fact(Timeout = 15000)]
    public void DisposeTokenSource_AfterCancel_ShouldStillDisposeAndSetToNull()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        cts.Cancel();

        // Act
        QueueManager.DisposeTokenSource(ref cts);

        // Assert
        cts.Should().BeNull();
        token.IsCancellationRequested.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public void DisposeTokenSource_WithLinkedTokenSource_ShouldDisposeAndSetToNull()
    {
        // Arrange
        var primaryCts = new CancellationTokenSource();
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(primaryCts.Token);

        // Act
        QueueManager.DisposeTokenSource(ref linkedCts);

        // Assert
        linkedCts.Should().BeNull();
        primaryCts.Token.IsCancellationRequested.Should().BeFalse(); // Primary should not be affected

        // Cleanup
        primaryCts.Dispose();
    }

    #endregion

    #region Dispose Tests

    [Fact(Timeout = 15000)]
    public void Dispose_WithoutStarting_ShouldNotThrow()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);

        // Act
        Action act = () => queueManager.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact(Timeout = 15000)]
    public async Task Dispose_AfterStarting_ShouldStopQueue()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        await queueManager.StartQueueAsync();

        // Act
        Action act = () => queueManager.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact(Timeout = 15000)]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);

        // Act
        Action act = () =>
        {
            queueManager.Dispose();
            queueManager.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Integration Tests

    [Fact(Timeout = 15000)]
    public async Task IntegrationTest_StartEnqueueProcessStop_ShouldWorkCorrectly()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        var events = new List<QueueEvent>();
        queueManager.QueueChanged += (sender, args) => events.Add(args.EventType);

        // Act
        await queueManager.StartQueueAsync();
        var work = await queueManager.EnqueueWorkAsync();
        await Task.Delay(100); // Wait for processing
        await queueManager.StopQueueAsync();

        // Assert
        events.Should().Contain(QueueEvent.QueueStarted);
        events.Should().Contain(QueueEvent.WorkEnqueued);
        events.Should().Contain(QueueEvent.WorkStarted);
        events.Should().Contain(QueueEvent.WorkFinished);
        events.Should().Contain(QueueEvent.QueueStopped);
        await _worker.Received(1).ExecuteAsync(work, Arg.Any<CancellationToken>());

        // Cleanup
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task IntegrationTest_EnqueueMultipleTimes_ShouldReturnSameWorkWhileEnqueued()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        var tcs = new TaskCompletionSource<bool>();

        // Make worker block so work stays in processing state
        _worker.ExecuteAsync(Arg.Any<QueuedWork>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                tcs.SetResult(true);
                await Task.Delay(5000);
            });

        // Act
        var work1 = await queueManager.EnqueueWorkAsync();
        var work2 = await queueManager.EnqueueWorkAsync();
        var work3 = await queueManager.EnqueueWorkAsync();

        // Assert
        work1.Should().BeSameAs(work2);
        work2.Should().BeSameAs(work3);

        // Cleanup
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task IntegrationTest_EnqueueAfterProcessing_ShouldProcessAgain()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);

        await queueManager.StartQueueAsync();

        // Act
        await queueManager.EnqueueWorkAsync();
        await Task.Delay(100); // Wait for first processing

        await queueManager.EnqueueWorkAsync();
        await Task.Delay(100); // Wait for second processing

        // Assert
        await _worker.Received(2).ExecuteAsync(Arg.Any<QueuedWork>(), Arg.Any<CancellationToken>());

        // Cleanup
        await queueManager.StopQueueAsync();
        queueManager.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task IntegrationTest_ClearQueueWhileProcessing_ShouldNotAffectCurrentWork()
    {
        // Arrange
        var queueManager = new QueueManager(_worker, _logger);
        var tcs = new TaskCompletionSource<bool>();

        _worker.ExecuteAsync(Arg.Any<QueuedWork>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                tcs.SetResult(true);
                await Task.Delay(200);
            });

        await queueManager.StartQueueAsync();
        await queueManager.EnqueueWorkAsync();

        // Wait for work to start
        await tcs.Task;

        // Act
        await queueManager.ClearQueueAsync();

        // Wait for work to complete
        await Task.Delay(300);

        // Assert
        await _worker.Received(1).ExecuteAsync(Arg.Any<QueuedWork>(), Arg.Any<CancellationToken>());

        // Cleanup
        await queueManager.StopQueueAsync();
        queueManager.Dispose();
    }

    #endregion
}
