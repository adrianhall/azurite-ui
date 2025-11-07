using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AzuriteUI.Web.Controllers.Models;

/// <summary>
/// Information about a recently modified blob.
/// </summary>
public class RecentBlobInfo
{
    /// <summary>
    /// The name of the blob.
    /// </summary>
    [property: Required]
    [property: Description("The name of the blob")]
    public required string Name { get; set; }

    /// <summary>
    /// The name of the container that contains this blob.
    /// </summary>
    [property: Required]
    [property: Description("The name of the container that contains this blob")]
    public required string ContainerName { get; set; }

    /// <summary>
    /// The last modified date of the blob.
    /// </summary>
    [property: Required]
    [property: Description("The last modified date of the blob")]
    public required DateTimeOffset LastModified { get; set; }

    /// <summary>
    /// The content type of the blob.
    /// </summary>
    [property: Required]
    [property: Description("The content type of the blob")]
    public required string ContentType { get; set; }

    /// <summary>
    /// The content length of the blob in bytes.
    /// </summary>
    [property: Required]
    [property: Description("The content length of the blob in bytes")]
    public required long ContentLength { get; set; }
}
