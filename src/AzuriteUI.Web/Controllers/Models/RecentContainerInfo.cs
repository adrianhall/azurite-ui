using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AzuriteUI.Web.Controllers.Models;

/// <summary>
/// Information about a recently modified container.
/// </summary>
public class RecentContainerInfo
{
    /// <summary>
    /// The name of the container.
    /// </summary>
    [property: Required]
    [property: Description("The name of the container")]
    public required string Name { get; set; }

    /// <summary>
    /// The last modified date for the most recent blob change or container last-modified (whichever is newer).
    /// </summary>
    [property: Required]
    [property: Description("The last modified date for the most recent blob change or container last-modified (whichever is newer)")]
    public required DateTimeOffset LastModified { get; set; }

    /// <summary>
    /// The number of blobs in the container.
    /// </summary>
    [property: Required]
    [property: Description("The number of blobs in the container")]
    public required int BlobCount { get; set; }

    /// <summary>
    /// The total size of all blobs in the container in bytes.
    /// </summary>
    [property: Required]
    [property: Description("The total size of all blobs in the container in bytes")]
    public required long TotalSize { get; set; }
}
