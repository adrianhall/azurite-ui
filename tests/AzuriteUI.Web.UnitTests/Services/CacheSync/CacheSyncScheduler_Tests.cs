using AzuriteUI.Web.Services.CacheSync;
using AzuriteUI.Web.UnitTests.Helpers;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace AzuriteUI.Web.UnitTests.Services.CacheSync;

[ExcludeFromCodeCoverage]
public class CacheSyncScheduler_Tests
{
    private readonly FakeLogger<CacheSyncScheduler> _logger = new();

    #region Interval Property Tests

    [Fact(Timeout = 15000)]
    public void Interval_WithConfiguredValue_ShouldReturnConfiguredValue()
    {
        // Arrange
        var queueManager = Substitute.For<IQueueManager>();
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            ["CacheSync:Interval"] = "00:10:00"
        });
        var timeProvider = new FakeTimeProvider();
        var scheduler = new CacheSyncScheduler(queueManager, configuration, timeProvider, _logger);

        // Act
        var interval = scheduler.Interval;

        // Assert
        interval.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact(Timeout = 15000)]
    public void Interval_WithoutConfiguredValue_ShouldReturnDefaultValue()
    {
        // Arrange
        var queueManager = Substitute.For<IQueueManager>();
        var configuration = Utils.CreateConfiguration();
        var timeProvider = new FakeTimeProvider();
        var scheduler = new CacheSyncScheduler(queueManager, configuration, timeProvider, _logger);

        // Act
        var interval = scheduler.Interval;

        // Assert
        interval.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact(Timeout = 15000)]
    public void Interval_WithInvalidConfiguredValue_ShouldReturnDefaultValue()
    {
        // Arrange
        var queueManager = Substitute.For<IQueueManager>();
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            ["CacheSync:Interval"] = "invalid"
        });
        var timeProvider = new FakeTimeProvider();
        var scheduler = new CacheSyncScheduler(queueManager, configuration, timeProvider, _logger);

        // Act
        var interval = scheduler.Interval;

        // Assert
        interval.Should().Be(TimeSpan.FromMinutes(5));
    }

    #endregion

    #region AutoStart Property Tests

    [Fact(Timeout = 15000)]
    public void AutoStart_WithConfiguredTrueValue_ShouldReturnTrue()
    {
        // Arrange
        var queueManager = Substitute.For<IQueueManager>();
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            ["CacheSync:AutoStart"] = "true"
        });
        var timeProvider = new FakeTimeProvider();
        var scheduler = new CacheSyncScheduler(queueManager, configuration, timeProvider, _logger);

        // Act
        var autoStart = scheduler.AutoStart;

        // Assert
        autoStart.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public void AutoStart_WithConfiguredFalseValue_ShouldReturnFalse()
    {
        // Arrange
        var queueManager = Substitute.For<IQueueManager>();
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            ["CacheSync:AutoStart"] = "false"
        });
        var timeProvider = new FakeTimeProvider();
        var scheduler = new CacheSyncScheduler(queueManager, configuration, timeProvider, _logger);

        // Act
        var autoStart = scheduler.AutoStart;

        // Assert
        autoStart.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public void AutoStart_WithoutConfiguredValue_ShouldReturnDefaultTrue()
    {
        // Arrange
        var queueManager = Substitute.For<IQueueManager>();
        var configuration = Utils.CreateConfiguration();
        var timeProvider = new FakeTimeProvider();
        var scheduler = new CacheSyncScheduler(queueManager, configuration, timeProvider, _logger);

        // Act
        var autoStart = scheduler.AutoStart;

        // Assert
        autoStart.Should().BeTrue();
    }

    #endregion

    #region StartAsync Tests

    [Fact(Timeout = 15000)]
    public async Task StartAsync_WithAutoStartTrue_ShouldStartQueue()
    {
        // Arrange
        var queueManager = Substitute.For<IQueueManager>();
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            ["CacheSync:Interval"] = "00:05:00",
            ["CacheSync:AutoStart"] = "true"
        });
        var timeProvider = new FakeTimeProvider();
        var scheduler = new CacheSyncScheduler(queueManager, configuration, timeProvider, _logger);

        // Act
        await scheduler.StartAsync(CancellationToken.None);

        // Assert
        await queueManager.Received(1).StartQueueAsync(Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 15000)]
    public async Task StartAsync_WithAutoStartFalse_ShouldNotStartQueue()
    {
        // Arrange
        var queueManager = Substitute.For<IQueueManager>();
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            ["CacheSync:Interval"] = "00:05:00",
            ["CacheSync:AutoStart"] = "false"
        });
        var timeProvider = new FakeTimeProvider();
        var scheduler = new CacheSyncScheduler(queueManager, configuration, timeProvider, _logger);

        // Act
        await scheduler.StartAsync(CancellationToken.None);

        // Assert
        await queueManager.DidNotReceive().StartQueueAsync(Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 15000)]
    public async Task StartAsync_ShouldTriggerImmediateExecution()
    {
        // Arrange
        var queueManager = Substitute.For<IQueueManager>();
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            ["CacheSync:Interval"] = "00:05:00",
            ["CacheSync:AutoStart"] = "true"
        });
        var timeProvider = new FakeTimeProvider();
        var scheduler = new CacheSyncScheduler(queueManager, configuration, timeProvider, _logger);

        // Act
        await scheduler.StartAsync(CancellationToken.None);

        // Give the callback time to execute
        await Task.Delay(100);

        // Assert
        await queueManager.Received().EnqueueWorkAsync();
    }

    [Fact(Timeout = 15000)]
    public async Task StartAsync_AfterTimerPeriod_ShouldEnqueueWorkAgain()
    {
        // Arrange
        var queueManager = Substitute.For<IQueueManager>();
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            ["CacheSync:Interval"] = "00:05:00",
            ["CacheSync:AutoStart"] = "true"
        });
        var timeProvider = new FakeTimeProvider();
        var scheduler = new CacheSyncScheduler(queueManager, configuration, timeProvider, _logger);

        // Act
        await scheduler.StartAsync(CancellationToken.None);

        // Advance time by the interval period
        timeProvider.Advance(TimeSpan.FromMinutes(5));

        // Give the callback time to execute
        await Task.Delay(100);

        // Assert
        await queueManager.Received(2).EnqueueWorkAsync();
    }

    #endregion

    #region StopAsync Tests

    [Fact(Timeout = 15000)]
    public async Task StopAsync_WithoutStarting_ShouldCompleteSuccessfully()
    {
        // Arrange
        var queueManager = Substitute.For<IQueueManager>();
        var configuration = Utils.CreateConfiguration();
        var timeProvider = new FakeTimeProvider();
        var scheduler = new CacheSyncScheduler(queueManager, configuration, timeProvider, _logger);

        // Act
        Func<Task> act = async () => await scheduler.StopAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact(Timeout = 15000)]
    public async Task StopAsync_AfterStop_ShouldNotEnqueueMoreWork()
    {
        // Arrange
        var queueManager = Substitute.For<IQueueManager>();
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            ["CacheSync:Interval"] = "00:05:00",
            ["CacheSync:AutoStart"] = "true"
        });
        var timeProvider = new FakeTimeProvider();
        var scheduler = new CacheSyncScheduler(queueManager, configuration, timeProvider, _logger);
        await scheduler.StartAsync(CancellationToken.None);

        // Reset the call count
        queueManager.ClearReceivedCalls();

        // Act
        await scheduler.StopAsync(CancellationToken.None);

        // Advance time after stopping
        timeProvider.Advance(TimeSpan.FromMinutes(5));
        await Task.Delay(100);

        // Assert
        await queueManager.DidNotReceive().EnqueueWorkAsync();
    }

    #endregion

    #region EnqueueWorkCallback Tests

    [Fact(Timeout = 15000)]
    public async Task EnqueueWorkCallback_ShouldCallEnqueueWorkAsync()
    {
        // Arrange
        var queueManager = Substitute.For<IQueueManager>();
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            ["CacheSync:Interval"] = "00:05:00",
            ["CacheSync:AutoStart"] = "false"
        });
        var timeProvider = new FakeTimeProvider();
        var scheduler = new CacheSyncScheduler(queueManager, configuration, timeProvider, _logger);

        // Act
        await scheduler.StartAsync(CancellationToken.None);

        // Give the callback time to execute
        await Task.Delay(100);

        // Assert
        await queueManager.Received(1).EnqueueWorkAsync();
    }

    [Fact(Timeout = 15000)]
    public async Task EnqueueWorkCallback_WhenExceptionThrown_ShouldNotStopTimer()
    {
        // Arrange
        var queueManager = Substitute.For<IQueueManager>();
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            ["CacheSync:Interval"] = "00:05:00",
            ["CacheSync:AutoStart"] = "false"
        });
        var timeProvider = new FakeTimeProvider();

        var callCount = 0;
        queueManager.EnqueueWorkAsync().Returns(_ =>
        {
            callCount++;
            if (callCount == 1)
            {
                return Task.FromException(new InvalidOperationException("Test exception"));
            }
            return Task.CompletedTask;
        });

        var scheduler = new CacheSyncScheduler(queueManager, configuration, timeProvider, _logger);

        // Act
        await scheduler.StartAsync(CancellationToken.None);

        // Give the first callback time to execute and fail
        await Task.Delay(100);

        // Advance time to trigger another execution
        timeProvider.Advance(TimeSpan.FromMinutes(5));
        await Task.Delay(100);

        // Assert
        await queueManager.Received(2).EnqueueWorkAsync();
    }

    #endregion

    #region Dispose Tests

    [Fact(Timeout = 15000)]
    public void Dispose_WithoutStarting_ShouldCompleteSuccessfully()
    {
        // Arrange
        var queueManager = Substitute.For<IQueueManager>();
        var configuration = Utils.CreateConfiguration();
        var timeProvider = new FakeTimeProvider();
        var scheduler = new CacheSyncScheduler(queueManager, configuration, timeProvider, _logger);

        // Act & Assert
        scheduler.Invoking(s => s.Dispose()).Should().NotThrow();
    }

    [Fact(Timeout = 15000)]
    public async Task Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var queueManager = Substitute.For<IQueueManager>();
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            ["CacheSync:Interval"] = "00:05:00",
            ["CacheSync:AutoStart"] = "true"
        });
        var timeProvider = new FakeTimeProvider();
        var scheduler = new CacheSyncScheduler(queueManager, configuration, timeProvider, _logger);
        await scheduler.StartAsync(CancellationToken.None);

        // Act & Assert
        scheduler.Invoking(s =>
        {
            s.Dispose();
            s.Dispose();
        }).Should().NotThrow();
    }

    #endregion
}
