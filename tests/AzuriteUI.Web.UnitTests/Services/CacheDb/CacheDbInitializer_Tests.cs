using AzuriteUI.Web.Services.CacheDb;
using AzuriteUI.Web.Services.CacheDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AzuriteUI.Web.UnitTests.Services.CacheDb;

[ExcludeFromCodeCoverage]
public class CacheDbInitializer_Tests
{
    private static readonly ILogger<CacheDbInitializer> Logger = NullLogger<CacheDbInitializer>.Instance;

    private static CacheDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CacheDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        var context = new CacheDbContext(options);
        context.Database.OpenConnection();
        return context;
    }

    private static IServiceProvider CreateServiceProvider(CacheDbContext context)
    {
        var services = new ServiceCollection();

        // Register the specific context instance
        services.AddSingleton(context);

        return services.BuildServiceProvider();
    }

    #region StartAsync Tests

    [Fact(Timeout = 15000)]
    public async Task StartAsync_ShouldInitializeDatabase()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var serviceProvider = CreateServiceProvider(context);

        var initializer = new CacheDbInitializer(serviceProvider, Logger);

        // Act
        await initializer.StartAsync(CancellationToken.None);

        // Assert
        var schemaVersion = await CacheDbInitializer.GetSchemaVersionAsync(context);
        schemaVersion.Should().Be(CacheDbInitializer.CurrentSchemaVersion);

        context.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task StartAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var serviceProvider = CreateServiceProvider(context);
        var cts = new CancellationTokenSource();

        var initializer = new CacheDbInitializer(serviceProvider, Logger);
        cts.Cancel();

        // Act
        Func<Task> act = async () => await initializer.StartAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        context.Dispose();
    }

    #endregion

    #region StopAsync Tests

    [Fact(Timeout = 15000)]
    public async Task StopAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var serviceProvider = CreateServiceProvider(context);
        var initializer = new CacheDbInitializer(serviceProvider, Logger);

        // Act
        await initializer.StopAsync(CancellationToken.None);

        // Assert - Should not throw

        context.Dispose();
    }

    #endregion

    #region InitializeDatabaseAsync Tests

    [Fact(Timeout = 15000)]
    public async Task InitializeDatabaseAsync_WithNewDatabase_ShouldCreateSchema()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var serviceProvider = CreateServiceProvider(context);
        var initializer = new CacheDbInitializer(serviceProvider, Logger);

        // Act
        await initializer.InitializeDatabaseAsync(context);

        // Assert
        var schemaVersion = await CacheDbInitializer.GetSchemaVersionAsync(context);
        schemaVersion.Should().Be(CacheDbInitializer.CurrentSchemaVersion);

        context.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task InitializeDatabaseAsync_WithCurrentSchemaVersion_ShouldNotRecreateDatabase()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var serviceProvider = CreateServiceProvider(context);
        var initializer = new CacheDbInitializer(serviceProvider, Logger);

        await initializer.InitializeDatabaseAsync(context);

        // Add a test container to verify database is not deleted
        context.Containers.Add(new ContainerModel { Name = "test-container" });
        await context.SaveChangesAsync();

        // Act
        await initializer.InitializeDatabaseAsync(context);

        // Assert
        var containers = await context.Containers.ToListAsync();
        containers.Should().HaveCount(1);
        containers[0].Name.Should().Be("test-container");

        context.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task InitializeDatabaseAsync_WithOldSchemaVersion_ShouldRecreateDatabase()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var serviceProvider = CreateServiceProvider(context);
        var initializer = new CacheDbInitializer(serviceProvider, Logger);

        await CacheDbInitializer.CreateSchemaAsync(context);

        // Set an old schema version
        await context.SchemaVersions.ExecuteDeleteAsync();
        context.SchemaVersions.Add(new SchemaVersion { SchemaVersionId = 0 });
        await context.SaveChangesAsync();

        // Add a test container that should be deleted
        context.Containers.Add(new ContainerModel { Name = "test-container" });
        await context.SaveChangesAsync();

        // Clear change tracker before database recreation
        context.ChangeTracker.Clear();

        // Act
        await initializer.InitializeDatabaseAsync(context);

        // Assert
        var schemaVersion = await CacheDbInitializer.GetSchemaVersionAsync(context);
        schemaVersion.Should().Be(CacheDbInitializer.CurrentSchemaVersion);

        var containers = await context.Containers.ToListAsync();
        containers.Should().BeEmpty();

        context.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task InitializeDatabaseAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var serviceProvider = CreateServiceProvider(context);
        var initializer = new CacheDbInitializer(serviceProvider, Logger);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await initializer.InitializeDatabaseAsync(context, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        context.Dispose();
    }

    #endregion

    #region CreateSchemaAsync Tests

    [Fact(Timeout = 15000)]
    public async Task CreateSchemaAsync_WithNewDatabase_ShouldCreateTables()
    {
        // Arrange
        var context = CreateInMemoryContext();

        // Act
        await CacheDbInitializer.CreateSchemaAsync(context);

        // Assert
        var schemaVersion = await CacheDbInitializer.GetSchemaVersionAsync(context);
        schemaVersion.Should().Be(CacheDbInitializer.CurrentSchemaVersion);

        context.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task CreateSchemaAsync_WithExistingDatabase_ShouldNotRecreate()
    {
        // Arrange
        var context = CreateInMemoryContext();
        await CacheDbInitializer.CreateSchemaAsync(context);

        context.Containers.Add(new ContainerModel { Name = "test-container" });
        await context.SaveChangesAsync();

        // Act
        await CacheDbInitializer.CreateSchemaAsync(context);

        // Assert
        var containers = await context.Containers.ToListAsync();
        containers.Should().HaveCount(1);

        context.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task CreateSchemaAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await CacheDbInitializer.CreateSchemaAsync(context, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        context.Dispose();
    }

    #endregion

    #region GetSchemaVersionAsync Tests

    [Fact(Timeout = 15000)]
    public async Task GetSchemaVersionAsync_WithNoSchemaVersion_ShouldReturnZero()
    {
        // Arrange
        var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();

        // Act
        var schemaVersion = await CacheDbInitializer.GetSchemaVersionAsync(context);

        // Assert
        schemaVersion.Should().Be(0);

        context.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task GetSchemaVersionAsync_WithSchemaVersion_ShouldReturnVersion()
    {
        // Arrange
        var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();

        context.SchemaVersions.Add(new SchemaVersion { SchemaVersionId = 5 });
        await context.SaveChangesAsync();

        // Act
        var schemaVersion = await CacheDbInitializer.GetSchemaVersionAsync(context);

        // Assert
        schemaVersion.Should().Be(5);

        context.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task GetSchemaVersionAsync_WithMultipleSchemaVersions_ShouldReturnLatest()
    {
        // Arrange
        var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();

        context.SchemaVersions.Add(new SchemaVersion { SchemaVersionId = 1 });
        context.SchemaVersions.Add(new SchemaVersion { SchemaVersionId = 5 });
        context.SchemaVersions.Add(new SchemaVersion { SchemaVersionId = 3 });
        await context.SaveChangesAsync();

        // Act
        var schemaVersion = await CacheDbInitializer.GetSchemaVersionAsync(context);

        // Assert
        schemaVersion.Should().Be(5);

        context.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task GetSchemaVersionAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await CacheDbInitializer.GetSchemaVersionAsync(context, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        context.Dispose();
    }

    #endregion

    #region StoreCurrentSchemaVersionAsync Tests

    [Fact(Timeout = 15000)]
    public async Task StoreCurrentSchemaVersionAsync_ShouldStoreCurrentVersion()
    {
        // Arrange
        var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();

        // Act
        await CacheDbInitializer.StoreCurrentSchemaVersionAsync(context);

        // Assert
        var schemaVersion = await CacheDbInitializer.GetSchemaVersionAsync(context);
        schemaVersion.Should().Be(CacheDbInitializer.CurrentSchemaVersion);

        context.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task StoreCurrentSchemaVersionAsync_WithExistingVersion_ShouldReplaceVersion()
    {
        // Arrange
        var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();

        context.SchemaVersions.Add(new SchemaVersion { SchemaVersionId = 5 });
        await context.SaveChangesAsync();

        // Act
        await CacheDbInitializer.StoreCurrentSchemaVersionAsync(context);

        // Assert
        var schemaVersions = await context.SchemaVersions.ToListAsync();
        schemaVersions.Should().HaveCount(1);
        schemaVersions[0].SchemaVersionId.Should().Be(CacheDbInitializer.CurrentSchemaVersion);

        context.Dispose();
    }

    [Fact(Timeout = 15000)]
    public async Task StoreCurrentSchemaVersionAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await CacheDbInitializer.StoreCurrentSchemaVersionAsync(context, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        context.Dispose();
    }

    #endregion

    #region Constant Tests

    [Fact(Timeout = 15000)]
    public void CurrentSchemaVersion_ShouldBePositive()
    {
        // Arrange & Act & Assert
        CacheDbInitializer.CurrentSchemaVersion.Should().BeGreaterThan(0);
    }

    #endregion
}
