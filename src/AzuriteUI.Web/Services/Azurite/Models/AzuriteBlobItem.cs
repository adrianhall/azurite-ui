using Azure.Storage.Blobs.Models;
using AzuriteUI.Web.Extensions;

namespace AzuriteUI.Web.Services.Azurite.Models;

/// <summary>
/// The types of supported Azurite blobs.
/// </summary>
public enum AzuriteBlobType
{
    /// <summary>
    /// Block blob type.
    /// </summary>
    Block,

    /// <summary>
    /// Append blob type.
    /// </summary>
    Append,

    /// <summary>
    /// Page blob type.
    /// </summary>
    Page
}

/// <summary>
/// The information about an Azurite blob.
/// </summary>
public class AzuriteBlobItem : AzuriteResourceItem
{
    /// <summary>
    /// The type of the blob.
    /// </summary>
    public required AzuriteBlobType BlobType { get; set; }

    /// <summary>
    /// The content encoding of the blob.
    /// </summary>
    public required string ContentEncoding { get; set; }

    /// <summary>
    /// The content language of the blob.
    /// </summary>
    public required string ContentLanguage { get; set; }

    /// <summary>
    /// The length of the blob.
    /// </summary>
    public required long ContentLength { get; set; }

    /// <summary>
    /// The content type of the blob.
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    /// The date/time that the blob was created.
    /// </summary>
    public DateTimeOffset? CreatedOn { get; set; }

    /// <summary>
    /// The date/time that the blob will expire, if it has a time-to-live set.
    /// </summary>
    public DateTimeOffset? ExpiresOn { get; set; }

    /// <summary>
    /// If true, the resource has a legal hold placed on it.
    /// </summary>
    /// <remarks>
    /// Located in the Properties version of the model class from Azurite.
    /// </remarks>
    public bool HasLegalHold { get; set; } = false;

    /// <summary>
    /// The date/time that the blob was last accessed.
    /// </summary>
    public DateTimeOffset? LastAccessedOn { get; set; }

    /// <summary>
    /// The metadata for the resource (blob or container).
    /// </summary>
    /// <remarks>
    /// Located in the Properties version of the model class from Azurite.
    /// </remarks>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Only set if HasLegalHold is true.  The remaining amount of time to retain the resource, in days.
    /// </summary>
    /// <remarks>
    /// Located in the Properties version of the model class from Azurite.
    /// </remarks>
    public int? RemainingRetentionDays { get; set; }

    /// <summary>
    /// The tags (key-value pairs) associated with this blob.
    /// </summary>
    public IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Creates an <see cref="AzuriteBlobItem"/> from an Azurite blob model.
    /// </summary>
    /// <param name="blobItem">The Azurite blob model.</param>
    /// <returns>The Azurite container item.</returns>
    public static AzuriteBlobItem FromAzure(BlobItem blobItem)
    {
        return new AzuriteBlobItem
        {
            // AzureResourceItem properties
            Name = blobItem.Name,
            ETag = blobItem.Properties.ETag?.ToString().Dequote() ?? string.Empty,
            LastModified = blobItem.Properties.LastModified.GetValueOrDefault(DateTimeOffset.MinValue),

            // AzuriteBlobItem properties
            BlobType = blobItem.Properties.BlobType.ToAzuriteBlobType(),
            ContentEncoding = blobItem.Properties.ContentEncoding ?? string.Empty,
            ContentLanguage = blobItem.Properties.ContentLanguage ?? string.Empty,
            ContentLength = blobItem.Properties.ContentLength.GetValueOrDefault(0L),
            ContentType = blobItem.Properties.ContentType,
            CreatedOn = blobItem.Properties.CreatedOn,
            ExpiresOn = blobItem.Properties.ExpiresOn,
            HasLegalHold = blobItem.Properties.HasLegalHold,
            LastAccessedOn = blobItem.Properties.LastAccessedOn,
            Metadata = blobItem.Metadata.ToDictionary(),
            RemainingRetentionDays = blobItem.Properties.RemainingRetentionDays,
            Tags = blobItem.Tags.ToDictionaryOrEmpty()
        };
    }
}
