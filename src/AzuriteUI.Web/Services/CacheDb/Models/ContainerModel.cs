using AzuriteUI.Web.Services.Azurite.Models;

namespace AzuriteUI.Web.Services.CacheDb.Models;

/// <summary>
/// A model representing a container in Azurite.
/// </summary>
public class ContainerModel : ResourceModel, IEquatable<ContainerModel>
{
    /// <summary>
    /// Specifies the default encryption scope for the container.
    /// </summary>
    public string DefaultEncryptionScope { get; set; } = string.Empty;

    /// <summary>
    /// Specifies whether the container has an immutability policy set.
    /// </summary>
    public bool HasImmutabilityPolicy { get; set; } = false;

    /// <summary>
    /// Specifies whether the container has immutable storage with versioning enabled.
    /// </summary>
    public bool HasImmutableStorageWithVersioning { get; set; } = false;

    /// <summary>
    /// Specifies whether data in the container may be accessed publicly and the level of access.
    /// </summary>
    public AzuritePublicAccess PublicAccess { get; set; } = AzuritePublicAccess.None;

    /// <summary>
    /// Specifies whether the container prevents overriding the encryption scope set at the container level.
    /// </summary>
    public bool PreventEncryptionScopeOverride { get; set; } = false;

    /// <summary>
    /// Gets the total size, in bytes, of all blobs in the container.
    /// This value is updated during cache synchronization.
    /// </summary>
    public long TotalSize { get; set; } = 0L;

    /// <summary>
    /// Gets the count of blobs in the container.
    /// This value is updated during cache synchronization.
    /// </summary>
    public int BlobCount { get; set; } = 0;

    /// <summary>
    /// Navigation property for the blobs in the container.
    /// </summary>
    public virtual ICollection<BlobModel> Blobs { get; set; } = [];

    /// <inheritdoc />
    /// <remarks>
    /// Equality is determined by Name and ETag.
    /// </remarks>
    public bool Equals(ContainerModel? other)
        => other is not null
        && Name == other.Name
        && ETag == other.ETag;

    /// <summary>
    /// Override for Object.Equals because we are implementing <see cref="IEquatable{T}"/>
    /// </summary>
    /// <param name="obj">The object to compare</param>
    /// <returns>The result of the comparison.</returns>
    public override bool Equals(object? obj)
        => obj is ContainerModel other && Equals(other);

    /// <summary>
    /// Override for Object.GetHashCode because we are implementing <see cref="IEquatable{T}"/>
    /// </summary>
    /// <returns>The hash code for the current instance.</returns>
    public override int GetHashCode()
        => HashCode.Combine(Name, ETag);
}