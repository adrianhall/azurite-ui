using AzuriteUI.Web.Services.CacheDb.Converters;
using AzuriteUI.Web.Services.CacheDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AzuriteUI.Web.Services.CacheDb.Configurations;

/// <summary>
/// The entity type configuration for the upload block model.
/// </summary>
public class UploadBlockModelConfiguration : IEntityTypeConfiguration<UploadBlockModel>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UploadBlockModel> builder)
    {
        // Primary key
        builder.HasKey(e => e.Id);

        // Required properties
        builder.Property(e => e.UploadId)
            .IsRequired();

        builder.Property(e => e.BlockId)
            .IsRequired();

        // Properties with converters
        builder.Property(e => e.UploadedAt)
            .HasConversion<DateTimeOffsetConverter>()
            .HasColumnType("TEXT")
            .IsRequired();

        // Unique constraint on UploadId + BlockId to prevent duplicate block IDs
        builder.HasIndex(e => new { e.UploadId, e.BlockId })
            .IsUnique();

        // Relationship is configured in UploadModelConfiguration
    }
}