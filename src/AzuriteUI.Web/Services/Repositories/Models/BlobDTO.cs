using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AzuriteUI.Web.Services.Repositories.Models;

/// <summary>
/// A data transfer object representing a blob in Azurite.
/// </summary>
public class BlobDTO : IBaseDTO
{
    /// <summary>
    /// The name of the blob.
    /// </summary>
    [property: Required]
    [property: Description("The name of the blob")]
    public required string Name { get; set; }

    /// <summary>
    /// The entity tag of the blob.
    /// </summary>
    [property: Required]
    [property: Description("The entity tag of the blob")]
    public required string ETag { get; set; }

    /// <summary>
    /// The date/time that the blob was last modified.
    /// </summary>
    [property: Required]
    [property: Description("The date/time that the blob was last modified")]
    public required DateTimeOffset LastModified { get; set; }

    /// <summary>
    /// The type of the blob.
    /// </summary>
    [property: Required]
    [property: Description("The type of the blob")]
    [property: RegularExpression("block|append|page", ErrorMessage = "BlobType must be one of: block, append, page")]
    public string BlobType { get; set; } = "block";

    /// <summary>
    /// The name of the container holding this blob.
    /// </summary>
    [property: Required]
    [property: Description("The name of the container holding this blob")]
    public required string ContainerName { get; set; }

    /// <summary>
    /// The content encoding of the blob.
    /// </summary>
    [property: Description("The content encoding of the blob")]
    public string ContentEncoding { get; set; } = string.Empty;

    /// <summary>
    /// The content language of the blob.
    /// </summary>
    [property: Description("The content language of the blob")]
    public string ContentLanguage { get; set; } = string.Empty;

    /// <summary>
    /// The length of the blob.
    /// </summary>
    [property: Description("The length of the blob")]
    public long ContentLength { get; set; } = 0L;

    /// <summary>
    /// The content type of the blob.
    /// </summary>
    [property: Description("The content type of the blob")]
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>
    /// The date/time that the blob was created.
    /// </summary>
    [property: Description("The date/time that the blob was created")]
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.MinValue;

    /// <summary>
    /// The date/time that the blob will expire, if it has a time-to-live set.
    /// </summary>
    [property: Description("The date/time that the blob will expire, if it has a time-to-live set")]
    public DateTimeOffset? ExpiresOn { get; set; }

    /// <summary>
    /// If true, the resource has a legal hold placed on it.
    /// </summary>
    [property: Description("If true, the resource has a legal hold placed on it")]
    public bool HasLegalHold { get; set; } = false;

    /// <summary>
    /// The date/time that the blob was last accessed.
    /// </summary>
    [property: Description("The date/time that the blob was last accessed")]
    public DateTimeOffset? LastAccessedOn { get; set; }

    /// <summary>
    /// The metadata for the blob.
    /// </summary>
    [property: Description("The metadata (key-value pairs) for the blob")]
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// The tags (key-value pairs) associated with this blob.
    /// </summary>
    [property: Description("The tags (key-value pairs) associated with this blob")]
    public IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Only set if HasLegalHold is true.  The remaining amount of time to retain the resource, in days.
    /// </summary>
    [property: Description("Only set if HasLegalHold is true.  The remaining amount of time to retain the resource, in days")]
    public int? RemainingRetentionDays { get; set; }
}