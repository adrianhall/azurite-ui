using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AzuriteUI.Web.Controllers.Models;

/// <summary>
/// A paged result with a specific type.
/// </summary>
/// <remarks>
/// This class is used in constructing OpenApi documents.
/// </remarks>
/// <typeparam name="T">The type of the entity.</typeparam>
public class PagedResponse<T>
{
    /// <summary>
    /// The list of entities to include in the response.
    /// </summary>
    [property: Required]
    [property: Description("The list of items within this page of the results")]
    public required IEnumerable<T> Items { get; set; }

    /// <summary>
    /// The number of entities returned by the search (after filtering, but before paging).
    /// </summary>
    [property: Description("The number of items returned by the search (after filtering, but before paging)")]
    public int? FilteredCount { get; set; }

    /// <summary>
    /// The number of entities in the entire set (before filtering).
    /// </summary>
    [property: Description("The number of items in the entire set (before filtering)")]
    public int? TotalCount { get; set; }

    /// <summary>
    /// The arguments to retrieve the next page of items.  The client needs to prepend
    /// the URI of the table to this.
    /// </summary>
    [property: Description("The arguments to retrieve the next page of items.  The client needs to prepend the URI of the table to this.")]
    public string? NextLink { get; set; }

    /// <summary>
    /// The arguments to retrieve the previous page of items.  The client needs to prepend
    /// the URI of the table to this.
    /// </summary>
    [property: Description("The arguments to retrieve the previous page of items.  The client needs to prepend the URI of the table to this.")]
    public string? PrevLink { get; set; }
}
