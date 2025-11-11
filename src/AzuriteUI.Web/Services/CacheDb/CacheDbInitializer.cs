using AzuriteUI.Web.Services.CacheDb.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace AzuriteUI.Web.Services.CacheDb;

/// <summary>
/// A hosted service used to initialize the cache database on application startup.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> for dependency injection.</param>
/// <param name="logger">The <see cref="ILogger{CacheDbInitializer}"/> for logging.</param>
public class CacheDbInitializer(IServiceProvider serviceProvider, ILogger<CacheDbInitializer> logger) : IHostedService
{
    /// <summary>
    /// A semaphore used to ensure that database initialization is only performed once at a time.
    /// </summary>
    private static readonly SemaphoreSlim _initializationLock = new(1, 1);

    /// <summary>
    /// The current schema version - this is incremented whenever a database schema change is made.
    /// </summary>
    internal const int CurrentSchemaVersion = 2;

    /// <summary>
    /// Part of the IHostedService implementation - starts the database initialization.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that resolves when the operation is complete.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CacheDbContext>();
        await InitializeDatabaseAsync(context, cancellationToken);
    }

    /// <summary>
    /// Part of the IHostedService implementation - stops the service.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that resolves when the operation is complete.</returns>
    [ExcludeFromCodeCoverage(Justification = "Not used within the implementation")]
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Initializes the database, creating it if it does not exist, and updating the schema if it has changed.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that resolves when the operation is complete.</returns>
    internal async Task InitializeDatabaseAsync(CacheDbContext context, CancellationToken cancellationToken = default)
    {
        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            logger.LogDebug("Starting database initialization.");
            await context.Database.OpenConnectionAsync(cancellationToken);
            logger.LogInformation("Database initialized at: {DatabasePath}", context.Database.GetConnectionString());
            await CreateSchemaAsync(context, cancellationToken);

            logger.LogDebug("Checking stored schema version vs. current schema version");
            int schemaVersion = await GetSchemaVersionAsync(context, cancellationToken);
            logger.LogDebug("Stored schema version = {SchemaVersion}, current schema version = {CurrentSchemaVersion}", schemaVersion, CurrentSchemaVersion);

            if (schemaVersion != CurrentSchemaVersion)
            {
                logger.LogWarning("Database schema has changed.  Refreshing the database with schema version {SchemaVersion}", CurrentSchemaVersion);
                await context.Database.EnsureDeletedAsync(cancellationToken);
                await CreateSchemaAsync(context, cancellationToken);
            }

            logger.LogDebug("Database initialization complete.");
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    /// <summary>
    /// Creates the database schema.  If we need to do anything beyond table creation, this is where it would go.
    /// </summary>
    /// <param name="context">The database context to use.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that resolves when the operation is complete.</returns>
    internal static async Task CreateSchemaAsync(CacheDbContext context, CancellationToken cancellationToken = default)
    {
        bool wasCreated = await context.Database.EnsureCreatedAsync(cancellationToken);
        if (wasCreated)
        {
            await StoreCurrentSchemaVersionAsync(context, cancellationToken);
        }
    }

    /// <summary>
    /// Retrieves the schema version from the database.  If it doesn't exist yet, then it returns 0.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The schema version, or 0 if not initialized.</returns>
    internal static async Task<int> GetSchemaVersionAsync(CacheDbContext context, CancellationToken cancellationToken = default)
    {
        SchemaVersion? schemaVersion = await context.SchemaVersions
            .OrderByDescending(sv => sv.SchemaVersionId)
            .FirstOrDefaultAsync(cancellationToken);
        return schemaVersion?.SchemaVersionId ?? 0;
    }

    /// <summary>
    /// Stores the current schema version in the database.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that resolves when the operation is complete.</returns>
    internal static async Task StoreCurrentSchemaVersionAsync(CacheDbContext context, CancellationToken cancellationToken = default)
    {
        await context.SchemaVersions.ExecuteDeleteAsync(cancellationToken);
        context.SchemaVersions.Add(new SchemaVersion { SchemaVersionId = CurrentSchemaVersion });
        await context.SaveChangesAsync(cancellationToken);
    }
}