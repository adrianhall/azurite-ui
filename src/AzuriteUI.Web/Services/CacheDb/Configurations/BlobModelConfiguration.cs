using AzuriteUI.Web.Services.CacheDb.Converters;
using AzuriteUI.Web.Services.CacheDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AzuriteUI.Web.Services.CacheDb.Configurations;

/// <summary>
/// The entity type configuration for the blob model.
/// </summary>
public class BlobModelConfiguration : ResourceModelConfiguration<BlobModel>
{
    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<BlobModel> builder)
    {
        base.Configure(builder);

        // Primary key
        builder.HasKey(e => new { e.ContainerName, e.Name });

        // Properties with converters
        builder.Property(e => e.CreatedOn)
               .HasConversion<DateTimeOffsetConverter>()
               .HasColumnType("TEXT")
               .IsRequired();

        builder.Property(e => e.ExpiresOn)
               .HasConversion<DateTimeOffsetConverter>()
               .HasColumnType("TEXT");

        builder.Property(e => e.LastAccessedOn)
               .HasConversion<DateTimeOffsetConverter>()
               .HasColumnType("TEXT");

        builder.Property(e => e.Tags)
               .HasConversion<DictionaryJsonConverter>()
               .HasColumnType("TEXT")
               .IsRequired();

        // Indexes for frequently queried properties
        builder.HasIndex(e => e.BlobType);
        builder.HasIndex(e => e.ContainerName);
        builder.HasIndex(e => e.ContentType);
        builder.HasIndex(e => e.ContentLength);
        builder.HasIndex(e => e.ContentEncoding);
        builder.HasIndex(e => e.ContentLanguage);
        builder.HasIndex(e => e.CreatedOn);
        builder.HasIndex(e => e.ExpiresOn);
        builder.HasIndex(e => e.LastAccessedOn);
        builder.HasIndex(e => e.Tags);

        // Relationships
        builder.HasOne(e => e.Container)
               .WithMany(e => e.Blobs)
               .HasForeignKey(e => e.ContainerName)
               .OnDelete(DeleteBehavior.Cascade);
    }
}