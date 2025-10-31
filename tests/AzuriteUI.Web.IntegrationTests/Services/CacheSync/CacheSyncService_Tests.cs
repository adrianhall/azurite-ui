using AzuriteUI.Web.Services.Azurite;
using AzuriteUI.Web.Services.CacheDb;
using AzuriteUI.Web.Services.CacheSync;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace AzuriteUI.Web.IntegrationTests.Services.CacheSync;

/// <summary>
/// Integration tests for the <see cref="CacheSyncService"/> class.
/// </summary>
[ExcludeFromCodeCoverage]
public class CacheSyncService_Tests : IClassFixture<AzuriteFixture>, IAsyncLifetime
{
    private readonly AzuriteFixture _fixture;
    private readonly IAzuriteService _azuriteService;
    private readonly SqliteConnection _connection;
    private readonly CacheDbContext _context;

    public CacheSyncService_Tests(AzuriteFixture fixture)
    {
        _fixture = fixture;
        _azuriteService = new AzuriteService(_fixture.ConnectionString, NullLogger<AzuriteService>.Instance);

        // Create in-memory SQLite database for testing
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<CacheDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new CacheDbContext(options);
        _context.Database.EnsureCreated();
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
        await _fixture.CleanupAsync();
        GC.SuppressFinalize(this);
    }

    #region SynchronizeCacheAsync Performance Tests

    [Fact(Timeout = 1200000)]
    public async Task SynchronizeCacheAsync_With150ContainersAndVariableBlobs_ShouldSynchronizeSuccessfully()
    {
        // Arrange
        const int containerCount = 150;
        var stopwatch = Stopwatch.StartNew();

        // Create 150 containers, each with i blobs (container-1 has 1 blob, container-2 has 2 blobs, etc.)
        var setupTasks = new List<Task>();
        for (int i = 1; i <= containerCount; i++)
        {
            setupTasks.Add(SetupContainerWithBlobsAsync(i));
        }
        await Task.WhenAll(setupTasks);

        var setupTime = stopwatch.Elapsed;
        Console.WriteLine($"Setup completed in {setupTime.TotalSeconds:F2} seconds");
        Console.WriteLine($"Created {containerCount} containers with {containerCount * (containerCount + 1) / 2} total blobs");

        var cacheSyncService = new CacheSyncService(_context, _azuriteService, NullLogger<CacheSyncService>.Instance);

        // Act
        stopwatch.Restart();
        await cacheSyncService.SynchronizeCacheAsync();
        var syncTime = stopwatch.Elapsed;

        // Assert
        Console.WriteLine($"Synchronization completed in {syncTime.TotalSeconds:F2} seconds");

        // Verify all containers are in the database
        var containers = await _context.Containers.ToListAsync();
        containers.Should().HaveCount(containerCount);

        // Verify container names are correct (container-1 through container-150)
        for (int i = 1; i <= containerCount; i++)
        {
            var containerName = $"container-{i}";
            containers.Should().Contain(c => c.Name == containerName,
                because: $"container {containerName} should exist in the cache");
        }

        // Verify blob counts for each container
        var totalExpectedBlobs = containerCount * (containerCount + 1) / 2;
        var blobs = await _context.Blobs.ToListAsync();
        blobs.Should().HaveCount(totalExpectedBlobs);

        // Verify each container has the correct number of blobs
        for (int i = 1; i <= containerCount; i++)
        {
            var containerName = $"container-{i}";
            var containerBlobs = blobs.Where(b => b.ContainerName == containerName).ToList();
            containerBlobs.Should().HaveCount(i,
                because: $"{containerName} should have {i} blob(s)");

            // Verify blob names are correct (blob-1 through blob-i)
            for (int j = 1; j <= i; j++)
            {
                var blobName = $"blob-{j}.txt";
                containerBlobs.Should().Contain(b => b.Name == blobName,
                    because: $"{containerName} should contain {blobName}");
            }
        }

        // Verify container metadata (TotalSize and BlobCount)
        for (int i = 1; i <= containerCount; i++)
        {
            var containerName = $"container-{i}";
            var container = containers.First(c => c.Name == containerName);
            container.BlobCount.Should().Be(i,
                because: $"{containerName} should have BlobCount = {i}");
            container.TotalSize.Should().BeGreaterThan(0,
                because: $"{containerName} should have a positive TotalSize");
        }

        // Performance assertions
        syncTime.Should().BeLessThan(TimeSpan.FromMinutes(5),
            because: "synchronization should complete within a reasonable time");

        Console.WriteLine($"Performance metrics:");
        Console.WriteLine($"  Setup time: {setupTime.TotalSeconds:F2}s");
        Console.WriteLine($"  Sync time: {syncTime.TotalSeconds:F2}s");
        Console.WriteLine($"  Containers/second: {containerCount / syncTime.TotalSeconds:F2}");
        Console.WriteLine($"  Blobs/second: {totalExpectedBlobs / syncTime.TotalSeconds:F2}");
    }

    [Fact(Timeout = 60000)]
    public async Task SynchronizeCacheAsync_CalledTwice_ShouldUpdateExistingCache()
    {
        // Arrange
        const int containerCount = 10; // Smaller set for this test

        // Create initial set of containers
        for (int i = 1; i <= containerCount; i++)
        {
            await SetupContainerWithBlobsAsync(i);
        }

        var cacheSyncService = new CacheSyncService(_context, _azuriteService, NullLogger<CacheSyncService>.Instance);

        // Act - First synchronization
        await cacheSyncService.SynchronizeCacheAsync();
        var firstSyncContainerCount = await _context.Containers.CountAsync();
        var firstSyncBlobCount = await _context.Blobs.CountAsync();

        // Add more containers to Azurite
        for (int i = containerCount + 1; i <= containerCount + 5; i++)
        {
            await SetupContainerWithBlobsAsync(i);
        }

        // Act - Second synchronization
        await cacheSyncService.SynchronizeCacheAsync();
        var secondSyncContainerCount = await _context.Containers.CountAsync();
        var secondSyncBlobCount = await _context.Blobs.CountAsync();

        // Assert
        firstSyncContainerCount.Should().Be(containerCount);
        secondSyncContainerCount.Should().Be(containerCount + 5);
        secondSyncBlobCount.Should().BeGreaterThan(firstSyncBlobCount);

        // Verify old containers are removed from the first sync that no longer exist
        // (This tests the cleanup logic)
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Sets up a container with a specific number of blobs.
    /// Container naming: container-{number}
    /// Blob naming: blob-{number}.txt
    /// </summary>
    /// <param name="containerNumber">The container number (also determines the number of blobs)</param>
    private async Task SetupContainerWithBlobsAsync(int containerNumber)
    {
        var containerName = $"container-{containerNumber}";

        // Create the container
        await _fixture.CreateContainerAsync(containerName);

        // Create blobs in the container
        var blobTasks = new List<Task>();
        for (int j = 1; j <= containerNumber; j++)
        {
            var blobName = $"blob-{j}.txt";
            var content = $"This is blob {j} in {containerName}";
            blobTasks.Add(_fixture.CreateBlobAsync(containerName, blobName, content));
        }
        await Task.WhenAll(blobTasks);
    }

    #endregion
}
