using AzuriteUI.Web.Services.CacheDb.Models;
using Microsoft.EntityFrameworkCore;

namespace AzuriteUI.Web.Services.CacheDb;

/// <summary>
/// The database context for the cache Sqlite database.
/// </summary>
/// <param name="options">the database context options</param>
public class CacheDbContext(DbContextOptions<CacheDbContext> options) : DbContext(options)
{
    /// <summary>
    /// The blobs registered within Azurite.
    /// </summary>
    public DbSet<BlobModel> Blobs => Set<BlobModel>();

    /// <summary>
    /// The containers registered within Azurite.
    /// </summary>
    public DbSet<ContainerModel> Containers => Set<ContainerModel>();

    /// <summary>
    /// The schema versions applied to the database.
    /// </summary>
    public DbSet<SchemaVersion> SchemaVersions => Set<SchemaVersion>();

    /// <summary>
    /// The in-progress uploads.
    /// </summary>
    public DbSet<UploadModel> Uploads => Set<UploadModel>();

    /// <summary>
    /// The uploaded blocks for in-progress uploads.
    /// </summary>
    public DbSet<UploadBlockModel> UploadBlocks => Set<UploadBlockModel>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CacheDbContext).Assembly);
    }
}