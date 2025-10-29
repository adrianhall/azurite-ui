using Azure.Storage.Blobs.Models;
using AzuriteUI.Web.Extensions;

namespace AzuriteUI.Web.Services.Azurite.Models;

/// <summary>
/// The possible levels of public access for an Azurite container.
/// </summary>
public enum AzuritePublicAccess
{
    /// <summary>
    /// No public access.
    /// </summary>
    None,

    /// <summary>
    /// Blob-level public access.
    /// </summary>
    Blob,

    /// <summary>
    /// Container-level public access.
    /// </summary>
    Container
}

/// <summary>
/// The information about an Azurite container.
/// </summary>
public class AzuriteContainerItem : AzuriteResourceItem
{
    /// <summary>
    /// Specifies the default encryption scope for the container.
    /// </summary>
    public required string DefaultEncryptionScope { get; set; }

    /// <summary>
    /// Specifies whether the container has an immutability policy set.
    /// </summary>
    public bool HasImmutabilityPolicy { get; set; } = false;

    /// <summary>
    /// Specifies whether the container has immutable storage with versioning enabled.
    /// </summary>
    public bool HasImmutableStorageWithVersioning { get; set; } = false;

    /// <summary>
    /// If true, the resource has a legal hold placed on it.
    /// </summary>
    /// <remarks>
    /// Located in the Properties version of the model class from Azurite.
    /// </remarks>
    public bool HasLegalHold { get; set; } = false;

    /// <summary>
    /// The metadata for the resource (blob or container).
    /// </summary>
    /// <remarks>
    /// Located in the Properties version of the model class from Azurite.
    /// </remarks>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Specifies whether the container prevents overriding the encryption scope set at the container level.
    /// </summary>
    public bool PreventEncryptionScopeOverride { get; set; } = false;

    /// <summary>
    /// Specifies whether data in the container may be accessed publicly and the level of access.
    /// </summary>
    public required AzuritePublicAccess PublicAccess { get; set; }

    /// <summary>
    /// Only set if HasLegalHold is true.  The remaining amount of time to retain the resource, in days.
    /// </summary>
    /// <remarks>
    /// Located in the Properties version of the model class from Azurite.
    /// </remarks>
    public int? RemainingRetentionDays { get; set; }

    /// <summary>
    /// Creates an <see cref="AzuriteContainerItem"/> from an Azurite container model.
    /// </summary>
    /// <param name="container">The Azurite container model.</param>
    /// <returns>The Azurite container item.</returns>
    public static AzuriteContainerItem FromAzure(BlobContainerItem container)
    {
        return new AzuriteContainerItem
        {
            // AzuriteResourceItem properties
            Name = container.Name,
            ETag = container.Properties.ETag.ToString().Dequote(),
            LastModified = container.Properties.LastModified,

            // AzuriteContainerItem properties
            DefaultEncryptionScope = container.Properties.DefaultEncryptionScope ?? string.Empty,
            HasImmutabilityPolicy = container.Properties.HasImmutabilityPolicy.GetValueOrDefault(false),
            HasImmutableStorageWithVersioning = container.Properties.HasImmutableStorageWithVersioning,
            HasLegalHold = container.Properties.HasLegalHold.GetValueOrDefault(false),
            Metadata = container.Properties.Metadata?.ToDictionary() ?? [],
            PreventEncryptionScopeOverride = container.Properties.PreventEncryptionScopeOverride.GetValueOrDefault(false),
            PublicAccess = container.Properties.PublicAccess.GetValueOrDefault(PublicAccessType.None).ToAzuritePublicAccess(),
            RemainingRetentionDays = container.Properties.RemainingRetentionDays
        };
    }
}
