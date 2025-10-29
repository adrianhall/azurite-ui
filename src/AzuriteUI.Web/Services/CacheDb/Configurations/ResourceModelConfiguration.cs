using AzuriteUI.Web.Services.CacheDb.Converters;
using AzuriteUI.Web.Services.CacheDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AzuriteUI.Web.Services.CacheDb.Configurations;

/// <summary>
/// The entity type configuration for <see cref="ResourceModel"/> and its derived types.
/// </summary>
/// <typeparam name="T">The type of the derived type.</typeparam>
public abstract class ResourceModelConfiguration<T> : IEntityTypeConfiguration<T> where T : ResourceModel
{
    /// <inheritdoc />
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        // Configure common properties
        builder.Property(e => e.LastModified)
               .HasConversion<DateTimeOffsetConverter>()
               .HasColumnType("TEXT")
               .IsRequired();

        builder.Property(e => e.Name)
               .IsRequired();

        builder.Property(e => e.Metadata)
               .HasConversion<DictionaryJsonConverter>()
               .HasColumnType("TEXT")
               .IsRequired();

        // Indices for frequently queries properties
        builder.HasIndex(e => e.LastModified);
        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.CachedCopyId);
        builder.HasIndex(e => e.ETag);
        builder.HasIndex(e => e.Metadata);
    }
}