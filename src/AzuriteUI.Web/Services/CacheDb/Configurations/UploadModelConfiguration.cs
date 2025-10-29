using AzuriteUI.Web.Services.CacheDb.Converters;
using AzuriteUI.Web.Services.CacheDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AzuriteUI.Web.Services.CacheDb.Configurations;

/// <summary>
/// The entity type configuration for the upload model.
/// </summary>
public class UploadModelConfiguration : IEntityTypeConfiguration<UploadModel>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UploadModel> builder)
    {
        // Primary key
        builder.HasKey(e => e.UploadId);

        // Required properties
        builder.Property(e => e.ContainerName)
               .IsRequired();

        builder.Property(e => e.BlobName)
               .IsRequired();

        builder.Property(e => e.ContentType)
               .IsRequired();

        // Properties with converters
        builder.Property(e => e.CreatedAt)
               .HasConversion<DateTimeOffsetConverter>()
               .HasColumnType("TEXT")
               .IsRequired();

        builder.Property(e => e.LastActivityAt)
               .HasConversion<DateTimeOffsetConverter>()
               .HasColumnType("TEXT")
               .IsRequired();

        builder.Property(e => e.Metadata)
               .HasConversion<DictionaryJsonConverter>()
               .HasColumnType("TEXT")
               .IsRequired();

        builder.Property(e => e.Tags)
               .HasConversion<DictionaryJsonConverter>()
               .HasColumnType("TEXT")
               .IsRequired();

        // Indexes for frequently queried properties
        builder.HasIndex(e => e.LastActivityAt);
        builder.HasIndex(e => new { e.ContainerName, e.BlobName });

        // Relationships - Blocks will be cascade deleted when upload is deleted
        builder.HasMany(e => e.Blocks)
               .WithOne(e => e.Upload)
               .HasForeignKey(e => e.UploadId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
