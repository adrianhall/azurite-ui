using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AzuriteUI.Web.Services.Repositories.Models;

/// <summary>
/// A data transfer object representing a container in Azurite.
/// </summary>
public class ContainerDTO : IBaseDTO
{
    /// <summary>
    /// The name of the container.
    /// </summary>
    [property: Required]
    [property: Description("The name of the container")]
    public required string Name { get; set; }

    /// <summary>
    /// The entity tag of the container.
    /// </summary>
    [property: Required]
    [property: Description("The entity tag of the container")]
    public required string ETag { get; set; }

    /// <summary>
    /// The date/time that the container was last modified.
    /// </summary>
    [property: Required]
    [property: Description("The date/time that the container was last modified")]
    public required DateTimeOffset LastModified { get; set; }

    /// <summary>
    /// Gets the count of blobs in the container.
    /// </summary>
    [property: Required]
    [property: Description("The count of blobs in the container")]
    public int BlobCount { get; set; } = 0;

    /// <summary>
    /// Specifies the default encryption scope for the container.
    /// </summary>
    [property: Description("The default encryption scope for the container")]
    public string DefaultEncryptionScope { get; set; } = string.Empty;

    /// <summary>
    /// Specifies whether the container has an immutability policy set.
    /// </summary>
    [property: Description("Specifies whether the container has an immutability policy set")]
    public bool HasImmutabilityPolicy { get; set; } = false;

    /// <summary>
    /// Specifies whether the container has immutable storage with versioning enabled.
    /// </summary>
    [property: Description("Specifies whether the container has immutable storage with versioning enabled")]
    public bool HasImmutableStorageWithVersioning { get; set; } = false;

    /// <summary>
    /// If true, the resource has a legal hold placed on it.
    /// </summary>
    [property: Description("If true, the resource has a legal hold placed on it")]
    public bool HasLegalHold { get; set; } = false;

    /// <summary>
    /// The metadata for the container.
    /// </summary>
    [property: Description("The metadata (key-value pairs) for the container")]
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Specifies whether the container prevents overriding the encryption scope set at the container level.
    /// </summary>
    [property: Description("Specifies whether the container prevents overriding the encryption scope set at the container level")]
    public bool PreventEncryptionScopeOverride { get; set; } = false;

    /// <summary>
    /// Specifies whether data in the container may be accessed publicly and the level of access.
    /// </summary>
    [property: Description("Specifies whether data in the container may be accessed publicly and the level of access")]
    [property: RegularExpression("none|blob|blobcontainer", ErrorMessage = "PublicAccess must be one of: none, blob, blobcontainer")]
    public string PublicAccess { get; set; } = "none";

    /// <summary>
    /// Only set if HasLegalHold is true.  The remaining amount of time to retain the resource, in days.
    /// </summary>
    [property: Description("Only set if HasLegalHold is true.  The remaining amount of time to retain the resource, in days")]
    public int? RemainingRetentionDays { get; set; }

    /// <summary>
    /// Gets the total size, in bytes, of all blobs in the container.
    /// </summary>
    [property: Description("The total size, in bytes, of all blobs in the container")]
    public long TotalSize { get; set; } = 0L;    
}