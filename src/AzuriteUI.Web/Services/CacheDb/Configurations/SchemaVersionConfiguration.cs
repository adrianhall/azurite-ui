using AzuriteUI.Web.Services.CacheDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AzuriteUI.Web.Services.CacheDb.Configurations;

/// <summary>
/// The entity type configuration for the schema version model.
/// </summary>
public class SchemaVersionConfiguration : IEntityTypeConfiguration<SchemaVersion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SchemaVersion> builder)
    {
        builder.ToTable("_schema_versions");
        builder.HasKey(e => e.SchemaVersionId);
    }
}