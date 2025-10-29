namespace AzuriteUI.Web.Services.CacheDb.Models;

/// <summary>
/// The base class for both blobs and containers, containing the common properties.
/// </summary>
public abstract class ResourceModel : IEquatable<ResourceModel>
{
    /// <summary>
    /// The name of the resource (blob or container).
    /// </summary>
    /// <remarks>
    /// Located in the Item version of the model class from Azurite.
    /// </remarks>
    public required string Name { get; set; }

    /// <summary>
    /// The cached copy ID, used for maintaining cache consistency.
    /// </summary>
    public string CachedCopyId { get; set; } = string.Empty;

    /// <summary>
    /// The entity tag of the resource (blob or container).
    /// </summary>
    /// <remarks>
    /// Located in the Properties version of the model class from Azurite.
    /// </remarks>
    public string ETag { get; set; } = string.Empty;

    /// <summary>
    /// If true, the resource has a legal hold placed on it.
    /// </summary>
    /// <remarks>
    /// Located in the Properties version of the model class from Azurite.
    /// </remarks>
    public bool HasLegalHold { get; set; } = false;

    /// <summary>
    /// The date/time that the resource (blob or container) was last modified.
    /// </summary>
    /// <remarks>
    /// Located in the Properties version of the model class from Azurite.
    /// </remarks>
    public DateTimeOffset LastModified { get; set; } = DateTimeOffset.MinValue;

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

    /// <inheritdoc />
    /// <remarks>
    /// Equality is determined by Name and ETag.
    /// </remarks>
    public bool Equals(ResourceModel? other)
        => other is not null
        && Name == other.Name
        && ETag == other.ETag;

    /// <summary>
    /// Override for Object.Equals because we are implementing <see cref="IEquatable{T}"/> 
    /// </summary>
    /// <param name="obj">The object to compare</param>
    /// <returns>The result of the comparison.</returns>
    public override bool Equals(object? obj)
        => obj is not null && Equals(obj as ResourceModel);

    /// <summary>
    /// Override for Object.GetHashCode because we are implementing <see cref="IEquatable{T}"/>
    /// </summary>
    /// <returns>The hash code for the current instance.</returns>
    public override int GetHashCode()
        => HashCode.Combine(Name, ETag);
}