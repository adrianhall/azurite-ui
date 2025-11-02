namespace AzuriteUI.Web.Services.Repositories.Models;

/// <summary>
/// The base information for a data transfer object.
/// </summary>
/// <remarks>
/// Do not make this an abstract class.  OData library requires a concrete type for certain operations.
/// </remarks>
public interface IBaseDTO
{
    /// <summary>
    /// The name of the entity.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// The entity tag of the resource (blob or container).
    /// </summary>
    string ETag { get; set; }

    /// <summary>
    /// The date/time that the resource (blob or container) was last modified.
    /// </summary>
    DateTimeOffset LastModified { get; set; }
}