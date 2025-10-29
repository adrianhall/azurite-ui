using AzuriteUI.Web.Services.CacheDb.Converters;
using AzuriteUI.Web.Services.CacheDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AzuriteUI.Web.Services.CacheDb.Configurations;

/// <summary>
/// The entity type configuration for the container model.
/// </summary>
public class ContainerModelConfiguration : ResourceModelConfiguration<ContainerModel>
{
    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<ContainerModel> builder)
    {
        base.Configure(builder);

        // Primary key
        builder.HasKey(e => e.Name);

        // Indexes for frequently queried properties
        builder.HasIndex(e => e.DefaultEncryptionScope);
        builder.HasIndex(e => e.PublicAccess);
        builder.HasIndex(e => e.HasImmutabilityPolicy);
        builder.HasIndex(e => e.HasImmutableStorageWithVersioning);
        builder.HasIndex(e => e.PreventEncryptionScopeOverride);
    }
}