namespace AzuriteUI.Web.Services.Azurite.Models;

/// <summary>
/// The base information for an Azurite resource item (blob or container).
/// </summary>
public abstract class AzuriteResourceItem
{
    /// <summary>
    /// The name of the resource (blob or container).
    /// </summary>
    /// <remarks>
    /// Located in the Item version of the model class from Azurite.
    /// </remarks>
    public required string Name { get; set; }

    /// <summary>
    /// The entity tag of the resource (blob or container).
    /// </summary>
    /// <remarks>
    /// Located in the Properties version of the model class from Azurite.
    /// </remarks>
    public required string ETag { get; set; }

    /// <summary>
    /// The date/time that the resource (blob or container) was last modified.
    /// </summary>
    /// <remarks>
    /// Located in the Properties version of the model class from Azurite.
    /// </remarks>
    public required DateTimeOffset LastModified { get; set; }
}
