using AzuriteUI.Web.Services.Azurite.Models;
using System.Diagnostics.CodeAnalysis;

namespace AzuriteUI.Web.Services.CacheDb.Models;

/// <summary>
/// A model representing a blob in Azurite.
/// </summary>
public class BlobModel : ResourceModel, IEquatable<BlobModel>
{
    /// <summary>
    /// The type of the blob.
    /// </summary>
    public AzuriteBlobType BlobType { get; set; } = AzuriteBlobType.Block;

    /// <summary>
    /// The content encoding of the blob.
    /// </summary>
    public string ContentEncoding { get; set; } = string.Empty;

    /// <summary>
    /// The content language of the blob.
    /// </summary>
    public string ContentLanguage { get; set; } = string.Empty;

    /// <summary>
    /// The length of the blob.
    /// </summary>
    public long ContentLength { get; set; } = 0L;

    /// <summary>
    /// The content type of the blob.
    /// </summary>
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>
    /// The date/time that the blob was created.
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.MinValue;

    /// <summary>
    /// The date/time that the blob will expire, if it has a time-to-live set.
    /// </summary>
    public DateTimeOffset? ExpiresOn { get; set; }

    /// <summary>
    /// The date/time that the blob was last accessed.
    /// </summary>
    public DateTimeOffset? LastAccessedOn { get; set; }

    /// <summary>
    /// The tags (key-value pairs) associated with this blob.
    /// </summary>
    public IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// The name of the container holding this blob.
    /// </summary>
    public required string ContainerName { get; set; }

    /// <summary>
    /// Navigation property for the container holding this blob.
    /// </summary>
    public virtual ContainerModel? Container
    {
        get;
        [ExcludeFromCodeCoverage(Justification = "Set by EF Core so not tested")]
        set;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Equality is determined by ContainerName, Name, and ETag.
    /// </remarks>
    public bool Equals(BlobModel? other)
        => other is not null
        && ContainerName == other.ContainerName
        && Name == other.Name
        && ETag == other.ETag;

    /// <summary>
    /// Override for Object.Equals because we are implementing <see cref="IEquatable{T}"/>
    /// </summary>
    /// <param name="obj">The object to compare</param>
    /// <returns>The result of the comparison.</returns>
    public override bool Equals(object? obj)
        => obj is BlobModel other && Equals(other);

    /// <summary>
    /// Override for Object.GetHashCode because we are implementing <see cref="IEquatable{T}"/>
    /// </summary>
    /// <returns>The hash code for the current instance.</returns>
    public override int GetHashCode()
        => HashCode.Combine(ContainerName, Name, ETag);
}